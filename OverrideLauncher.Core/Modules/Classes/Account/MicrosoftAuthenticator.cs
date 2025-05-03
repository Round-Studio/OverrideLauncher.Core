using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OverrideLauncher.Core.Modules.Entry.AccountEntry;

public class CustomHttpClientHandler : HttpClientHandler
{
    public CustomHttpClientHandler()
    {
        // 在构造函数中设置 ServerCertificateCustomValidationCallback
        this.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 其他逻辑保持不变
        return await base.SendAsync(request, cancellationToken);
    }
}

public class MicrosoftAuthenticator
{
    public class LoginCodeEntry
    {
        public string URL { get; set; }
        public string Code { get; set; }
    }
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly HttpClient _httpClient;
    private readonly IEnumerable<string> _scopes = new List<string> { "XboxLive.signin", "offline_access" };
    private AccountEntry _accountEntry;
    public Action<LoginCodeEntry> Login;

    public MicrosoftAuthenticator(string clientId, string clientSecret = null)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _httpClient = new HttpClient(new CustomHttpClientHandler()); // 使用自定义的 HttpClientHandler
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _accountEntry = new AccountEntry();
    }

    public async Task<AccountEntry> Authenticator()
    {
        // 获取设备代码
        var deviceCodeResponse = await GetDeviceCode();
        if (deviceCodeResponse == null) return null;

        Console.WriteLine($"请在设备上输入以下代码: {deviceCodeResponse.user_code}");
        Console.WriteLine($"验证地址: {deviceCodeResponse.verification_uri}");
        Login.Invoke(new LoginCodeEntry()
        {
            URL = deviceCodeResponse.verification_uri,
            Code = deviceCodeResponse.user_code
        });

        // 轮询授权状态
        var tokenResponse = await PollForToken(deviceCodeResponse.device_code);
        if (tokenResponse == null) return null;

        // Xbox Live身份验证
        var xblTokenResponse = await AuthenticateWithXboxLive(tokenResponse.access_token);
        if (xblTokenResponse == null) return null;

        // XSTS身份验证
        var xstsTokenResponse = await AuthenticateWithXSTS(xblTokenResponse.Token);
        if (xstsTokenResponse == null) return null;

        // 获取Minecraft访问令牌
        var minecraftTokenResponse = await GetMinecraftAccessToken(xblTokenResponse.DisplayClaims.xui[0].uhs, xstsTokenResponse.Token);
        if (minecraftTokenResponse == null) return null;

        // 检查游戏拥有情况
        var entitlementsResponse = await CheckGameEntitlements(minecraftTokenResponse.access_token);
        if (entitlementsResponse == null || entitlementsResponse.items == null || entitlementsResponse.items.Count == 0)
        {
            Console.WriteLine("该账号未拥有Minecraft游戏。");
            return null;
        }

        // 获取玩家UUID
        var profileResponse = await GetPlayerProfile(minecraftTokenResponse.access_token);
        if (profileResponse == null)
        {
            Console.WriteLine("无法获取玩家UUID。");
            return null;
        }

        // 设置账户信息
        _accountEntry.Token = minecraftTokenResponse.access_token;
        _accountEntry.UserName = profileResponse.name;
        _accountEntry.UUID = profileResponse.id;
        _accountEntry.AccountType = "msa";

        return _accountEntry;
    }

    private async Task<DeviceCodeResponse> GetDeviceCode()
    {
        var requestUri = "https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode";
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("scope", string.Join(" ", _scopes))
        });

        var response = await _httpClient.PostAsync(requestUri, content);
        if (!response.IsSuccessStatusCode) return null;

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DeviceCodeResponse>(responseContent);
    }

    private async Task<TokenResponse> PollForToken(string deviceCode)
    {
        var requestUri = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code"),
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("device_code", deviceCode)
        });

        while (true)
        {
            var response = await _httpClient.PostAsync(requestUri, content);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent);
                if (errorResponse.error == "authorization_pending")
                {
                    await Task.Delay(5000); // 等待5秒后重试
                    continue;
                }
                else
                {
                    Console.WriteLine($"轮询授权状态失败: {errorResponse.error_description}");
                    return null;
                }
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TokenResponse>(responseContent);
        }
    }

    private async Task<XblTokenResponse> AuthenticateWithXboxLive(string accessToken)
    {
        var requestUri = "https://user.auth.xboxlive.com/user/authenticate";
        var content = new StringContent(
            JsonSerializer.Serialize(new
            {
                Properties = new
                {
                    AuthMethod = "RPS",
                    SiteName = "user.auth.xboxlive.com",
                    RpsTicket = $"d={accessToken}"
                },
                RelyingParty = "http://auth.xboxlive.com",
                TokenType = "JWT"
            }),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync(requestUri, content);
        if (!response.IsSuccessStatusCode) return null;

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<XblTokenResponse>(responseContent);
    }

    private async Task<XstsTokenResponse> AuthenticateWithXSTS(string xblToken)
    {
        var requestUri = "https://xsts.auth.xboxlive.com/xsts/authorize";
        var content = new StringContent(
            JsonSerializer.Serialize(new
            {
                Properties = new
                {
                    SandboxId = "RETAIL",
                    UserTokens = new[] { xblToken }
                },
                RelyingParty = "rp://api.minecraftservices.com/",
                TokenType = "JWT"
            }),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync(requestUri, content);
        if (!response.IsSuccessStatusCode) return null;

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<XstsTokenResponse>(responseContent);
    }

    private async Task<MinecraftTokenResponse> GetMinecraftAccessToken(string uhs, string xstsToken)
    {
        var requestUri = "https://api.minecraftservices.com/authentication/login_with_xbox";
        var content = new StringContent(
            JsonSerializer.Serialize(new
            {
                identityToken = $"XBL3.0 x={uhs};{xstsToken}"
            }),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync(requestUri, content);
        if (!response.IsSuccessStatusCode) return null;

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MinecraftTokenResponse>(responseContent);
    }

    private async Task<EntitlementsResponse> CheckGameEntitlements(string accessToken)
    {
        var requestUri = "https://api.minecraftservices.com/entitlements/mcstore";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.GetAsync(requestUri);
        if (!response.IsSuccessStatusCode) return null;

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<EntitlementsResponse>(responseContent);
    }

    private async Task<PlayerProfileResponse> GetPlayerProfile(string accessToken)
    {
        var requestUri = "https://api.minecraftservices.com/minecraft/profile";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.GetAsync(requestUri);
        if (!response.IsSuccessStatusCode) return null;

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PlayerProfileResponse>(responseContent);
    }
}

public class DeviceCodeResponse
{
    public string device_code { get; set; }
    public string user_code { get; set; }
    public string verification_uri { get; set; }
    public int expires_in { get; set; }
    public int interval { get; set; }
    public string message { get; set; }
}

public class TokenResponse
{
    public string token_type { get; set; }
    public string scope { get; set; }
    public int expires_in { get; set; }
    public string access_token { get; set; }
    public string refresh_token { get; set; }
    public string id_token { get; set; }
}

public class ErrorResponse
{
    public string error { get; set; }
    public string error_description { get; set; }
}

public class XblTokenResponse
{
    public string IssueInstant { get; set; }
    public string NotAfter { get; set; }
    public string Token { get; set; }
    public DisplayClaims DisplayClaims { get; set; }
}

public class DisplayClaims
{
    public List<Xui> xui { get; set; }
}

public class Xui
{
    public string uhs { get; set; }
}

public class XstsTokenResponse
{
    public string IssueInstant { get; set; }
    public string NotAfter { get; set; }
    public string Token { get; set; }
    public DisplayClaims DisplayClaims { get; set; }
}

public class MinecraftTokenResponse
{
    public string username { get; set; }
    public List<string> roles { get; set; }
    public string access_token { get; set; }
    public string token_type { get; set; }
    public int expires_in { get; set; }
}

public class EntitlementsResponse
{
    public List<Item> items { get; set; }
    public string signature { get; set; }
    public string keyId { get; set; }
}

public class Item
{
    public string name { get; set; }
    public string signature { get; set; }
}

public class PlayerProfileResponse
{
    public string id { get; set; }
    public string name { get; set; }
    public List<Skin> skins { get; set; }
    public List<Cape> capes { get; set; }
}

public class Skin
{
    public string id { get; set; }
    public string state { get; set; }
    public string url { get; set; }
    public string variant { get; set; }
    public string alias { get; set; }
}

public class Cape
{
    public string id { get; set; }
    public string state { get; set; }
    public string url { get; set; }
}