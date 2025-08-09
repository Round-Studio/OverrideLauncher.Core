using System.Runtime.InteropServices;
using System.Text.Json;
using OverrideLauncher.Core.Base.Dictionary;
using OverrideLauncher.Core.Base.Entry.Download.Install.Client;
using OverrideLauncher.Core.Base.Entry.Download.Install.Manifest;
using OverrideLauncher.Core.Base.Entry.Info;
using OverrideLauncher.Core.Classes.Install.Manifest;
using OverrideLauncher.Core.Classes.Utilities;

namespace OverrideLauncher.Core.Classes.Reader;

public class ReadClient : ClientInfo
{
    public ReadClient(ClientRootInfo info)
    {
        ClientName = info.ClientName;
        ClientRootPath = info.ClientRootPath;

        if (!Directory.Exists(ClientRootPath)) throw new DirectoryNotFoundException($"未找到 {ClientRootPath}");
        if (!File.Exists(
                Path.Combine(ClientRootPath, DictionaryGameRoot.VersionsPath, ClientName, $"{ClientName}.json")))
            throw new FileNotFoundException($"未找到 版本文件 {ClientName}.json");

        ManifestClientJson =
            JsonSerializer.Deserialize<ManifestClientJson>(
                File.ReadAllText(Path.Combine(ClientRootPath, DictionaryGameRoot.VersionsPath, ClientName,
                    $"{ClientName}.json"))) ?? throw new InvalidOperationException();
        
        if (!File.Exists(Path.Combine(ClientRootPath, DictionaryGameRoot.AssetsIndexPath,
                $"{ManifestClientJson.AssetIndex.Id}.json")))
            throw new FileNotFoundException($"未找到 资源引索 {ManifestClientJson.AssetIndex.Id}.json");
        if (ManifestClientJson?.MinimumLauncherVersion == null || ManifestClientJson?.MinimumLauncherVersion <= 7)
            ManifestClientJson.MinimumLauncherVersion = 8;
        
        ManifestClientAssetsJson = JsonSerializer.Deserialize<ManifestClientAssetsJson>(
            File.ReadAllText(Path.Combine(ClientRootPath, DictionaryGameRoot.AssetsIndexPath,
                $"{ManifestClientJson.AssetIndex.Id}.json"))) ?? throw new InvalidOperationException();

        if (!File.Exists(Path.Combine(ClientRootPath, DictionaryGameRoot.VersionsPath, ClientName,
                $"{ClientName}.jar")))
            throw new FileNotFoundException("未找到游戏本体文件");

        System = GetSystem();
        ModLoaders = GetModLoadeer();
        FilesFullRange = IsFullFiles();

        try
        {
            var clientBodyJson = JsonSerializer.Deserialize<ClientBodyJson>(
                ZipUtil.ReadFileContentFromZip(
                    Path.Combine(ClientRootPath, DictionaryGameRoot.VersionsPath, ClientName,
                        $"{ClientName}.jar"), "version.json"));
            ClientVersion = clientBodyJson.Id;
        }
        catch
        {
            ClientVersion = null;
        }
    }

    private string GetSystem()
    {
        if (OperatingSystem.IsWindows()) return "windows";
        if (OperatingSystem.IsLinux()) return "linux";
        if (OperatingSystem.IsMacOS()) return "osx";

        return string.Empty;
    }

    private List<string> GetModLoadeer()
    {
        if (string.IsNullOrEmpty(ManifestClientJson.MainClass)) throw new NullReferenceException("\"MainClass\" is null.");
        
        var res = new List<string>();
        switch (ManifestClientJson.MainClass)
        {
            case "net.minecraftforge.bootstrap.ForgeBootstrap":
            case "net.minecraft.launchwrapper.Launch":
                res.Add("Forge");
                break;
            case "net.minecraft.client.main.Main":
                res.Add("Vanilla");
                break;
            case "cpw.mods.bootstraplauncher.BootstrapLauncher":
                res.Add("NeoForge");
                break;
            case "net.fabricmc.loader.impl.launch.knot.KnotClient":
                res.Add("Fabric");
                break;
        }

        return res;
    }

    private bool IsFullFiles()
    {
        foreach (var file in ManifestClientJson.Libraries)
        {
            var path = "";

            if (string.IsNullOrEmpty(file.Name))
            {
                if (InstallHelper.IsThisSystemFile(file.Name))
                    path = Path.Combine(ClientRootPath, DictionaryGameRoot.LibrariesPath,
                        InstallHelper.ConvertToMavenPath(file.Name));
            }

            if (file?.Downloads != null)
            {
                if (file.Downloads?.Artifact != null)
                {
                    if (InstallHelper.IsThisSystemFile(file.Downloads.Artifact.Path))
                        path = Path.Combine(ClientRootPath, DictionaryGameRoot.LibrariesPath,
                            file.Downloads.Artifact.Path);
                }

                if (file?.Downloads?.Classifiers != null)
                {
                    try
                    {
                        var classFi = file?.Downloads?.Classifiers["natives-" + System];
                        if (classFi != null)
                        {
                            path = Path.Combine(ClientRootPath, DictionaryGameRoot.LibrariesPath,
                                classFi.Path);
                        }
                    }catch{ }
                }
            }

            if(!string.IsNullOrEmpty(path)) if (!File.Exists(path)) return false;
        }
        
        foreach (var fileInfo in ManifestClientAssetsJson.Objects)
        {
            var file = Path.Combine(ClientRootPath, DictionaryGameRoot.AssetsObjectPath,
                fileInfo.Value.Hash.Substring(0, 2), fileInfo.Value.Hash);

            if (!File.Exists(file)) return false;
        }
        
        return true;
    }
}