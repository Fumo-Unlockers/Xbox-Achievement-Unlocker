[CmdletBinding()]
param(
    [ValidateRange(1, 65535)]
    [int]$ApiPort = 1337,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$TargetProcessName,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$ObserverStatusPath,

    [ValidateRange(1, 300)]
    [int]$MaximumHeartbeatAgeSeconds = 10,

    [ValidateNotNullOrEmpty()]
    [string]$ExpectedObserverProcessName = 'XauDbwinObserver',

    [string]$ExpectedTargetExecutablePath = '',

    [string]$ExpectedTargetPackageFullName = ''
)

$ErrorActionPreference = 'Stop'
$checks = [System.Collections.Generic.List[object]]::new()
$apiReady = $false
$portOwnerReady = $false
$observerStatusLoaded = $false
$observerHeartbeatFresh = $false
$observerIdentityVerified = $false
$observerTargetVerified = $false
$observerResourcesReported = $false
$dbwinResourcesPresent = $false

function Add-Check {
    param(
        [string]$Name,
        [bool]$Passed,
        [string]$Details
    )

    $checks.Add([pscustomobject]@{
        Name = $Name
        Passed = $Passed
        Details = $Details
    })
}

if (-not [string]::IsNullOrWhiteSpace($ExpectedTargetPackageFullName))
{
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Text;

public static class XauResearchPackageIdentity
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint access, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetPackageFullName(IntPtr process, ref uint length, StringBuilder packageFullName);

    public static string Get(int processId)
    {
        var process = OpenProcess(0x1000, false, processId);
        if (process == IntPtr.Zero) return null;

        try
        {
            uint length = 0;
            if (GetPackageFullName(process, ref length, null) != 122) return null;
            var value = new StringBuilder((int)length);
            return GetPackageFullName(process, ref length, value) == 0 ? value.ToString() : null;
        }
        finally
        {
            CloseHandle(process);
        }
    }
}
'@
}

function Get-TargetPackageFullName {
    param([System.Diagnostics.Process]$Process)

    if ([string]::IsNullOrWhiteSpace($ExpectedTargetPackageFullName)) { return $null }
    return [XauResearchPackageIdentity]::Get($Process.Id)
}

$xauProcesses = @(Get-Process -Name 'XAU' -ErrorAction SilentlyContinue)
$xauDetails = if ($xauProcesses.Count -gt 0) {
    'PID(s): ' + (($xauProcesses | Select-Object -ExpandProperty Id) -join ', ')
} else {
    'XAU.exe is not running.'
}
Add-Check 'XAU process' ($xauProcesses.Count -gt 0) $xauDetails

$listener = @(Get-NetTCPConnection -State Listen -LocalPort $ApiPort -ErrorAction SilentlyContinue)
$xauProcessIds = @($xauProcesses | Select-Object -ExpandProperty Id)
$xauListener = @($listener | Where-Object { $xauProcessIds -contains $_.OwningProcess })
$httpSysListener = @($listener | Where-Object { $_.OwningProcess -eq 4 })
$portOwnedByOtherProcess = @($listener | Where-Object {
    $xauProcessIds -notcontains $_.OwningProcess -and $_.OwningProcess -ne 4
})
$apiSignatureVerified = $false

if ($portOwnedByOtherProcess.Count -gt 0)
{
    Add-Check 'Local API port owner' $false (
        "Port $ApiPort is owned by PID(s): " + (($portOwnedByOtherProcess | Select-Object -ExpandProperty OwningProcess -Unique) -join ', '))
}
elseif ($xauListener.Count -eq 0 -and $httpSysListener.Count -eq 0)
{
    Add-Check 'Local API port owner' $false "XAU is not listening on port $ApiPort."
}

try
{
    $response = Invoke-WebRequest -UseBasicParsing -Uri "http://localhost:$ApiPort/" -TimeoutSec 3
    $apiSignatureVerified = $response.StatusCode -eq 200 -and $response.Content -match '<title>XAU API Endpoints</title>'
    $apiReady = $apiSignatureVerified
    $readinessDetails = if ($apiSignatureVerified) {
        "GET / returned HTTP $($response.StatusCode) with the expected XAU endpoint signature."
    } else {
        "GET / returned HTTP $($response.StatusCode), but the response was not identified as XAU."
    }
    Add-Check 'Local API readiness' $apiSignatureVerified $readinessDetails
}

catch
{
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode)
    {
        Add-Check 'Local API readiness' $false "GET / returned HTTP $statusCode; the API is reachable but not ready."
    }
    else
    {
        Add-Check 'Local API readiness' $false "GET / failed: $($_.Exception.Message)"
    }
}

