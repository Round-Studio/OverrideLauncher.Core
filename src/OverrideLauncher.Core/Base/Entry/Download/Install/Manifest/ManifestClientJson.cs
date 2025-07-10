using System.Text.Json.Serialization;

namespace OverrideLauncher.Core.Base.Entry.Download.Install.Manifest;

public class ManifestClientJson
{
    [JsonPropertyName("minecraftArguments")] public string MinecraftArguments { get; set; }

    [JsonPropertyName("arguments")] public ArgumentsEntry Arguments { get; set; }

    [JsonPropertyName("assetIndex")] public AssetIndexEntry AssetIndex { get; set; }

    [JsonPropertyName("assets")] public string Assets { get; set; }

    [JsonPropertyName("complianceLevel")] public int ComplianceLevel { get; set; }

    [JsonPropertyName("downloads")] public DownloadsEntry Downloads { get; set; }

    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("javaVersion")] public JavaVersionEntry JavaVersion { get; set; }

    [JsonPropertyName("libraries")] public List<Library> Libraries { get; set; }

    [JsonPropertyName("logging")] public LoggingEntry Logging { get; set; }

    [JsonPropertyName("mainClass")] public string MainClass { get; set; }

    [JsonPropertyName("minimumLauncherVersion")] public int MinimumLauncherVersion { get; set; }

    [JsonPropertyName("releaseTime")] public string ReleaseTime { get; set; }

    [JsonPropertyName("time")] public string Time { get; set; }

    [JsonPropertyName("type")] public string Type { get; set; }

    public class ArgumentsEntry
    {
        [JsonPropertyName("game")] public List<object> Game { get; set; } // Can be string or RuleObject

        [JsonPropertyName("jvm")] public List<object> Jvm { get; set; } // Can be string or RuleObject
    }

    public class RuleObject
    {
        [JsonPropertyName("rules")] public List<Rule> Rules { get; set; }

        [JsonPropertyName("value")] public object Value { get; set; } // Can be string or string[]
    }

    public class Rule
    {
        [JsonPropertyName("action")] public string Action { get; set; }

        [JsonPropertyName("features")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Features Features { get; set; }

        [JsonPropertyName("os")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Os Os { get; set; }
    }

    public class Features
    {
        [JsonPropertyName("is_demo_user")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsDemoUser { get; set; }

        [JsonPropertyName("has_custom_resolution")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? HasCustomResolution { get; set; }
    }

    public class Os
    {
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Name { get; set; }

        [JsonPropertyName("version")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Version { get; set; }

        [JsonPropertyName("arch")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Arch { get; set; }
    }

    public class AssetIndexEntry
    {
        [JsonPropertyName("id")] public string Id { get; set; }

        [JsonPropertyName("sha1")] public string Sha1 { get; set; }

        [JsonPropertyName("size")] public int Size { get; set; }

        [JsonPropertyName("totalSize")] public int TotalSize { get; set; }

        [JsonPropertyName("url")] public string Url { get; set; }
    }

    public class DownloadsEntry
    {
        [JsonPropertyName("client")] public DownloadItem Client { get; set; }

        [JsonPropertyName("server")] public DownloadItem Server { get; set; }
    }

    public class DownloadItem
    {
        [JsonPropertyName("sha1")] public string Sha1 { get; set; }

        [JsonPropertyName("size")] public int Size { get; set; }

        [JsonPropertyName("url")] public string Url { get; set; }
    }

    public class JavaVersionEntry
    {
        [JsonPropertyName("component")] public string Component { get; set; }

        [JsonPropertyName("majorVersion")] public int MajorVersion { get; set; }
    }

    public class Library
    {
        [JsonPropertyName("downloads")] public LibraryDownloads Downloads { get; set; }

        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("url")] public string Url { get; set; }

        [JsonPropertyName("natives")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string> Natives { get; set; }

        [JsonPropertyName("rules")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<Rule> Rules { get; set; }

        [JsonPropertyName("extract")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Extract Extract { get; set; }
    }

    public class LibraryDownloads
    {
        [JsonPropertyName("artifact")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Artifact Artifact { get; set; }

        [JsonPropertyName("classifiers")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, Artifact> Classifiers { get; set; }
    }

    public class Artifact
    {
        [JsonPropertyName("path")] public string Path { get; set; }

        [JsonPropertyName("sha1")] public string Sha1 { get; set; }

        [JsonPropertyName("size")] public int Size { get; set; }

        [JsonPropertyName("url")] public string Url { get; set; }
    }

    public class Extract
    {
        [JsonPropertyName("exclude")] public List<string> Exclude { get; set; }
    }

    public class LoggingEntry
    {
        [JsonPropertyName("client")] public LoggingClient Client { get; set; }
    }

    public class LoggingClient
    {
        [JsonPropertyName("argument")] public string Argument { get; set; }

        [JsonPropertyName("file")] public LoggingFile File { get; set; }

        [JsonPropertyName("type")] public string Type { get; set; }
    }

    public class LoggingFile
    {
        [JsonPropertyName("id")] public string Id { get; set; }

        [JsonPropertyName("sha1")] public string Sha1 { get; set; }

        [JsonPropertyName("size")] public int Size { get; set; }

        [JsonPropertyName("url")] public string Url { get; set; }
    }
}