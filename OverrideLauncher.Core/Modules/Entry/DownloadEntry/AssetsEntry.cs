using Newtonsoft.Json;

namespace OverrideLauncher.Core.Modules.Entry.DownloadEntry;

public class AssetsEntry
{
    public class FileInfo
    {
        [JsonProperty("hash")]
        public string Hash { get; set; }
        
        [JsonProperty("size")]
        public int Size { get; set; }
    }

    public class RootObject
    {
        [JsonProperty("objects")]
        public Dictionary<string, FileInfo> Objects { get; set; }
    }
}