using System.Text.Json.Serialization;

namespace OverrideLauncher.Core.Base.Entry.Download.Install.Fabric;

public class FabricApiEntry
{
    public class FileInfo
    {
        [JsonPropertyName("hashes")]
        public Dictionary<string, string> Hashes { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("filename")]
        public string Filename { get; set; }

        [JsonPropertyName("primary")]
        public bool Primary { get; set; }

        [JsonPropertyName("size")]
        public int Size { get; set; }
    }

    public class Dependency
    {
        [JsonPropertyName("version_id")]
        public string VersionId { get; set; }

        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; }

        [JsonPropertyName("file_name")]
        public string FileName { get; set; }

        [JsonPropertyName("dependency_type")]
        public string DependencyType { get; set; }
    }

    public class FabricApiVersion
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("version_number")]
        public string VersionNumber { get; set; }

        [JsonPropertyName("changelog")]
        public string Changelog { get; set; }

        [JsonPropertyName("game_versions")]
        public List<string> GameVersions { get; set; }

        [JsonPropertyName("version_type")]
        public string VersionType { get; set; }

        [JsonPropertyName("loaders")]
        public List<string> Loaders { get; set; }

        [JsonPropertyName("featured")]
        public bool Featured { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; }

        [JsonPropertyName("author_id")]
        public string AuthorId { get; set; }

        [JsonPropertyName("date_published")]
        public DateTime DatePublished { get; set; }

        [JsonPropertyName("downloads")]
        public int Downloads { get; set; }

        [JsonPropertyName("files")]
        public List<FileInfo> Files { get; set; }

        [JsonPropertyName("dependencies")]
        public List<Dependency> Dependencies { get; set; }
    }
}