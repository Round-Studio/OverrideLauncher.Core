using System.Text.Json.Serialization;

namespace OverrideLauncher.Core.Modules.Entry.AccountEntry;

public class MicrosoftLoginEntry
{
    #region 响应模型

    public sealed record DeviceCodeResponse {
        [JsonPropertyName("interval")] public int Interval { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("user_code")] public string UserCode { get; set; }
        [JsonPropertyName("device_code")] public string DeviceCode { get; set; }
        [JsonPropertyName("verification_uri")] public string VerificationUrl { get; set; }
    }

    public sealed record OAuth2TokenResponse {
        [JsonPropertyName("foci")] public string Foci { get; set; }
        [JsonPropertyName("scope")] public string Scope { get; set; }
        [JsonPropertyName("user_id")] public string UserId { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("token_type")] public string TokenType { get; set; }
        [JsonPropertyName("access_token")] public string AccessToken { get; set; }
        [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; }
    }

    public sealed record OAuth2TokenResponse2 {
        [JsonPropertyName("foci")] public string Foci { get; set; }
        [JsonPropertyName("scope")] public string Scope { get; set; }
        [JsonPropertyName("ext_expires_in")] public string ExtExpiresIn { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("token_type")] public string TokenType { get; set; }
        [JsonPropertyName("access_token")] public string AccessToken { get; set; }
        [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; }
    }

    public class OAuthErrorResponse
    {
        public string Error { get; set; }
        public string ErrorDescription { get; set; }
    }

    public class XboxLiveAuthRequest
    {
        public XboxLiveAuthProperties Properties { get; set; }
        public string RelyingParty { get; set; }
        public string TokenType { get; set; }
    }

    public class XboxLiveAuthProperties
    {
        public string AuthMethod { get; set; }
        public string SiteName { get; set; }
        public string RpsTicket { get; set; }
    }

    public class XboxLiveAuthResponse
    {
        public string IssueInstant { get; set; }
        public string NotAfter { get; set; }
        public string Token { get; set; }
        public XboxLiveDisplayClaims DisplayClaims { get; set; }
    }

    public class XboxLiveDisplayClaims
    {
        public List<XboxLiveUserHash> Xui { get; set; }
    }

    public class XboxLiveUserHash
    {
        public string Uhs { get; set; }
    }

    public class XSTSAuthRequest
    {
        public XSTSAuthProperties Properties { get; set; }
        public string RelyingParty { get; set; }
        public string TokenType { get; set; }
    }

    public class XSTSAuthProperties
    {
        public string SandboxId { get; set; }
        public string[] UserTokens { get; set; }
    }

    public class XSTSAuthResponse
    {
        public string IssueInstant { get; set; }
        public string NotAfter { get; set; }
        public string Token { get; set; }
        public XSTSDisplayClaims DisplayClaims { get; set; }
    }

    public class XSTSDisplayClaims
    {
        public List<XboxLiveUserHash> Xui { get; set; }
    }

    public class MinecraftAuthResponse
    {
        public string username { get; set; }
        public List<string> roles { get; set; }
        public string accesstoken { get; set; }
        public string tokenType { get; set; }
        public int expires_in { get; set; }
    }

    public class MinecraftEntitlementsResponse
    {
        public List<MinecraftEntitlement> Items { get; set; }
        public string Signature { get; set; }
        public string KeyId { get; set; }
    }

    public class MinecraftEntitlement
    {
        public string Name { get; set; }
        public string Signature { get; set; }
    }

    public class MinecraftProfileResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<MinecraftSkin> Skins { get; set; }
        public List<MinecraftCape> Capes { get; set; }
    }

    public class MinecraftSkin
    {
        public string Id { get; set; }
        public string State { get; set; }
        public string Url { get; set; }
        public string Variant { get; set; }
        public string Alias { get; set; }
    }

    public class MinecraftCape
    {
        public string Id { get; set; }
        public string State { get; set; }
        public string Url { get; set; }
        public string Alias { get; set; }
    }

    #endregion
}