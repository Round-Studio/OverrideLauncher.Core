using System.Text.Json;
using OverrideLauncher.Core.Base.Dictionary;
using OverrideLauncher.Core.Base.Entry.Download;
using OverrideLauncher.Core.Base.Entry.Download.Install.Client;
using OverrideLauncher.Core.Base.Entry.Download.Install.Fabric;
using OverrideLauncher.Core.Base.Entry.Download.Install.Manifest;
using OverrideLauncher.Core.Classes.Install.Manifest;
using OverrideLauncher.Core.Interface.Download;
using OverrideLauncher.Core.Base.Entry.Download.Install;
using OverrideLauncher.Core.Base.Enum;
using OverrideLauncher.Core.Base.Enum.Download;

namespace OverrideLauncher.Core.Classes.Install.Installer;

public class InstallerFabric : IDownload
{
    public FabricApiEntry.FabricApiVersion FabricApiVersion { get; set; } = null;
    
    private ManifestClientJson? ManifestClientJson { get; set; }
    private FabricLoaderVersion _fabricProfile;
    private ClientRootInfo ClientRootInfo { get; set; }

    public InstallerFabric(FabricLoaderVersion fabricProfile)
    {
        _fabricProfile = fabricProfile;
    }

    public async Task Install(ClientRootInfo rootInfo)
    {
        ClientRootInfo = rootInfo;
        ManifestClientJson = InstallHelper.GetClientJsonEntry(rootInfo);

        ManifestClientJson.ModLoader.Add(new ModLoaderInfo()
        {
            Name = "fabric",
            Version = _fabricProfile.Loader.Version
        });

        var lst = new List<ManifestClientJson.Library>();

        lst.Add(new ManifestClientJson.Library()
                  {
                      Url = DictionaryDownloadHost.Sources.FabricResourceHost,
                      Name = _fabricProfile.Intermediary.Maven
                  });
        lst.Add(new ManifestClientJson.Library()
        {
            Url = DictionaryDownloadHost.Sources.FabricResourceHost,
            Name = _fabricProfile.Loader.Maven
        });
        _fabricProfile.LauncherMeta.Libraries.Common.ToList().ForEach(x =>
        {
            lst.Add(new ManifestClientJson.Library()
            {
                Url = x.Url,
                Name = x.Name,
                Size = (ulong)x.Size
            });
        });

        ManifestClientJson.Libraries.AddRange(lst);

        if (FabricApiVersion != null)
        {
            FabricApiVersion.Files.ForEach(x =>
            {
                downloadList.Files.Add(new DownloadListEntry.DownloadFileItem()
                {
                    Type = FileType.JarFile,
                    FileInfo = new DownloadFileInfo()
                    {
                        Url = x.Url,
                        FileName = Path.Combine(rootInfo.ClientRootPath,
                            DictionaryGameRoot.VersionsPath,
                            rootInfo.ClientName, DictionaryGameRoot.ModsPath, x.Filename),
                        Hash = x.Hashes["sha1"],
                        Size = (ulong)x.Size
                    }
                });
            });
        }

        if (_fabricProfile.LauncherMeta.MainClass is string mastr)
        {
            ManifestClientJson.MainClass = mastr;
        }
        else
        {
            var mainClass = JsonSerializer.Deserialize<MainClass>(_fabricProfile.LauncherMeta.MainClass.ToString());
            ManifestClientJson.MainClass = mainClass.Client;
        }

        InstallHelper.SaveClientJson(ManifestClientJson, rootInfo);

        // 准备下载文件并执行下载
        PrepareFiles(lst);
        FileCount = (ulong)(downloadList.Files.Count - 1);
        await Download();
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
    /// <param name="fabricLibraries">Fabric 库列表</param>
    private void PrepareFiles(List<ManifestClientJson.Library> fabricLibraries)
    {
        foreach (var library in fabricLibraries)
        {
            var artifactPath = MavenCoordinateToPath(library.Name);
            var fullUrl = $"{library.Url}/{artifactPath}";
            var fileName = Path.Combine(ClientRootInfo.ClientRootPath, DictionaryGameRoot.LibrariesPath, artifactPath);

            downloadList.Files.Add(new DownloadListEntry.DownloadFileItem()
            {
                Type = FileType.JarFile,
                FileInfo = new()
                {
                    FileName = fileName,
                    Url = fullUrl,
                    Size = library.Size,
                    Hash = "" // Fabric 库通常没有提供 hash
                }
            });
        }
    }
}