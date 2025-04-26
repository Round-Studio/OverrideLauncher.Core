using Newtonsoft.Json;
using System.Collections.Generic;

public class GameJsonEntry
{
    [JsonProperty("minecraftArguments")]
    public string MinecraftArguments { get; set; }
    
    [JsonProperty("arguments")]
    public Arguments Arguments { get; set; }

    [JsonProperty("assetIndex")]
    public AssetIndex AssetIndex { get; set; }

    [JsonProperty("assets")]
    public string Assets { get; set; }

    [JsonProperty("complianceLevel")]
    public int ComplianceLevel { get; set; }

    [JsonProperty("downloads")]
    public Downloads Downloads { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("javaVersion")]
    public JavaVersion JavaVersion { get; set; }

    [JsonProperty("libraries")]
    public List<Library> Libraries { get; set; }

    [JsonProperty("logging")]
    public Logging Logging { get; set; }

    [JsonProperty("mainClass")]
    public string MainClass { get; set; }

    [JsonProperty("minimumLauncherVersion")]
    public int MinimumLauncherVersion { get; set; }

    [JsonProperty("releaseTime")]
    public string ReleaseTime { get; set; }

    [JsonProperty("time")]
    public string Time { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }
}

public class Arguments
{
    [JsonProperty("game")]
    public List<object> Game { get; set; } // Can be string or RuleObject

    [JsonProperty("jvm")]
    public List<object> Jvm { get; set; } // Can be string or RuleObject
}

public class RuleObject
{
    [JsonProperty("rules")]
    public List<Rule> Rules { get; set; }

    [JsonProperty("value")]
    public object Value { get; set; } // Can be string or string[]
}

public class Rule
{
    [JsonProperty("action")]
    public string Action { get; set; }

    [JsonProperty("features", NullValueHandling = NullValueHandling.Ignore)]
    public Features Features { get; set; }

    [JsonProperty("os", NullValueHandling = NullValueHandling.Ignore)]
    public Os Os { get; set; }
}

public class Features
{
    [JsonProperty("is_demo_user", NullValueHandling = NullValueHandling.Ignore)]
    public bool? IsDemoUser { get; set; }

    [JsonProperty("has_custom_resolution", NullValueHandling = NullValueHandling.Ignore)]
    public bool? HasCustomResolution { get; set; }
}

public class Os
{
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
    public string Version { get; set; }

    [JsonProperty("arch", NullValueHandling = NullValueHandling.Ignore)]
    public string Arch { get; set; }
}

public class AssetIndex
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("sha1")]
    public string Sha1 { get; set; }

    [JsonProperty("size")]
    public int Size { get; set; }

    [JsonProperty("totalSize")]
    public int TotalSize { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }
}

public class Downloads
{
    [JsonProperty("client")]
    public DownloadItem Client { get; set; }

    [JsonProperty("server")]
    public DownloadItem Server { get; set; }
}

public class DownloadItem
{
    [JsonProperty("sha1")]
    public string Sha1 { get; set; }

    [JsonProperty("size")]
    public int Size { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }
}

public class JavaVersion
{
    [JsonProperty("component")]
    public string Component { get; set; }

    [JsonProperty("majorVersion")]
    public int MajorVersion { get; set; }
}

public class Library
{
    [JsonProperty("downloads")]
    public LibraryDownloads Downloads { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("natives", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, string> Natives { get; set; }

    [JsonProperty("rules", NullValueHandling = NullValueHandling.Ignore)]
    public List<Rule> Rules { get; set; }

    [JsonProperty("extract", NullValueHandling = NullValueHandling.Ignore)]
    public Extract Extract { get; set; }
}

public class LibraryDownloads
{
    [JsonProperty("artifact", NullValueHandling = NullValueHandling.Ignore)]
    public Artifact Artifact { get; set; }

    [JsonProperty("classifiers", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, Artifact> Classifiers { get; set; }
}

public class Artifact
{
    [JsonProperty("path")]
    public string Path { get; set; }

    [JsonProperty("sha1")]
    public string Sha1 { get; set; }

    [JsonProperty("size")]
    public int Size { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }
}

public class Extract
{
    [JsonProperty("exclude")]
    public List<string> Exclude { get; set; }
}

public class Logging
{
    [JsonProperty("client")]
    public LoggingClient Client { get; set; }
}

public class LoggingClient
{
    [JsonProperty("argument")]
    public string Argument { get; set; }

    [JsonProperty("file")]
    public LoggingFile File { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }
}

public class LoggingFile
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("sha1")]
    public string Sha1 { get; set; }

    [JsonProperty("size")]
    public int Size { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }
}