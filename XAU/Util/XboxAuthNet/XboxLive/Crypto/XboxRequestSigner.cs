using System;
using System.Text;

namespace XboxAuthNet.XboxLive.Crypto
{
    public class XboxRequestSigner : IXboxRequestSigner
    {
        private readonly IPopCryptoProvider _signer;

        public XboxRequestSigner(IPopCryptoProvider signer)
        {
            this._signer = signer;
        }

        public object ProofKey => _signer.ProofKey;

        public string SignRequest(string reqUri, string token, string body)
        {
            var timestamp = getWindowsTimestamp();
            var data = generatePayload(timestamp, reqUri, token, body);
            var signature = sign(timestamp, data);
            return Convert.ToBase64String(signature);
        }

        private byte[] generatePayload(ulong windowsTimestamp, string uri, string token, string payload)
        {
            var pathAndQuery = new Uri(uri).PathAndQuery;

            var allocSize =
                4 + 1 +
                8 + 1 +
                4 + 1 +
                pathAndQuery.Length + 1 +
                token.Length + 1 +
                payload.Length + 1;
            var bytes = new byte[allocSize];

            var policyVersion = BitConverter.GetBytes((int)1);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(policyVersion);
            Array.Copy(policyVersion, 0, bytes, 0, 4);

            var windowsTimestampBytes = BitConverter.GetBytes(windowsTimestamp);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(windowsTimestampBytes);
            Array.Copy(windowsTimestampBytes, 0, bytes, 5, 8);

            var strs =
                $"POST\0" +
                $"{pathAndQuery}\0" +
                $"{token}\0" +
                $"{payload}\0";
            var strsBytes = Encoding.ASCII.GetBytes(strs);
            Array.Copy(strsBytes, 0, bytes, 14, strsBytes.Length);

            return bytes;
        }

        private byte[] sign(ulong windowsTimestamp, byte[] bytes)
        {
            var signature = _signer.Sign(bytes);

            var policyVersion = BitConverter.GetBytes((int)1);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(policyVersion);

            var windowsTimestampBytes = BitConverter.GetBytes(windowsTimestamp);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(windowsTimestampBytes);

            var header = new byte[signature.Length + 12];
            Array.Copy(policyVersion, 0, header, 0, 4);
            Array.Copy(windowsTimestampBytes, 0, header, 4, 8);
            Array.Copy(signature, 0, header, 12, signature.Length);

            return header;
        }

        private ulong getWindowsTimestamp()
        {
            var unixTimestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            ulong windowsTimestamp = (unixTimestamp + 11644473600u) * 10000000u;
            return windowsTimestamp;
        }
    }
}
