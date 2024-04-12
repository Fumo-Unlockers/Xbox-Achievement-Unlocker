using System.Runtime.InteropServices;

namespace Memory;

public static partial class Imps
{
    [LibraryImport("kernel32.dll")]
    public static partial nint OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

    [LibraryImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
    public static partial nuint Native_VirtualQueryEx(nint hProcess, nuint lpAddress, out MemoryBasicInformation32 lpBuffer, nuint dwLength);

    [LibraryImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
    public static partial nuint Native_VirtualQueryEx(nint hProcess, nuint lpAddress, out MemoryBasicInformation64 lpBuffer, nuint dwLength);

    [LibraryImport("kernel32.dll")]
    public static partial void GetSystemInfo(out SystemInfo lpSystemInfo);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial void ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, IntPtr lpBuffer, UIntPtr nSize, out ulong lpNumberOfBytesRead);
    
    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesRead);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWow64Process(nint hProcess, [MarshalAs(UnmanagedType.Bool)] out bool lpSystemInfo);
    
    public const uint MemCommit = 0x00001000;

    public const uint Readonly = 0x02;
    public const uint Readwrite = 0x04;
    public const uint WriteCopy = 0x08;
    public const uint ExecuteReadwrite = 0x40;
    public const uint ExecuteWriteCopy = 0x80;
    public const uint Execute = 0x10;
    public const uint ExecuteRead = 0x20;

    public const uint Guard = 0x100;
    public const uint NoAccess = 0x01;

    public const uint MemPrivate = 0x20000;
    public const uint MemImage = 0x1000000;
    public const uint MemMapped = 0x40000;
    
    public struct SystemInfo
    {
        public ushort ProcessorArchitecture;
        private ushort _reserved;
        public uint PageSize;
        public nuint MinimumApplicationAddress;
        public nuint MaximumApplicationAddress;
        public nint ActiveProcessorMask;
        public uint NumberOfProcessors;
        public uint ProcessorType;
        public uint AllocationGranularity;
        public ushort ProcessorLevel;
        public ushort ProcessorRevision;
    }

    public struct MemoryBasicInformation32
    {
        public nuint BaseAddress;
        public nuint AllocationBase;
        public uint AllocationProtect;
        public uint RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    public struct MemoryBasicInformation64
    {
        public nuint BaseAddress;
        public nuint AllocationBase;
        public uint AllocationProtect;
        public uint Alignment1;
        public ulong RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
        public uint Alignment2;
    }

    public struct MemoryBasicInformation
    {
        public nuint BaseAddress;
        public nuint AllocationBase;
        public uint AllocationProtect;
        public long RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }
}