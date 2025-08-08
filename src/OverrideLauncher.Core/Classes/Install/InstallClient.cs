using System.Text.Json;
using System.Text.Json.Serialization;
using OverrideLauncher.Core.Base.Dictionary;
using OverrideLauncher.Core.Base.Entry.Download.Install;
using OverrideLauncher.Core.Base.Entry.Download.Install.Client;
using OverrideLauncher.Core.Base.Entry.Download.Install.Manifest;
using OverrideLauncher.Core.Base.Entry.Info;
using OverrideLauncher.Core.Base.Enum;
using OverrideLauncher.Core.Base.Enum.Download;
using OverrideLauncher.Core.Classes.Install.Manifest;
using OverrideLauncher.Core.Classes.Utilities;
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
        var versionid = _installName;
        ClientRootInfo = rootInfo;
        if(!string.IsNullOrEmpty(rootInfo.ClientName)) _installName = rootInfo.ClientName;
        
        SaveJson();
        PrepareFiles();

        FileCount = (ulong)(downloadList.Files.Count - 1);
        await Download();

        try
        {
            ZipUtil.ReadFileContentFromZip(Path.Combine(ClientRootInfo.ClientRootPath, DictionaryGameRoot.VersionsPath,
                _installName,
                $"{_installName}.jar"), "version.json");
        }
        catch
        {
            ZipUtil.AddJsonToZip(Path.Combine(ClientRootInfo.ClientRootPath, DictionaryGameRoot.VersionsPath, _installName,
                $"{_installName}.jar"),"version.json", new ClientBodyJson()
            {
                Id = versionid
            });
        }
    }

    #endregion
    
    #region Private
    private string _installName { get; set; }
    private ClientRootInfo ClientRootInfo { get; set; }
    private ManifestClientAssetsJson ManifestClientAssetsJson { get; set; }
    private ManifestClientJson ManifestClientJson { get; set; }
    private void SaveJson()
    {
        var versionJsonPath = Path.Combine(ClientRootInfo.ClientRootPath, DictionaryGameRoot.VersionsPath,
            _installName, $"{_installName}.json");
        var assetsJsonPath = Path.Combine(ClientRootInfo.ClientRootPath, DictionaryGameRoot.AssetsIndexPath,
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
                FileName = Path.Combine(ClientRootInfo.ClientRootPath, DictionaryGameRoot.VersionsPath, _installName,
                    $"{_installName}.jar")
            }
        }); // 添加本体文件

        downloadList.Files.AddRange(GetArtifacts()); // 添加所有 Artifact 文件
        downloadList.Files.AddRange(GetAssets()); // 添加所有 Assets 文件
    }
    private string GetSystem()
    {
        if (OperatingSystem.IsWindows()) return "windows";
        if (OperatingSystem.IsLinux()) return "linux";
        if (OperatingSystem.IsMacOS()) return "osx";

        return string.Empty;
    }
    private List<DownloadListEntry.DownloadFileItem> GetArtifacts()
    {
        var list = new List<DownloadListEntry.DownloadFileItem>();
        ManifestClientJson.Libraries.ForEach(x =>
        {
            if (x?.Downloads != null)
            {
                var path = "";
                var size = 0;
                var hash = "";
                var url = "";
                
                var file = x;

                if (string.IsNullOrEmpty(file.Name))
                {
                    if (InstallHelper.IsThisSystemFile(file.Name) && !string.IsNullOrEmpty(file.Url))
                    {
                        path = Path.Combine(ClientRootInfo.ClientRootPath, DictionaryGameRoot.LibrariesPath,
                            InstallHelper.ConvertToMavenPath(file.Name));
                        size = (int)file.Size;
                        hash = String.Empty;
                        url = file.Url;
                    }
                }

                if (file?.Downloads != null)
                {
                    if (file.Downloads?.Artifact != null)
                    {
                        if (InstallHelper.IsThisSystemFile(file.Downloads.Artifact.Path))
                        {
                            path = Path.Combine(ClientRootInfo.ClientRootPath, DictionaryGameRoot.LibrariesPath,
                                file.Downloads.Artifact.Path);
                            hash = file.Downloads.Artifact.Sha1;
                            url = file.Downloads.Artifact.Url;
                            size = file.Downloads.Artifact.Size;
                        }
                    }

                    if (file?.Downloads?.Classifiers != null)
                    {
                        try
                        {
                            var classFi = file?.Downloads?.Classifiers[$"natives-{GetSystem()}"];
                            if (classFi != null)
                            {
                                path = Path.Combine(ClientRootInfo.ClientRootPath, DictionaryGameRoot.LibrariesPath,
                                    classFi.Path);

                                url = classFi.Url;
                                size = classFi.Size;
                                hash = classFi.Sha1;
                            }
                        }catch{ }
                    }
                }
                
                list.Add(new DownloadListEntry.DownloadFileItem()
                {
                    Type = FileType.JarFile,
                    FileInfo = new()
                    {
                        FileName = path,
                        Url = url,
                        Size = (ulong)size,
                        Hash = hash
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
                var fileName = Path.Combine(ClientRootInfo.ClientRootPath, DictionaryGameRoot.AssetsObjectPath, hashPrefix, hash);

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