if ($portOwnedByOtherProcess.Count -eq 0 -and ($xauListener.Count -gt 0 -or ($httpSysListener.Count -gt 0 -and $apiSignatureVerified)))
{
    $portOwnerReady = $true
    $ownerDetails = if ($xauListener.Count -gt 0) {
        "XAU owns a listening socket on port $ApiPort."
    } else {
        "HTTP.sys owns port $ApiPort and the XAU endpoint signature was verified."
    }
    Add-Check 'Local API port owner' $true $ownerDetails
}
elseif ($portOwnedByOtherProcess.Count -eq 0 -and $listener.Count -gt 0)
{
    Add-Check 'Local API port owner' $false "Port $ApiPort is registered through HTTP.sys, but the XAU endpoint was not verified."
}

$discoveredTargetProcesses = @(Get-Process -Name $TargetProcessName -ErrorAction SilentlyContinue)
$targetRunning = $discoveredTargetProcesses.Count -gt 0
$targetProcesses = @($discoveredTargetProcesses | Where-Object {
    $path = try { $_.Path } catch { $null }
    $packageFullName = Get-TargetPackageFullName $_
    if (-not [string]::IsNullOrWhiteSpace($ExpectedTargetPackageFullName)) {
        $packageFullName -ieq $ExpectedTargetPackageFullName
    } else {
        -not [string]::IsNullOrWhiteSpace($path) -and
        ([string]::IsNullOrWhiteSpace($ExpectedTargetExecutablePath) -or
            [IO.Path]::GetFullPath($path) -ieq [IO.Path]::GetFullPath($ExpectedTargetExecutablePath))
    }
})
if (-not $targetRunning)
{
    Add-Check 'Target process identity' $false "No process named $TargetProcessName is running."
}
elseif ($targetProcesses.Count -eq 0)
{
    $expected = if (-not [string]::IsNullOrWhiteSpace($ExpectedTargetPackageFullName)) {
        "package $ExpectedTargetPackageFullName"
    } elseif ([string]::IsNullOrWhiteSpace($ExpectedTargetExecutablePath)) {
        'a readable executable path'
    } else {
        [IO.Path]::GetFullPath($ExpectedTargetExecutablePath)
    }
    Add-Check 'Target process identity' $false "A matching process is running, but none matched $expected."
}
else
{
    $identities = $targetProcesses | ForEach-Object {
        $path = try { $_.Path } catch { $null }
        $pathDisplay = if ($path) { $path } else { '<unavailable>' }
        $packageFullName = Get-TargetPackageFullName $_
        $packageDisplay = if ($packageFullName) { $packageFullName } else { '<none>' }
        "PID=$($_.Id), name=$($_.ProcessName), path=$pathDisplay, package=$packageDisplay"
    }
    Add-Check 'Target process identity' $true ($identities -join '; ')
}

$observerStatus = $null
try
{
    $resolvedStatusPath = (Resolve-Path -LiteralPath $ObserverStatusPath).Path
    $observerStatus = Get-Content -Raw -LiteralPath $resolvedStatusPath | ConvertFrom-Json
    $observerStatusLoaded = $true
    Add-Check 'Observer status file' $true "Loaded $resolvedStatusPath."
}
catch
{
    Add-Check 'Observer status file' $false "Observer status is unavailable or invalid: $($_.Exception.Message)"
}

