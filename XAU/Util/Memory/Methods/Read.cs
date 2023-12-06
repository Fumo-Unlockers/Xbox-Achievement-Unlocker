using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Memory.Imps;

namespace Memory
{
    public partial class Mem
    {
        /// <summary>
        /// Read a string value from an address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="file">path and name of ini file. (OPTIONAL)</param>
        /// <param name="length">length of bytes to read (OPTIONAL)</param>
        /// <param name="zeroTerminated">terminate string at null char</param>
        /// <param name="stringEncoding">System.Text.Encoding.UTF8 (DEFAULT). Other options: ascii, unicode, utf32, utf7</param>
        /// <returns></returns>
        public string ReadString(string code, string file = "", int length = 32, bool zeroTerminated = true, System.Text.Encoding stringEncoding = null)
        {
            if (stringEncoding == null)
                stringEncoding = System.Text.Encoding.UTF8;

            byte[] memoryNormal = new byte[length];
            UIntPtr theCode = GetCode(code, file);
            if (theCode == null || theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return "";

            if (ReadProcessMemory(mProc.Handle, theCode, memoryNormal, (UIntPtr)length, IntPtr.Zero))
                return (zeroTerminated) ? stringEncoding.GetString(memoryNormal).Split('\0')[0] : stringEncoding.GetString(memoryNormal);
            else
                return "";
        }
    }
}
