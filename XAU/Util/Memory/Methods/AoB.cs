using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using static Memory.Imps;

namespace Memory;

public partial class Mem
{
    public Task<IEnumerable<nuint>> AoBScan(long start, long end, string search, bool writable = false, bool executable = true, bool mapped = false)
    {
        return AoBScan(start, end, search, false, writable, executable, mapped);
    }

    public Task<IEnumerable<nuint>> AoBScan(string search, bool writable = false, bool executable = true, bool mapped = false)
    {
        return AoBScan(0, long.MaxValue, search, false, writable, executable, mapped);
    }

    public Task<IEnumerable<nuint>> AoBScan(long start, long end, string search, bool readable, bool writable, bool executable, bool mapped)
    {
        return Task.Run(() =>
        {
            List<MemoryRegionResult> memRegionList = new();
            var aobPattern = Utils.ParseSig(search, out var mask);
            GetSystemInfo(out var sysInfo);

            var procMinAddress = sysInfo.MinimumApplicationAddress;
            var procMaxAddress = sysInfo.MaximumApplicationAddress;

            if (start < (long)procMinAddress.ToUInt64())
            {
                start = (long)procMinAddress.ToUInt64();
            }

            if (end > (long)procMaxAddress.ToUInt64())
            {
                end = (long)procMaxAddress.ToUInt64();
            }

            var currentBaseAddress = (nuint)start;

            while (VirtualQueryEx(MProc.Handle, currentBaseAddress, out var memInfo).ToUInt64() != 0 &&
                   currentBaseAddress.ToUInt64() < (ulong)end &&
                   currentBaseAddress.ToUInt64() + (ulong)memInfo.RegionSize >
                   currentBaseAddress.ToUInt64())
            {
                var isValid = memInfo.State == MemCommit;
                isValid &= memInfo.BaseAddress.ToUInt64() < procMaxAddress.ToUInt64();
                isValid &= (memInfo.Protect & Guard) == 0;
                isValid &= (memInfo.Protect & NoAccess) == 0;
                isValid &= memInfo.Type is MemPrivate or MemImage;
                if (mapped)
                {
                    isValid &= memInfo.Type == MemMapped;
                }

                if (isValid)
                {
                    var isReadable = (memInfo.Protect & Readonly) > 0;

                    var isWritable = (memInfo.Protect & Readwrite) > 0 ||
                                     (memInfo.Protect & WriteCopy) > 0 ||
                                     (memInfo.Protect & ExecuteReadwrite) > 0 ||
                                     (memInfo.Protect & ExecuteWriteCopy) > 0;

                    var isExecutable = (memInfo.Protect & Execute) > 0 ||
                                       (memInfo.Protect & ExecuteRead) > 0 ||
                                       (memInfo.Protect & ExecuteReadwrite) > 0 ||
                                       (memInfo.Protect & ExecuteWriteCopy) > 0;

                    isReadable &= readable;
                    isWritable &= writable;
                    isExecutable &= executable;

                    isValid &= isReadable || isWritable || isExecutable;
                }

                if (!isValid)
                {
                    currentBaseAddress = new UIntPtr(memInfo.BaseAddress.ToUInt64() + (ulong)memInfo.RegionSize);
                    continue;
                }

                MemoryRegionResult memRegion = new()
                {
                    CurrentBaseAddress = currentBaseAddress,
                    RegionSize = memInfo.RegionSize,
                    RegionBase = memInfo.BaseAddress
                };

                currentBaseAddress = new UIntPtr(memInfo.BaseAddress.ToUInt64() + (ulong)memInfo.RegionSize);

                if (memRegionList.Count > 0)
                {
                    var previousRegion = memRegionList[^1];

                    if ((long)previousRegion.RegionBase + previousRegion.RegionSize == (long)memInfo.BaseAddress)
                    {
                        memRegionList[^1] = previousRegion with { RegionSize = previousRegion.RegionSize + memInfo.RegionSize };

                        continue;
                    }
                }

                memRegionList.Add(memRegion);
            }

            ConcurrentBag<nuint> bagResult = new();

            Parallel.ForEach(memRegionList,
                (item, _, _) =>
                {
                    var compareResults = CompareScan(item, aobPattern, mask);

                    foreach (var result in compareResults)
                    {
                        bagResult.Add(result);
                    }
                });

            return bagResult.ToList().OrderBy(c => c).AsEnumerable();
        });
    }
    
    private IEnumerable<nuint> CompareScan(MemoryRegionResult item, byte[] aobPattern, byte[] mask)
    {
        if (mask.Length != aobPattern.Length)
        {
            throw new ArgumentException(null, $"{nameof(aobPattern)}.Length != {nameof(mask)}.Length");
        }

        var buffer = Marshal.AllocHGlobal((int)item.RegionSize);

        ReadProcessMemory(MProc.Handle, item.CurrentBaseAddress, buffer, (nuint)item.RegionSize, out var bytesRead);

        var result = 0 - aobPattern.Length;
        List<nuint> ret = new();
        unsafe
        {
            do
            {
                result = FindPattern((byte*)buffer.ToPointer(), (int)bytesRead, aobPattern, mask, result + aobPattern.Length);

                if (result >= 0)
                    ret.Add(item.CurrentBaseAddress + (uint)result);

            } while (result != -1);
        }

        Marshal.FreeHGlobal(buffer);

        return ret.ToArray();
    }

    private static unsafe int FindPattern(byte* body, int bodyLength, IReadOnlyList<byte> pattern, IReadOnlyList<byte> masks, int start = 0)
    {
        var foundIndex = -1;

        if (bodyLength <= 0 || pattern.Count <= 0 || start > bodyLength - pattern.Count || pattern.Count > bodyLength)
        {
            return foundIndex;
        }

        for (var index = start; index <= bodyLength - pattern.Count; index++)
        {
            if ((body[index] & masks[0]) != (pattern[0] & masks[0])) continue;

            var match = true;

            for (var index2 = pattern.Count - 1; index2 >= 1; index2--)
            {
                if ((body[index + index2] & masks[index2]) == (pattern[index2] & masks[index2])) continue;

                match = false;
                break;
            }

            if (!match)
            {
                continue;
            }

            foundIndex = index;
            break;
        }

        return foundIndex;
    }
}