namespace Memory;

public static class Utils
{
    public static byte[] ParseSig(string sig, out byte[] mask)
    {
        sig = sig.Replace('*', '?').Trim();
        while (sig.EndsWith(" ?") || sig.EndsWith(" ??"))
        {
            if (sig.EndsWith(" ??"))
            {
                sig = sig[..^3];
            }
            if (sig.EndsWith(" ?"))
            {
                sig = sig[..^2];
            }
        }
        var stringByteArray = sig.Split(' ');
        var sigPattern = new byte[stringByteArray.Length];
        mask = new byte[stringByteArray.Length];

        for (var i = 0; i < stringByteArray.Length; i++)
        {
            var ba = stringByteArray[i];

            if (ba == "??" || (ba.Length == 1 && ba == "?"))
            {
                mask[i] = 0x00;
                stringByteArray[i] = "0x00";
            }
            else if (char.IsLetterOrDigit(ba[0]) && ba[1] == '?')
            {
                mask[i] = 0xF0;
                stringByteArray[i] = ba[0] + "0";
            }
            else if (char.IsLetterOrDigit(ba[1]) && ba[0] == '?')
            {
                mask[i] = 0x0F;
                stringByteArray[i] = "0" + ba[1];
            }
            else
            {
                mask[i] = 0xFF;
            }
        }

        for (var i = 0; i < stringByteArray.Length; i++)
        {
            const int hexBase = 16;
            sigPattern[i] = (byte)(Convert.ToByte(stringByteArray[i], hexBase) & mask[i]);
        }
        
        return sigPattern;
    }
}