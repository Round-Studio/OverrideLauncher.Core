using System.Text.Json.Serialization;

namespace OverrideLauncher.Core.Entry.Download.Install.Fabric;

/// <summary>
/// Fabric Loader 版本信息
/// </summary>
public class FabricLoaderVersion
{
    [JsonPropertyName("separator")]
    public string Separator { get; set; } = "";

    [JsonPropertyName("build")]
    public int Build { get; set; }

    [JsonPropertyName("maven")]
    public string Maven { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("stable")]
    public bool Stable { get; set; }
}

/// <summary>
/// Fabric 安装配置文件
/// </summary>
public class FabricProfile
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("inheritsFrom")]
    public string InheritsFrom { get; set; } = "";

    [JsonPropertyName("releaseTime")]
    public DateTime ReleaseTime { get; set; }

    [JsonPropertyName("time")]
    public DateTime Time { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "release";

    [JsonPropertyName("mainClass")]
    public string MainClass { get; set; } = "";

    [JsonPropertyName("arguments")]
    public FabricArguments? Arguments { get; set; }

    [JsonPropertyName("libraries")]
    public List<FabricLibrary>? Libraries { get; set; }
}

/// <summary>
/// Fabric 启动参数
/// </summary>
public class FabricArguments
{
    [JsonPropertyName("game")]
    public List<object>? Game { get; set; }

    [JsonPropertyName("jvm")]
    public List<object>? Jvm { get; set; }
}

/// <summary>
/// Fabric 库文件信息
/// </summary>
public class FabricLibrary
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("sha1")]
    public string? Sha1 { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("downloads")]
    public FabricDownloads? Downloads { get; set; }
}

/// <summary>
/// Fabric 下载信息
/// </summary>
public class FabricDownloads
{
    [JsonPropertyName("artifact")]
    public FabricArtifact? Artifact { get; set; }
}

/// <summary>
/// Fabric 构件信息
/// </summary>
public class FabricArtifact
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("sha1")]
    public string Sha1 { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }
}

/// <summary>
/// Fabric API 版本信息
/// </summary>
public class FabricApiVersion
{
    [JsonPropertyName("version_number")]
    public string VersionNumber { get; set; } = "";

    [JsonPropertyName("game_versions")]
    public List<string> GameVersions { get; set; } = new();

    [JsonPropertyName("version_type")]
    public string VersionType { get; set; } = "";

    [JsonPropertyName("loaders")]
    public List<string> Loaders { get; set; } = new();

    [JsonPropertyName("featured")]
    public bool Featured { get; set; }

    [JsonPropertyName("files")]
    public List<FabricApiFile> Files { get; set; } = new();
}

/// <summary>
/// Fabric API 文件信息
/// </summary>
public class FabricApiFile
{
    [JsonPropertyName("hashes")]
    public Dictionary<string, string> Hashes { get; set; } = new();

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("filename")]
    public string Filename { get; set; } = "";

    [JsonPropertyName("primary")]
    public bool Primary { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }
}
