using System.Text.Json.Serialization;

namespace OverrideLauncher.Core.Base.Entry.Download.Install.Manifest;

public class ManifestMojang
{
    [JsonPropertyName("latest")]
    public LatestVersionInfo Latest { get; set; }
    
    [JsonPropertyName("versions")]
    public List<ManifestVersion> Versions { get; set; }

    public class LatestVersionInfo
    {
        [JsonPropertyName("release")]
        public string Release { get; set; }
        [JsonPropertyName("snapshot")]
        public string Snapshot { get; set; }
    }

    public class ManifestVersion
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; } // "release" or "snapshot"
        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("time")]
        public DateTime Time { get; set; }
        [JsonPropertyName("releaseTime")]
        public DateTime ReleaseTime { get; set; }
    }
}