using System.Diagnostics;
using System.Runtime.InteropServices;

using static Memory.Imps;

namespace Memory;

public partial class Mem
{
    public readonly Proc MProc = new();

    public nuint VirtualQueryEx(nint hProcess, nuint lpAddress, out MemoryBasicInformation lpBuffer)
    {
        nuint retVal;
        if (MProc.Is64Bit)
        {
            MemoryBasicInformation64 tmp64 = new();
            retVal = Native_VirtualQueryEx(hProcess, lpAddress, out tmp64, new UIntPtr((uint)Marshal.SizeOf(tmp64)));

            lpBuffer.BaseAddress = tmp64.BaseAddress;
            lpBuffer.AllocationBase = tmp64.AllocationBase;
            lpBuffer.AllocationProtect = tmp64.AllocationProtect;
            lpBuffer.RegionSize = (long)tmp64.RegionSize;
            lpBuffer.State = tmp64.State;
            lpBuffer.Protect = tmp64.Protect;
            lpBuffer.Type = tmp64.Type;

            return retVal;
        }

        MemoryBasicInformation32 tmp32 = new();
        retVal = Native_VirtualQueryEx(hProcess, lpAddress, out tmp32, new UIntPtr((uint)Marshal.SizeOf(tmp32)));

        lpBuffer.BaseAddress = tmp32.BaseAddress;
        lpBuffer.AllocationBase = tmp32.AllocationBase;
        lpBuffer.AllocationProtect = tmp32.AllocationProtect;
        lpBuffer.RegionSize = tmp32.RegionSize;
        lpBuffer.State = tmp32.State;
        lpBuffer.Protect = tmp32.Protect;
        lpBuffer.Type = tmp32.Type;
        return retVal;
    }

    public enum OpenProcessResults
    {
        InvalidArgument = 0,
        ProcessNotFound,
        NotResponding,
        FailedToOpenHandle,
        Success,
    }

    public OpenProcessResults OpenProcess(int processId)
    {
        var found = IsProcessRunning(processId);
        if (!found)
        {
            return OpenProcessResults.ProcessNotFound;
        }

        try
        {
            MProc.Process = Process.GetProcessById(processId);
        }
        catch
        {
            return OpenProcessResults.ProcessNotFound;
        }
        if (MProc.Process is { Responding: false })
        {
            return OpenProcessResults.NotResponding;
        }

        const int processAllAccess = 0x1F0FFF;
        MProc.Handle = Imps.OpenProcess(processAllAccess, false, processId);
        if (MProc.Handle == nint.Zero)
        {
            return OpenProcessResults.FailedToOpenHandle;
        }

        MProc.Is64Bit = IsWow64Process(MProc.Handle, out var retVal) && !retVal;
        return OpenProcessResults.Success;
    }

    public static bool IsProcessRunning(int processId)
    {
        if (processId <= 0)
        {
            return false;
        }
        
        var runningProcesses = Process.GetProcesses().Select(p => p.Id);
        return runningProcesses.Contains(processId);
    }
    
    public OpenProcessResults OpenProcess(string proc)
    {
        return string.IsNullOrWhiteSpace(proc) ? OpenProcessResults.InvalidArgument : OpenProcess(GetProcIdFromName(proc));
    }

    public static int GetProcIdFromName(string name)
    {
        var processlist = Process.GetProcesses();
        if (name.ToLower().Contains(".exe"))
        {
            name = name.Replace(".exe", "");
        }

        return (from theProcess in processlist
            where theProcess.ProcessName.Equals(name, StringComparison.CurrentCultureIgnoreCase)
            select theProcess.Id).FirstOrDefault();
    }
}