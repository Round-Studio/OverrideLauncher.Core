using OverrideLauncher.Core.Base.Dictionary;
using OverrideLauncher.Core.Base.Entry.Download.Install;
using OverrideLauncher.Core.Base.Entry.Download.Install.Client;
using OverrideLauncher.Core.Base.Entry.Download.Install.LiteLoader;
using OverrideLauncher.Core.Base.Entry.Download.Install.Manifest;
using OverrideLauncher.Core.Classes.Install.Manifest;
using OverrideLauncher.Core.Interface.Download;
using OverrideLauncher.Core.Base.Entry.Download;
using OverrideLauncher.Core.Base.Enum;
using OverrideLauncher.Core.Base.Enum.Download;

namespace OverrideLauncher.Core.Classes.Install.Installer;

public class InstallerLiteLoader : IDownload
{
    private readonly LiteLoaderManifest.VersionData _versionData;
    private ClientRootInfo _rootInfo { get; set; }
    public InstallerLiteLoader(LiteLoaderManifest.VersionData versionData)
    {
        if (versionData == null) throw new NullReferenceException();
        _versionData = versionData;
    }

    public async Task Install(ClientRootInfo rootInfo)
    {
        _rootInfo = rootInfo;
        var valjson = InstallHelper.GetClientJsonEntry(rootInfo);
        // Get version from snapshots or artefacts
        string liteLoaderVersion = null;
        if (_versionData.Snapshots?.LiteLoader?.ContainsKey("latest") == true)
        {
            liteLoaderVersion = _versionData.Snapshots.LiteLoader["latest"].Version;
        }
        else if (_versionData.Artefacts?.LiteLoader?.ContainsKey("latest") == true)
        {
            liteLoaderVersion = _versionData.Artefacts.LiteLoader["latest"].Version;
        }

        valjson.ModLoader.Add(new ModLoaderInfo()
        {
            Name = "liteloader",
            Version = liteLoaderVersion ?? _versionData.Dev?.FgVersion ?? "unknown"
        });

        valjson.MainClass = "net.minecraft.launchwrapper.Launch";
        if (valjson.Arguments == null) valjson.Arguments = new();
        if (valjson.Arguments.Game == null) valjson.Arguments.Game = new();

        // Get tweakClass from the artifact data
        string tweakClass = "com.mumfrey.liteloader.launch.LiteLoaderTweaker"; // default fallback
        if (_versionData.Snapshots?.LiteLoader?.ContainsKey("latest") == true)
        {
            tweakClass = _versionData.Snapshots.LiteLoader["latest"].TweakClass ?? tweakClass;
        }
        else if (_versionData.Artefacts?.LiteLoader?.ContainsKey("latest") == true)
        {
            tweakClass = _versionData.Artefacts.LiteLoader["latest"].TweakClass ?? tweakClass;
        }

        valjson.Arguments.Game.Add("--tweakClass");
        valjson.Arguments.Game.Add(tweakClass);

        // Get libraries from snapshots.com.mumfrey:liteloader.latest.libraries
        if (_versionData.Snapshots?.LiteLoader?.ContainsKey("latest") == true)
        {
            var latestSnapshot = _versionData.Snapshots.LiteLoader["latest"];
            if (latestSnapshot.Libraries != null)
            {
                latestSnapshot.Libraries.ForEach(x =>
                {
                    var url = string.IsNullOrEmpty(x.Url) ? DictionaryDownloadHost.Sources.LibrariesHost : x.Url;
                    valjson.Libraries.Add(new ManifestClientJson.Library()
                    {
                        Name = x.Name,
                        Url = url
                    });
                });
            }

            // Add the LiteLoader jar itself
            valjson.Libraries.Add(new ManifestClientJson.Library()
            {
                Name = $"com.mumfrey:liteloader:{latestSnapshot.Version}",
                Url = _versionData.Repo?.Url ?? "https://dl.liteloader.com/versions/"
            });
        }
        // Fallback to artefacts if snapshots are not available
        else if (_versionData.Artefacts?.LiteLoader?.ContainsKey("latest") == true)
        {
            var latestArtefact = _versionData.Artefacts.LiteLoader["latest"];
            if (latestArtefact.Libraries != null)
            {
                latestArtefact.Libraries.ForEach(x =>
                {
                    var url = string.IsNullOrEmpty(x.Url) ? DictionaryDownloadHost.Sources.LibrariesHost : x.Url;
                    valjson.Libraries.Add(new ManifestClientJson.Library()
                    {
                        Name = x.Name,
                        Url = url
                    });
                });
            }

            // Add the LiteLoader jar itself
            valjson.Libraries.Add(new ManifestClientJson.Library()
            {
                Name = $"com.mumfrey:liteloader:{latestArtefact.Version}",
                Url = _versionData.Repo?.Url ?? "https://dl.liteloader.com/versions/"
            });
        }

        InstallHelper.SaveClientJson(valjson, _rootInfo);

        // 准备下载文件并执行下载
        PrepareFiles(valjson.Libraries.Where(lib => lib.Name.Contains("liteloader") ||
                                                   lib.Name.Contains("launchwrapper") ||
                                                   lib.Name.Contains("asm-all")).ToList());
        FileCount = (ulong)(downloadList.Files.Count > 0 ? downloadList.Files.Count - 1 : 0);
        if (downloadList.Files.Count > 0)
        {
            await Download();
        }
    }

    /// <summary>
    /// 将 Maven 坐标转换为文件路径
    /// </summary>
    /// <param name="mavenCoordinate">Maven 坐标，格式：group:artifact:version</param>
    /// <returns>相对于 libraries 目录的文件路径</returns>
    private string MavenCoordinateToPath(string mavenCoordinate)
    {
        var parts = mavenCoordinate.Split(':');
        if (parts.Length < 3)
        {
            throw new ArgumentException($"Invalid Maven coordinate: {mavenCoordinate}");
        }

        var group = parts[0].Replace('.', '/');
        var artifact = parts[1];
        var version = parts[2];
        var fileName = $"{artifact}-{version}.jar";

        return Path.Combine(group, artifact, version, fileName);
    }

    /// <summary>
    /// 准备下载文件列表
    /// </summary>
    /// <param name="liteLoaderLibraries">LiteLoader 库列表</param>
    private void PrepareFiles(List<ManifestClientJson.Library> liteLoaderLibraries)
    {
        foreach (var library in liteLoaderLibraries)
        {
            var artifactPath = MavenCoordinateToPath(library.Name);
            var fullUrl = $"{library.Url.TrimEnd('/')}/{artifactPath}";
            var fileName = Path.Combine(_rootInfo.ClientRootPath, DictionaryGameRoot.LibrariesPath, artifactPath);

            downloadList.Files.Add(new DownloadListEntry.DownloadFileItem()
            {
                Type = FileType.JarFile,
                FileInfo = new DownloadFileInfo()
                {
                    FileName = fileName,
                    Url = fullUrl,
                    Size = library.Size,
                    Hash = "" // LiteLoader 库通常没有提供 hash
                }
            });
        }
    }
}