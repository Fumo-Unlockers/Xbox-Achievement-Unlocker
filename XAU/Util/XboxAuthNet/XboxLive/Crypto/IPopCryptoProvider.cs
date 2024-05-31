namespace XboxAuthNet.XboxLive.Crypto
{
    public interface IPopCryptoProvider
    {
        object ProofKey { get; }
        byte[] Sign(byte[] data);
    }
}