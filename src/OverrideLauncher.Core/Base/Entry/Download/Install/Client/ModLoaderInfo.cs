using System.Text.Json.Serialization;

namespace OverrideLauncher.Core.Base.Entry.Download.Install.Client;

public class ModLoaderInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = String.Empty;
    [JsonPropertyName("version")] public string Version { get; set; } = String.Empty;
}