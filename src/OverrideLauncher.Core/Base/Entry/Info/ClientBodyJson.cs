using System.Text.Json.Serialization;

namespace OverrideLauncher.Core.Base.Entry.Info;

public class ClientBodyJson
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = String.Empty;
}