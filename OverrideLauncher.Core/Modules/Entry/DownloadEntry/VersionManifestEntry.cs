using Newtonsoft.Json;

namespace OverrideLauncher.Core.Modules.Entry.DownloadEntry;

public class VersionManifestEntry
{
    public class VersionManifest
    {
        [JsonProperty("latest")]
        public Latest Latest { get; set; }

        [JsonProperty("versions")]
        public Version[] Versions { get; set; }
    }

    public class Latest
    {
        [JsonProperty("release")]
        public string Release { get; set; }

        [JsonProperty("snapshot")]
        public string Snapshot { get; set; }
    }

    public class Version
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("time")]
        public string Time { get; set; }

        [JsonProperty("releaseTime")]
        public string ReleaseTime { get; set; }
    }
}