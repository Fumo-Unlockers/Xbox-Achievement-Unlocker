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
    private readonly string _device;

    public DeviceRestApi(string device)
    {
        _device = device;
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

    private object BuildBody()
    {
        string id = Guid.NewGuid().ToString("D"); // Replace with your actual id
        string serialNumber = Guid.NewGuid().ToString("D"); // Replace with your actual serial number
        return new
        {
            Properties = new
            {
                AuthMethod = "ProofOfPossession",
                Id = "{" + id + "}",
                DeviceType = _device,
                SerialNumber = "{" + serialNumber + "}",
                Version = "15.6.1",
                ProofKey = _signer.ProofKey
            },
            RelyingParty = "http://auth.xboxlive.com",
            TokenType = "JWT"
        };
    }

    public async Task<DeviceToken?> GetDeviceTokenAsync()
    {
        SetDefaultHeaders();
        string bodyStr = JsonConvert.SerializeObject(BuildBody());
        var signature = _signer.SignRequest(DeviceUrl, HeaderValues.Signature, bodyStr);
        _httpClient.DefaultRequestHeaders.Add("Signature", signature);
        var response = await _httpClient.PostAsync(DeviceUrl, new StringContent(bodyStr, Encoding.UTF8, HeaderValues.Accept)).Result.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<DeviceToken>(response);
    }
}
