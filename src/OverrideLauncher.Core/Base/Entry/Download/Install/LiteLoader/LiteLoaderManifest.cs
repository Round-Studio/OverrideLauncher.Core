using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OverrideLauncher.Core.Base.Entry.Download.Install.LiteLoader
{
    public class LiteLoaderManifest
    {
        [JsonPropertyName("meta")]
        public MetaData Meta { get; set; }

        [JsonPropertyName("versions")]
        public Dictionary<string, VersionData> Versions { get; set; }

        public class MetaData
        {
            [JsonPropertyName("description")]
            public string Description { get; set; }

            [JsonPropertyName("authors")]
            public string Authors { get; set; }

            [JsonPropertyName("url")]
            public string Url { get; set; }

            [JsonPropertyName("updated")]
            public string Updated { get; set; }

            [JsonPropertyName("updatedTime")]
            public long UpdatedTime { get; set; }
        }

        public class Library
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("url")]
            public string Url { get; set; }
        }

        public class Artefact
        {
            [JsonPropertyName("tweakClass")]
            public string TweakClass { get; set; }

            [JsonPropertyName("libraries")]
            public List<Library> Libraries { get; set; }

            [JsonPropertyName("stream")]
            public string Stream { get; set; }

            [JsonPropertyName("file")]
            public string File { get; set; }

            [JsonPropertyName("version")]
            public string Version { get; set; }

            [JsonPropertyName("build")]
            public string Build { get; set; }

            [JsonPropertyName("md5")]
            public string Md5 { get; set; }

            [JsonPropertyName("timestamp")]
            public string Timestamp { get; set; }

            [JsonPropertyName("lastSuccessfulBuild")]
            public int? LastSuccessfulBuild { get; set; }

            [JsonPropertyName("srcJar")]
            public string SrcJar { get; set; }

            [JsonPropertyName("mcpJar")]
            public string McpJar { get; set; }
        }

        public class Artefacts
        {
            [JsonPropertyName("com.mumfrey:liteloader")]
            public Dictionary<string, Artefact> LiteLoader { get; set; }
        }

        public class Snapshots
        {
            [JsonPropertyName("libraries")]
            public List<Library> Libraries { get; set; }

            [JsonPropertyName("com.mumfrey:liteloader")]
            public Dictionary<string, Artefact> LiteLoader { get; set; }
        }

        public class Dev
        {
            [JsonPropertyName("fgVersion")]
            public string FgVersion { get; set; }

            [JsonPropertyName("mappings")]
            public string Mappings { get; set; }

            [JsonPropertyName("mcp")]
            public string Mcp { get; set; }
        }

        public class Repo
        {
            [JsonPropertyName("stream")]
            public string Stream { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("url")]
            public string Url { get; set; }

            [JsonPropertyName("classifier")]
            public string Classifier { get; set; }
        }

        public class VersionData
        {
            [JsonPropertyName("repo")]
            public Repo Repo { get; set; }

            [JsonPropertyName("artefacts")]
            public Artefacts Artefacts { get; set; }

            [JsonPropertyName("snapshots")]
            public Snapshots Snapshots { get; set; }

            [JsonPropertyName("dev")]
            public Dev Dev { get; set; }
        }
    }
}