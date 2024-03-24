using System.Text;
using static Memory.Imps;

namespace Memory;

public partial class Mem
{
    public string ReadStringMemory(nuint address, int length = 32, bool zeroTerminated = true, Encoding? stringEncoding = null)
    {
        stringEncoding ??= Encoding.UTF8;
        
        var memoryNormal = new byte[length];
        if (ReadProcessMemory(MProc.Handle, address, memoryNormal, (UIntPtr)length, 0))
        {
            return zeroTerminated ? stringEncoding.GetString(memoryNormal).Split('\0')[0] : stringEncoding.GetString(memoryNormal);
        }
        
        return "";
    }
}