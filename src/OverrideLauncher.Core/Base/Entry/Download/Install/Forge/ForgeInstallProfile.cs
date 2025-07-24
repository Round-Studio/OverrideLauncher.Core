using System.Text.Json;
using System.Text.Json.Serialization;

namespace OverrideLauncher.Core.Base.Entry.Download.Install.Forge;

/// <summary>
/// Forge install_profile.json 数据结构
/// </summary>
public class ForgeInstallProfile
{
    [JsonPropertyName("spec")]
    public int Spec { get; set; }

    [JsonPropertyName("profile")]
    public string Profile { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;

    [JsonPropertyName("json")]
    public string Json { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("logo")]
    public string Logo { get; set; } = string.Empty;

    [JsonPropertyName("minecraft")]
    public string Minecraft { get; set; } = string.Empty;

    [JsonPropertyName("welcome")]
    public string Welcome { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public Dictionary<string, DataEntry> Data { get; set; } = new();

    [JsonPropertyName("processors")]
    public List<Processor> Processors { get; set; } = new();

    [JsonPropertyName("libraries")]
    public List<Library> Libraries { get; set; } = new();

    public class DataEntry
    {
        [JsonPropertyName("client")]
        public string Client { get; set; } = string.Empty;

        [JsonPropertyName("server")]
        public string Server { get; set; } = string.Empty;
    }

    public class Processor
    {
        [JsonPropertyName("sides")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Sides { get; set; }

        [JsonPropertyName("jar")]
        public string Jar { get; set; } = string.Empty;

        [JsonPropertyName("classpath")]
        public List<string> Classpath { get; set; } = new();

        [JsonPropertyName("args")]
        public List<string> Args { get; set; } = new();

        [JsonPropertyName("outputs")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? Outputs { get; set; }
    }

    public class Library
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("downloads")]
        public Downloads Downloads { get; set; } = new();
    }

    public class Downloads
    {
        [JsonPropertyName("artifact")]
        public DownloadInfo? Artifact { get; set; }
    }

    public class DownloadInfo
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("sha1")]
        public string Sha1 { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    public static ForgeInstallProfile? FromJson(string jsonfile)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        return JsonSerializer.Deserialize<ForgeInstallProfile>(File.ReadAllText(jsonfile), options);
    }

    public string ToJson()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(this, options);
    }
}
