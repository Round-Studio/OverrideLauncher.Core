using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class FabricLoaderVersion
{
    [JsonProperty("loader")]
    public FabricLoaderVersionInfo Loader { get; set; }
    
    [JsonProperty("intermediary")]
    public FabricIntermediaryVersion Intermediary { get; set; }
    
    [JsonProperty("launcherMeta")]
    public FabricLauncherMeta LauncherMeta { get; set; }
}
public class FabricLoaderVersionInfo
{
    [JsonProperty("separator")]
    public string Separator { get; set; }

    [JsonProperty("build")]
    public int Build { get; set; }

    [JsonProperty("maven")]
    public string Maven { get; set; }

    [JsonProperty("version")]
    public string Version { get; set; }

    [JsonProperty("stable")]
    public bool Stable { get; set; }
}

public class FabricIntermediaryVersion
{
    [JsonProperty("maven")]
    public string Maven { get; set; }

    [JsonProperty("version")]
    public string Version { get; set; }

    [JsonProperty("stable")]
    public bool Stable { get; set; }
}

public class FabricLauncherMeta
{
    [JsonProperty("version")]
    public string Version { get; set; }

    [JsonProperty("libraries")]
    public Libraries Libraries { get; set; }

    [JsonProperty("mainClass")]
    public dynamic MainClass { get; set; }
}

public class Libraries
{
    [JsonProperty("client")]
    public object[] Client { get; set; }
    
    [JsonProperty("common")]
    public Common[] Ccommon { get; set; }
}

public class Common
{
    [JsonProperty("Name")]
    public string name { get; set; }
    
    [JsonProperty("Url")]
    public string url { get; set; }
    
    [JsonProperty("Size")]
    public int size { get; set; }
}

public class MainClass
{
    [JsonProperty("client")]
    public string Client { get; set; }

    [JsonProperty("server")]
    public string Server { get; set; }
}

public class FabricGameVersion
{
    [JsonProperty("version")]
    public string Version { get; set; }

    [JsonProperty("stable")]
    public bool Stable { get; set; }
}