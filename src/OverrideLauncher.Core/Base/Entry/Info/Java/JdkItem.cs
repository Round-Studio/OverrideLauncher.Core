using System.Text.Json.Serialization;

namespace OverrideLauncher.Core.Base.Entry.Info.Java;

public class JdkItem
{
    [JsonPropertyName("jdks")]
    public List<Jdk> Jdks { get; set; }
    
    public class Jdk
    {
        [JsonPropertyName("vendor")] public string Vendor { get; set; }

        [JsonPropertyName("product")] public string Product { get; set; }

        [JsonPropertyName("default")] public bool Default { get; set; }

        [JsonPropertyName("jdk_version_major")]
        public int JdkVersionMajor { get; set; }

        [JsonPropertyName("jdk_version")] public string JdkVersion { get; set; }

        [JsonPropertyName("suggested_sdk_name")]
        public string SuggestedSdkName { get; set; }

        [JsonPropertyName("shared_index_aliases")]
        public List<string> SharedIndexAliases { get; set; }

        [JsonPropertyName("packages")] public List<Package> Packages { get; set; }
    }

    public class Package
    {
        [JsonPropertyName("os")] public string Os { get; set; }

        [JsonPropertyName("arch")] public string Arch { get; set; }

        [JsonPropertyName("version")] public string Version { get; set; }

        [JsonPropertyName("url")] public string Url { get; set; }

        [JsonPropertyName("package_type")] public string PackageType { get; set; }

        [JsonPropertyName("unpack_prefix_filter")]
        public string UnpackPrefixFilter { get; set; }

        [JsonPropertyName("package_root_prefix")]
        public string PackageRootPrefix { get; set; }

        [JsonPropertyName("package_to_java_home_prefix")]
        public string PackageToJavaHomePrefix { get; set; }

        [JsonPropertyName("archive_file_name")]
        public string ArchiveFileName { get; set; }

        [JsonPropertyName("install_folder_name")]
        public string InstallFolderName { get; set; }

        [JsonPropertyName("archive_size")] public long ArchiveSize { get; set; }

        [JsonPropertyName("unpacked_size")] public long UnpackedSize { get; set; }

        [JsonPropertyName("sha256")] public string Sha256 { get; set; }

        [JsonPropertyName("filter")] public Filter Filter { get; set; }
    }

    public class Filter
    {
        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonPropertyName("arch")] public string Arch { get; set; }
    }
}