using System.IO.Compression;
using OverrideLauncher.Core.Base.Dictionary;
using OverrideLauncher.Core.Base.Entry.Download;
using OverrideLauncher.Core.Base.Entry.Download.Install;
using OverrideLauncher.Core.Base.Entry.Download.Install.Client;
using OverrideLauncher.Core.Base.Entry.Download.Install.Forge;
using OverrideLauncher.Core.Base.Entry.Download.Install.Manifest;
using OverrideLauncher.Core.Base.Enum;
using OverrideLauncher.Core.Classes.Install.Manifest;
using OverrideLauncher.Core.Interface.Download;

namespace OverrideLauncher.Core.Classes.Install.Installer;

public class InstallerForge : IDownload
{
    private DownloadListEntry downloadList { get; set; } = new();
    private string _installVersion;
    public InstallerForge(string forgeVersionId)
    {
        _installVersion = forgeVersionId;
        var forgeversion = forgeVersionId.Replace($"{forgeVersionId.Split('-')[0]}-", "");
        var clientversion = forgeVersionId.Split('-')[0];

        var installerUrl = DictionaryDownloadHost.Sources.ForgeResourceHost
            .Replace("{FORGE_VERSION_ID}", forgeVersionId)
            .Replace("{FORGE_VERSION}",forgeversion)
            .Replace("{CLIENT_VERSION}",clientversion);
        
        Console.WriteLine(installerUrl);
        _installfileurl = installerUrl;
    }

    public async Task Install(ClientRootInfo rootInfo)
    {
        _rootInfo = rootInfo;
        _temppath = Path.Combine(rootInfo.InstallPath, DictionaryGameRoot.VersionsPath, rootInfo.InstallName,
            $"install_temp_dir");

        var installfile = Path.Combine(rootInfo.InstallPath, DictionaryGameRoot.LibrariesPath,"net","minecraftforge",
            "forge",_installVersion, $"forge-{_installVersion}.jar");

        await DownloadInstallFile(installfile);
        await Task.Delay(100); // 防止文件被占用
        ExtractJar(installfile, _temppath);
        ProcessVersionJson();
    }

    private string _temppath { get; set; }
    private string _installfileurl { get; set; }
    private ClientRootInfo _rootInfo { get; set; }

    private async Task DownloadInstallFile(string file)
    {
        // 单文件类型，提供实时进度回调
        var progress = new Progress<double>(percentage =>
        {
            DownloadStatusChanged?.Invoke(this, new DownloadStatusChangedEntry()
            {
                Progress = percentage,
                Status = GetStatusForFileType(FileType.JarFile),
                FileType = FileType.JarFile,
                CurrentFileName = Path.GetFileName(file),
                CompletedFiles = percentage >= 100 ? 1 : 0,
                TotalFiles = 1
            });
        });
        await DownloadFileAsync(new DownloadFileInfo()
        {
            Url = _installfileurl,
            FileName = file
        }, progress);
    }
    private void ExtractJar(string jarPath, string extractTo)
    {
        using (var archive = ZipFile.OpenRead(jarPath))
        {
            foreach (var entry in archive.Entries)
            {
                var destinationPath = Path.Combine(extractTo, entry.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                
                if (!string.IsNullOrEmpty(entry.Name))
                {
                    entry.ExtractToFile(destinationPath, true);
                }
            }
        }
    }

    private void ProcessVersionJson()
    {
        var forgejsonfile = Path.Combine(_rootInfo.InstallPath, DictionaryGameRoot.VersionsPath, _rootInfo.InstallName,
            "install_temp_dir", "version.json");

        var valjson = InstallHelper.GetClientJsonEntry(_rootInfo);
        var forgjson = ForgeVersionInfo.FromJson(forgejsonfile);

        forgjson.Arguments.Game.ForEach(x => valjson.Arguments.Game.Add(x));
        forgjson.Arguments.Jvm.ForEach(x => valjson.Arguments.Jvm.Add(x));

        valjson.MainClass = forgjson.MainClass;
        forgjson.Libraries.ForEach(x =>
        {
            bool hany = false;
            valjson.Libraries.ForEach(x1 =>
            {
                if (x1.Name == x.Name) hany = true;
            });
            if (!hany)
            {
                // var url = string.IsNullOrEmpty(x.Downloads.Artifact.Url) ? _installfileurl : x.Downloads.Artifact.Url;
                valjson.Libraries.Add(new ManifestClientJson.Library()
                {
                    Name = x.Name,
                    Downloads = new ManifestClientJson.LibraryDownloads()
                    {
                        Artifact = new ManifestClientJson.Artifact()
                        {
                            Path = x.Downloads.Artifact.Path,
                            Sha1 = x.Downloads.Artifact.Sha1,
                            Size = (int)x.Downloads.Artifact.Size
                        }
                    }
                });
            }
        });

        InstallHelper.SaveClientJson(valjson, _rootInfo);
    }
}