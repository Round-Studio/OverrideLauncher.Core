using System.Text.Json.Serialization;


namespace OverrideLauncher.Core.Modules.Entry.DownloadEntry;

public class AssetsEntry
{
    public class FileInfo
    {
        [JsonPropertyName("hash")]
        public string Hash { get; set; }
        
        [JsonPropertyName("size")]
        public int Size { get; set; }
    }

    public class RootObject
    {
        [JsonPropertyName("objects")]
        public Dictionary<string, FileInfo> Objects { get; set; }
    }
}