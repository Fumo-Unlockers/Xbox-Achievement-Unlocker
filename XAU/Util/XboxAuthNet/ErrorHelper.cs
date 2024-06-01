using System;

namespace XboxAuthNet
{
    internal static class ErrorHelper
    {
        public static string? ConvertToHexErrorCode(string? errorCode)
        {
            if (!string.IsNullOrEmpty(errorCode))
            {
                var errorInt = long.Parse(errorCode);
                errorCode = errorInt.ToString("x");
            }
            return errorCode;
        }
        
        public static string? TryConvertToHexErrorCode(string? errorCode)
        {
            try
            {
                return ConvertToHexErrorCode(errorCode);
            }
            catch (FormatException)
            {
                return errorCode;
            }
            catch (OverflowException)
            {
                return errorCode;
            }
        }
    }
}