if ($observerStatus)
{
    $updatedUtc = [DateTime]::MinValue
    $hasHeartbeat = [DateTime]::TryParse(
        [string]$observerStatus.updatedUtc,
        [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::AssumeUniversal,
        [ref]$updatedUtc)
    $heartbeatAge = if ($hasHeartbeat) { [DateTime]::UtcNow - $updatedUtc.ToUniversalTime() } else { [TimeSpan]::MaxValue }
    $active = $hasHeartbeat -and -not $observerStatus.stoppedUtc -and $heartbeatAge.TotalSeconds -le $MaximumHeartbeatAgeSeconds
    $observerHeartbeatFresh = $active
    $heartbeatDetails = if ($hasHeartbeat) {
        "Age=$([Math]::Round($heartbeatAge.TotalSeconds, 1))s; stopped=$([bool]$observerStatus.stoppedUtc)."
    } else {
        'The observer status has no valid updatedUtc heartbeat.'
    }
    Add-Check 'Observer heartbeat' $active $heartbeatDetails

    $observerProcess = $null
    $observerPid = 0
    $hasObserverPid = [int]::TryParse([string]$observerStatus.observerPid, [ref]$observerPid)
    if ($hasObserverPid)
    {
        $observerProcess = Get-Process -Id $observerPid -ErrorAction SilentlyContinue
    }
    $actualObserverPath = if ($observerProcess) { try { $observerProcess.Path } catch { $null } } else { $null }
    $reportedObserverPath = [string]$observerStatus.observerExecutablePath
    $observerIdentityVerified = [bool](
        $observerProcess -and
        $observerProcess.ProcessName -ieq $ExpectedObserverProcessName -and
        -not [string]::IsNullOrWhiteSpace($actualObserverPath) -and
        -not [string]::IsNullOrWhiteSpace($reportedObserverPath) -and
        [IO.Path]::GetFullPath($actualObserverPath) -ieq [IO.Path]::GetFullPath($reportedObserverPath))
    $observerIdentityDetails = if ($observerIdentityVerified) {
        "PID=$observerPid; name=$($observerProcess.ProcessName); path=$actualObserverPath"
    } elseif (-not $observerProcess) {
        'The heartbeat observer PID is not running.'
    } else {
        "Expected $ExpectedObserverProcessName with a matching executable path; actual name=$($observerProcess.ProcessName)."
    }
    Add-Check 'Observer process identity' $observerIdentityVerified $observerIdentityDetails

    $observerResourcesReported = [bool](
        $observerStatus.source -eq 'DBWIN' -and
        $observerStatus.resourcesInitialized -eq $true -and
        [string]::IsNullOrWhiteSpace([string]$observerStatus.failure))
    $reportedResourceDetails = if ($observerResourcesReported) {
        'Heartbeat reports initialized DBWIN resources and no failure.'
    } else {
        "source=$($observerStatus.source); initialized=$($observerStatus.resourcesInitialized); failure=$($observerStatus.failure)"
    }
    Add-Check 'Observer reported resources' $observerResourcesReported $reportedResourceDetails

    $lastPidRefresh = [DateTime]::MinValue
    $hasPidRefresh = [DateTime]::TryParse(
        [string]$observerStatus.lastPidRefreshUtc,
        [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::AssumeUniversal,
        [ref]$lastPidRefresh)
    $pidRefreshAge = if ($hasPidRefresh) { [DateTime]::UtcNow - $lastPidRefresh.ToUniversalTime() } else { [TimeSpan]::MaxValue }
    $pidRefreshFresh = $hasPidRefresh -and $pidRefreshAge.TotalSeconds -le $MaximumHeartbeatAgeSeconds
    $pidRefreshDetails = if ($hasPidRefresh) {
        "Age=$([Math]::Round($pidRefreshAge.TotalSeconds, 1))s."
    } else {
        'No valid lastPidRefreshUtc.'
    }
    Add-Check 'Observer PID refresh' $pidRefreshFresh $pidRefreshDetails

    $targetIds = @($targetProcesses | Select-Object -ExpandProperty Id)
    $observedTargets = @($observerStatus.targets | Where-Object {
        $targetIds -contains [int]$_.pid -and $_.name -ieq $TargetProcessName -and -not $_.endedUtc
    })
    $observerTargetDetails = if ($observedTargets.Count -gt 0) {
        'Verified PID(s): ' + (($observedTargets | Select-Object -ExpandProperty pid) -join ', ')
    } else {
        'The observer heartbeat does not identify a live matching target PID.'
    }
    $observerTargetVerified = $observedTargets.Count -gt 0 -and $pidRefreshFresh
    Add-Check 'Observer target PID' $observerTargetVerified $observerTargetDetails
}
else
{
    Add-Check 'Observer heartbeat' $false 'Cannot verify without a valid observer status file.'
    Add-Check 'Observer process identity' $false 'Cannot verify without a valid observer status file.'
    Add-Check 'Observer reported resources' $false 'Cannot verify without a valid observer status file.'
    Add-Check 'Observer PID refresh' $false 'Cannot verify without a valid observer status file.'
    Add-Check 'Observer target PID' $false 'Cannot verify without a valid observer status file.'
}

$dbwinHandles = @()
try
{
    $dbwinHandles += [Threading.EventWaitHandle]::OpenExisting('DBWIN_BUFFER_READY')
    $dbwinHandles += [Threading.EventWaitHandle]::OpenExisting('DBWIN_DATA_READY')
    $dbwinResourcesPresent = $true
    Add-Check 'Observer diagnostic source' $true 'DBWIN ready and data events are present.'
}
catch
{
    Add-Check 'Observer diagnostic source' $false 'DBWIN observer resources are not active.'
}
finally
{
    $dbwinHandles | ForEach-Object { $_.Dispose() }
}

$ready = -not ($checks | Where-Object { -not $_.Passed })
$state = if ($ready) {
    'READY'
} elseif (-not $apiReady -or -not $portOwnerReady) {
    'API UNAVAILABLE'
} elseif (-not $targetRunning) {
    'TARGET PROCESS NOT RUNNING'
} elseif ($observerStatusLoaded -and -not $observerHeartbeatFresh) {
    'OBSERVER STALE'
} elseif ($observerStatusLoaded -and (-not $observerResourcesReported -or -not $dbwinResourcesPresent)) {
    'OBSERVER RESOURCE FAILURE'
} elseif ($observerIdentityVerified -and -not $observerTargetVerified) {
    'OBSERVER RUNNING BUT TARGET NOT FOUND'
} else {
    'NOT READY'
}
Write-Output "Research Diagnostic Preflight: $state"
$checks | ForEach-Object {
    $marker = if ($_.Passed) { 'PASS' } else { 'FAIL' }
    Write-Output "[$marker] $($_.Name): $($_.Details)"
}

if (-not $ready) { exit 2 }
