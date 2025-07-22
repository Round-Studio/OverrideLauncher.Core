using System.Text.Json.Serialization;

namespace OverrideLauncher.Core.Base.Entry.Download.Install.Fabric;

public class FabricLoaderVersion
{
    [JsonPropertyName("loader")]
    public FabricLoaderVersionInfo Loader { get; set; }
    
    [JsonPropertyName("intermediary")]
    public FabricIntermediaryVersion Intermediary { get; set; }
    
    [JsonPropertyName("launcherMeta")]
    public FabricLauncherMeta LauncherMeta { get; set; }
}
public class FabricLoaderVersionInfo
{
    [JsonPropertyName("separator")]
    public string Separator { get; set; }

    [JsonPropertyName("build")]
    public int Build { get; set; }

    [JsonPropertyName("maven")]
    public string Maven { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("stable")]
    public bool Stable { get; set; }
}

public class FabricIntermediaryVersion
{
    [JsonPropertyName("maven")]
    public string Maven { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("stable")]
    public bool Stable { get; set; }
}

public class FabricLauncherMeta
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("libraries")]
    public Libraries Libraries { get; set; }

    [JsonPropertyName("mainClass")]
    public dynamic MainClass { get; set; }
}

public class Libraries
{
    [JsonPropertyName("client")]
    public object[] Client { get; set; }
    
    [JsonPropertyName("common")]
    public List<Common> Common { get; set; }
}

public class Common
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("url")]
    public string Url { get; set; }
    
    [JsonPropertyName("size")]
    public int Size { get; set; }
}

public class MainClass
{
    [JsonPropertyName("client")]
    public string Client { get; set; }

    [JsonPropertyName("server")]
    public string Server { get; set; }
}

public class FabricGameVersion
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("stable")]
    public bool Stable { get; set; }
}