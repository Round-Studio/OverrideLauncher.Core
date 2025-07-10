using System.Text.Json.Serialization;

namespace OverrideLauncher.Core.Base.Entry.Download.Install.Manifest;

public class ManifestClientAssetsJson
{
    [JsonPropertyName("objects")]
    public Dictionary<string, FileInfo> Objects { get; set; }
    public class FileInfo
    {
        [JsonPropertyName("hash")]
        public string Hash { get; set; }
        
        [JsonPropertyName("size")]
        public int Size { get; set; }
    }
}