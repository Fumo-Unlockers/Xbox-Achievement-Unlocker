using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace XAU.Services.LiveCapture;

public sealed class WindowsGameProcessResolver : IGameProcessResolver
{
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const int ErrorInsufficientBuffer = 122;

    public ValueTask<IReadOnlyList<GameProcessIdentity>> FindProcessesAsync(
        LiveCaptureTitle title,
        CancellationToken cancellationToken)
    {
        var matches = new List<GameProcessIdentity>();
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var packageFamilyName = GetPackageFamilyName(process.Id);
                    var executablePath = TryGetPath(process);
                    var packageMatch = !string.IsNullOrWhiteSpace(title.PackageFamilyName)
                        && string.Equals(
                            packageFamilyName,
                            title.PackageFamilyName,
                            StringComparison.OrdinalIgnoreCase);
                    var pathMatch = !string.IsNullOrWhiteSpace(title.ExecutablePath)
                        && !string.IsNullOrWhiteSpace(executablePath)
                        && string.Equals(
                            Path.GetFullPath(executablePath),
                            Path.GetFullPath(title.ExecutablePath),
                            StringComparison.OrdinalIgnoreCase);
                    if (!packageMatch && !pathMatch)
                        continue;

                    matches.Add(new GameProcessIdentity
                    {
                        ProcessId = process.Id,
                        ProcessName = process.ProcessName,
                        ExecutablePath = executablePath,
                        PackageFamilyName = packageFamilyName
                    });
                }
                catch
                {
                    // Process enumeration races and access failures are expected.
                }
            }
        }

        return ValueTask.FromResult<IReadOnlyList<GameProcessIdentity>>(matches
            .OrderBy(match => match.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(match => match.ProcessId)
            .ToList());
    }

    private static string? TryGetPath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetPackageFamilyName(int processId)
    {
        var handle = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (handle == IntPtr.Zero)
            return null;

        try
        {
            uint length = 0;
            if (GetPackageFamilyName(handle, ref length, null) != ErrorInsufficientBuffer)
                return null;

            var value = new StringBuilder((int)length);
            return GetPackageFamilyName(handle, ref length, value) == 0
                ? value.ToString()
                : null;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint processAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetPackageFamilyName(
        IntPtr process,
        ref uint packageFamilyNameLength,
        StringBuilder? packageFamilyName);
}
