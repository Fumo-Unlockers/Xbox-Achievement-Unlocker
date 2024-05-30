using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XAU.ViewModels.Pages;
using XAU.ViewModels.Windows;

public class DeviceRestApi
{
    private readonly HttpClient _httpClient;
    private readonly Signer _signer;
    private const string DeviceUrl = "https://device.auth.xboxlive.com/device/authenticate";
    private const string UserAgent = "Mozilla/5.0 (XboxReplay; XboxLiveAuth/3.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36";
    private readonly string _requestedResponseLanguage;

    public DeviceRestApi()
    {
        _requestedResponseLanguage = HomeViewModel.Settings.RegionOverride ? "en-GB" : System.Globalization.CultureInfo.CurrentCulture.Name;
        _httpClient = new HttpClient();
        _signer = new Signer(new ECDCertificatePopCryptoProvider());
    }

    private void SetDefaultHeaders()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, "en-US");
        _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-store, must-revalidate, no-cache");

    }
    public static void AddDefaultHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("User-Agent",UserAgent);
        request.Headers.Add("Accept-Language", "en-US");
        request.Headers.Add("Cache-Control", "no-store, must-revalidate, no-cache");
    }

    private object BuildBody(string id, string serialNumber, object proofKey)
    {
        return new
        {
            Properties = new
            {
                AuthMethod = "ProofOfPossession",
                Id = "{" + id + "}",
                DeviceType = "Nintendo",
                SerialNumber = "{" + serialNumber + "}",
                Version = "0.0.0",
                ProofKey = proofKey
            },
            RelyingParty = "http://auth.xboxlive.com",
            TokenType = "JWT"
        };
    }

    public async Task GetDeviceTokenAsync()
    {
        SetDefaultHeaders();
        string id = Guid.NewGuid().ToString("D"); // Replace with your actual id
        string serialNumber = Guid.NewGuid().ToString("D"); // Replace with your actual serial number
        string bodyStr = JsonConvert.SerializeObject(BuildBody(id, serialNumber, _signer.ProofKey));

        var req = new HttpRequestMessage
        {
            RequestUri = new Uri(DeviceUrl),
            Method = HttpMethod.Post,
            Content = new StringContent(bodyStr, Encoding.UTF8, "application/json")
        };
        var signature = _signer.SignRequest(DeviceUrl, HeaderValues.Signature, bodyStr);
        req.Headers.Add("Signature", signature);
        AddDefaultHeaders(req);
        var response = await _httpClient.SendAsync(req);
        Console.WriteLine(response);
    }
}
