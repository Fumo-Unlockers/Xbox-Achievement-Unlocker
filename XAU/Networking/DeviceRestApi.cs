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
    private const string UserAgent = "XblAuthManager";
    private readonly string _requestedResponseLanguage;
    private readonly XboxDeviceTypes _device;

    public DeviceRestApi(XboxDeviceTypes device)
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
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, _requestedResponseLanguage);
        _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-store, must-revalidate, no-cache");
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion2);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.AcceptEncoding, "gzip, deflate, br");
    }

    public async Task<DeviceToken?> GetDeviceTokenAsync()
    {
        SetDefaultHeaders();
        string bodyStr = JsonConvert.SerializeObject(new DeviceTokenRequest { Properties = new Properties { DeviceType = _device.ToString(), ProofKey = _signer.ProofKey } });
        var signature = _signer.SignRequest(DeviceUrl, HeaderValues.Signature, bodyStr);
        _httpClient.DefaultRequestHeaders.Add("Signature", signature);
        var response = await _httpClient.PostAsync(DeviceUrl, new StringContent(bodyStr, Encoding.UTF8, HeaderValues.Accept));
        Console.Out.WriteLine(response.StatusCode);
        var responseStr = await response.Content.ReadAsStringAsync();
        var token = JsonConvert.DeserializeObject<DeviceToken>(responseStr);
        return token;
    }
}
