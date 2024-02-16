namespace Memory;

internal struct MemoryRegionResult
{
    public nuint CurrentBaseAddress { get; init; }
    public long RegionSize { get; init; }
    public nuint RegionBase { get; init; }
}