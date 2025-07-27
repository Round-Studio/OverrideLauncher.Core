using System.Text.Json;
using System.Text.Json.Serialization;
using OverrideLauncher.Core.Base.Dictionary;
using OverrideLauncher.Core.Base.Entry.Download.Install;
using OverrideLauncher.Core.Base.Entry.Download.Install.Client;
using OverrideLauncher.Core.Base.Entry.Download.Install.Manifest;
using OverrideLauncher.Core.Base.Enum;
using OverrideLauncher.Core.Base.Enum.Download;
using OverrideLauncher.Core.Classes.Install.Manifest;
using OverrideLauncher.Core.Interface.Download;

namespace OverrideLauncher.Core.Classes.Install;

public class InstallClient : IDownload
{
    #region Public
    public InstallClient(ManifestMojang.ManifestVersion manifestVersion)
    {
        _installName = manifestVersion.Id;
        ManifestClientJson = InstallHelper.TryingGetClientJson(manifestVersion).Result;
        ManifestClientAssetsJson = InstallHelper.TryingGetClientAssetsJson(ManifestClientJson).Result;
    }

    public async Task Install(ClientRootInfo rootInfo)
    {
        ClientRootInfo = rootInfo;
        if(!string.IsNullOrEmpty(rootInfo.InstallName)) _installName = rootInfo.InstallName;
        
        SaveJson();
        PrepareFiles();

        FileCount = (ulong)(downloadList.Files.Count - 1);
        await Download();
    }

    #endregion
    
    #region Private
    private string _installName { get; set; }
    private ClientRootInfo ClientRootInfo { get; set; }
    private ManifestClientAssetsJson ManifestClientAssetsJson { get; set; }
    private ManifestClientJson ManifestClientJson { get; set; }
    private void SaveJson()
    {
        var versionJsonPath = Path.Combine(ClientRootInfo.InstallPath, DictionaryGameRoot.VersionsPath,
            _installName, $"{_installName}.json");
        var assetsJsonPath = Path.Combine(ClientRootInfo.InstallPath, DictionaryGameRoot.AssetsIndexPath,
            $"{ManifestClientJson.AssetIndex.Id}.json");
        
        Directory.CreateDirectory(Path.GetDirectoryName(versionJsonPath));
        Directory.CreateDirectory(Path.GetDirectoryName(assetsJsonPath));
        
        File.WriteAllText(versionJsonPath,JsonSerializer.Serialize(ManifestClientJson));
        File.WriteAllText(assetsJsonPath,JsonSerializer.Serialize(ManifestClientAssetsJson));
    }

    private void PrepareFiles()
    {
        downloadList.Files.Add(new DownloadListEntry.DownloadFileItem()
        {
            Type = FileType.BaseGame,
            FileInfo = new()
            {
                Size = (ulong)ManifestClientJson.Downloads.Client.Size,
                Url = ManifestClientJson.Downloads.Client.Url,
                FileName = Path.Combine(ClientRootInfo.InstallPath, DictionaryGameRoot.VersionsPath, _installName,
                    $"{_installName}.jar")
            }
        }); // 添加本体文件

        downloadList.Files.AddRange(GetArtifacts()); // 添加所有 Artifact 文件
        downloadList.Files.AddRange(GetAssets()); // 添加所有 Assets 文件
    }
    
    private List<DownloadListEntry.DownloadFileItem> GetArtifacts()
    {
        var list = new List<DownloadListEntry.DownloadFileItem>();
        ManifestClientJson.Libraries.ForEach(x =>
        {
            if (x.Downloads != null && x.Downloads.Artifact != null)
            {
                list.Add(new DownloadListEntry.DownloadFileItem()
                {
                    Type = FileType.JarFile,
                    FileInfo = new()
                    {
                        FileName = Path.Combine(ClientRootInfo.InstallPath, DictionaryGameRoot.LibrariesPath,x.Downloads.Artifact.Path),
                        Url = x.Downloads.Artifact.Url,
                        Size = (ulong)x.Downloads.Artifact.Size,
                        Hash = x.Downloads.Artifact.Sha1
                    }
                });
            }
        });

        return list;
    }

    private List<DownloadListEntry.DownloadFileItem> GetAssets()
    {
        var list = new List<DownloadListEntry.DownloadFileItem>();

        if (ManifestClientAssetsJson?.Objects != null)
        {
            foreach (var asset in ManifestClientAssetsJson.Objects)
            {
                var hash = asset.Value.Hash;
                var hashPrefix = hash.Substring(0, 2);
                var url = $"{DictionaryDownloadHost.Sources.ResourceHost}/{hashPrefix}/{hash}";
                var fileName = Path.Combine(ClientRootInfo.InstallPath, DictionaryGameRoot.AssetsObjectPath, hashPrefix, hash);

                list.Add(new DownloadListEntry.DownloadFileItem()
                {
                    Type = FileType.AssetFile,
                    FileInfo = new()
                    {
                        FileName = fileName,
                        Url = url,
                        Size = (ulong)asset.Value.Size,
                        Hash = hash
                    }
                });
            }
        }

        return list;
    }

    private DownloadStatusType GetStatusForFileType(FileType fileType)
    {
        return fileType switch
        {
            FileType.BaseGame => DownloadStatusType.DownloadClient,
            FileType.JarFile => DownloadStatusType.DownloadLibrary,
            FileType.AssetFile => DownloadStatusType.DownloadAssets,
            _ => DownloadStatusType.DownloadJson
        };
    }

    private DownloadStatusType GetCompletionStatusForFileType(FileType fileType)
    {
        return fileType switch
        {
            FileType.BaseGame => DownloadStatusType.CompletionJar,
            FileType.JarFile => DownloadStatusType.DownloadLibrarySuccess,
            FileType.AssetFile => DownloadStatusType.DownloadAssetsSuccess,
            _ => DownloadStatusType.CompletionSuccess
        };
    }

    /// <summary>
    /// 格式化文件大小显示
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int counter = 0;
        decimal number = bytes;

        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:n1} {suffixes[counter]}";
    }

    #endregion
}