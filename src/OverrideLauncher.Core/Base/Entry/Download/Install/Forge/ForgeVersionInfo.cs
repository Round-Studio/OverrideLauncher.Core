using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OverrideLauncher.Core.Base.Entry.Download.Install.Forge;

public class ForgeVersionInfo
{
    [JsonPropertyName("_comment")]
    public List<string>? Comments { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public DateTime Time { get; set; }

    [JsonPropertyName("releaseTime")]
    public DateTime ReleaseTime { get; set; }

    [JsonPropertyName("inheritsFrom")]
    public string InheritsFrom { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("logging")]
    public Dictionary<string, object> Logging { get; set; } = new Dictionary<string, object>();

    [JsonPropertyName("mainClass")]
    public string MainClass { get; set; } = string.Empty;

    [JsonPropertyName("libraries")]
    public List<Library> Libraries { get; set; } = new List<Library>();

    [JsonPropertyName("arguments")]
    public ArgumentsEntry Arguments { get; set; } = new ArgumentsEntry();

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

    public class Downloads
    {
        [JsonPropertyName("artifact")]
        public DownloadInfo? Artifact { get; set; }
    }

    public class Library
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("downloads")]
        public Downloads Downloads { get; set; } = new Downloads();

        [JsonPropertyName("rules")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<Rule>? Rules { get; set; }
    }

    public class Rule
    {
        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("os")]
        public OsInfo? Os { get; set; }
    }

    public class OsInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class ArgumentsEntry
    {
        [JsonPropertyName("game")]
        public List<JsonElement> Game { get; set; } = new List<JsonElement>();

        [JsonPropertyName("jvm")]
        public List<JsonElement> Jvm { get; set; } = new List<JsonElement>();
    }

    public static ForgeVersionInfo? FromJson(string jsonfile)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        return JsonSerializer.Deserialize<ForgeVersionInfo>(File.ReadAllText(jsonfile), options);
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