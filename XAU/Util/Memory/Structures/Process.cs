using System.Diagnostics;

namespace Memory;

public class Proc
{
    public Process Process { get; set; } = new();
    public nint Handle { get; set; }
    public bool Is64Bit { get; set; }
}