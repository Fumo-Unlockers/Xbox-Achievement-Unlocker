namespace XboxAuthNet.XboxLive.Crypto
{
    public interface IXboxRequestSigner
    {
        object ProofKey { get; }
        string SignRequest(string reqUri, string token, string body);
    }
}
