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

public class InstallClient : Download
{
    #region Public
    public ManifestClientJson ManifestClientJson { get; set; }
    public ManifestClientAssetsJson ManifestClientAssetsJson { get; set; }
    public EventHandler<DownloadStatusChangedEntry> DownloadStatusChanged;
    public InstallClient(ManifestMojang.ManifestVersion manifestVersion)
    {
        _installName = manifestVersion.Id;
        ManifestClientJson = InstallHelper.TryingGetClientJson(manifestVersion).Result;
        ManifestClientAssetsJson = InstallHelper.TryingGetClientAssetsJson(ManifestClientJson).Result;
    }

    public async Task Install(InstallClientInfo info)
    {
        _installClientInfo = info;
        if(!string.IsNullOrEmpty(info.InstallName)) _installName = info.InstallName;
        
        SaveJson();
        PrepareFiles();

        FileCount = (ulong)(downloadList.Files.Count - 1);
        await Download();
    }

    #endregion
    
    #region Private
    private string _installName { get; set; }
    private InstallClientInfo _installClientInfo { get; set; }
    private DownloadListEntry downloadList { get; set; } = new();
    private void SaveJson()
    {
        var versionJsonPath = Path.Combine(_installClientInfo.InstallPath, DictionaryGameRoot.VersionsPath,
            _installName, $"{_installName}.json");
        var assetsJsonPath = Path.Combine(_installClientInfo.InstallPath, DictionaryGameRoot.AssetsIndexPath,
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
                FileName = Path.Combine(_installClientInfo.InstallPath, DictionaryGameRoot.VersionsPath, _installName,
                    $"{_installName}.jar")
            }
        }); // 添加本体文件
        
        downloadList.Files.AddRange(GetArtifacts()); // 添加所有 Artifact 文件
    }

    private async Task Download()
    {
        var downloadTasks = new List<Task>();
        var semaphore = new SemaphoreSlim(DownloadThreadCount);
        var downloadedCount = 0;
        var lockObj = new object();

        foreach (var file in downloadList.Files)
        {
            await semaphore.WaitAsync(); // 等待信号量，控制并发数

            downloadTasks.Add(Task.Run(() =>
            {
                try
                {
                    DownloadFile(file.FileInfo); // 假设这是异步下载方法

                    lock (lockObj)
                    {
                        downloadedCount++;
                        var progress = (double)downloadedCount / downloadList.Files.Count * 100;
                    
                        DownloadStatusChanged?.Invoke(this, new DownloadStatusChangedEntry()
                        {
                            Progress = progress,
                            Status = DownloadStatusType.CompletionSuccess
                        });
                    }
                }
                catch (Exception ex)
                {
                    DownloadStatusChanged?.Invoke(this, new DownloadStatusChangedEntry()
                    {
                        Progress = (double)downloadedCount / downloadList.Files.Count * 100,
                        Status = DownloadStatusType.Error
                    });
                }
                finally
                {
                    semaphore.Release(); // 释放信号量
                }
            }));
        }

        await Task.WhenAll(downloadTasks); // 等待所有下载任务完成
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
                        FileName = Path.Combine(_installClientInfo.InstallPath, DictionaryGameRoot.LibrariesPath,x.Downloads.Artifact.Path),
                        Url = x.Downloads.Artifact.Url,
                        Size = (ulong)x.Downloads.Artifact.Size,
                        Hash = x.Downloads.Artifact.Sha1
                    }
                });
            }
        });

        return list;
    }
    #endregion
}