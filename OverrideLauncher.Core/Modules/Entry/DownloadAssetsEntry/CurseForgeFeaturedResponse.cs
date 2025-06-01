using System.Text.Json.Serialization;

namespace OverrideLauncher.Core.Modules.Entry.DownloadEntry.DownloadAssetsEntry;

public class CurseForgeFeaturedResponse
{
    [JsonPropertyName("data")]
    public FeaturedData Data { get; set; }
}

public class FeaturedData
{
    [JsonPropertyName("featured")]
    public List<ModInfo> Featured { get; set; }
}