using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using OverrideLauncher.Core.Modules.Classes;
using OverrideLauncher.Core.Modules.Classes.Version;
using OverrideLauncher.Core.Modules.Entry.DownloadEntry;
using OverrideLauncher.Core.Modules.Entry.GameEntry;

public class FileIntegrityChecker
{
    public class MissingFile
    {
        public enum FileType
        {
            Jar,
            LoaderJar,
            Assets
        }
        public string Path { get; set; }
        public string Url { get; set; }
        public long Size { get; set; }
        public FileType Type { get; set; }
    }

    private readonly string _gamePath;
    private readonly AssetsEntry.RootObject _assets;
    private readonly GameInstancesInfo _versionInfo = new();
    private readonly GameJsonEntry _version;
    public FileIntegrityChecker(VersionParse versionInfo)
    {
        _gamePath = versionInfo.GameInstances.GameCatalog;
        _versionInfo = versionInfo.GameInstances;
        
        _version = JsonConvert.DeserializeObject<GameJsonEntry>(
            File.ReadAllText(Path.Combine(_gamePath, "versions", versionInfo.GameInstances.GameName,
                $"{versionInfo.GameInstances.GameName}.json")));
        _assets = JsonConvert.DeserializeObject<AssetsEntry.RootObject>(
            File.ReadAllText(Path.Combine(_gamePath, "assets", "indexes",
                $"{_version.Assets}.json")));
    }

    public List<MissingFile> GetMissingFiles()
    {
        var missingFiles = new List<MissingFile>();

        // 检查资源文件
        foreach (var asset in _assets.Objects)
        {
            string hash = asset.Value.Hash;
            string filePath = Path.Combine(_gamePath, "assets", "objects", hash[..2], hash);
            string url = $"https://resources.download.minecraft.net/{hash[..2]}/{hash}";

            if (!File.Exists(filePath) || !VerifyFileSize(filePath, asset.Value.Size))
            {
                missingFiles.Add(new MissingFile
                {
                    Path = filePath,
                    Url = url,
                    Size = asset.Value.Size,
                    Type = MissingFile.FileType.Assets
                });
            }
        }

        // 检查库文件
        var librariesPath = Path.Combine(_gamePath, "libraries");
        foreach (var library in _version.Libraries)
        {
            if (library.Downloads == null)
            {
                var rpath = FileHelper.GetJarFilePath(library.Name);
                var path = Path.GetFullPath(Path.Combine(librariesPath, rpath));

                if (!File.Exists(path))
                {
                    missingFiles.Add(new MissingFile
                    {
                        Path = path,
                        Url = $"{library.Url}{rpath}",
                        Type = MissingFile.FileType.LoaderJar
                    });
                }
                
                continue;
            }
            
            if (library.Downloads?.Artifact != null)
            {
                string libraryPath = Path.Combine(librariesPath, library.Downloads.Artifact.Path);
                if (!File.Exists(libraryPath) || !VerifyFileSize(libraryPath, library.Downloads.Artifact.Size))
                {
                    missingFiles.Add(new MissingFile
                    {
                        Path = libraryPath,
                        Url = library.Downloads.Artifact.Url,
                        Size = library.Downloads.Artifact.Size,
                        Type = MissingFile.FileType.Jar
                    });
                }
            }

            if (library.Downloads?.Classifiers != null)
            {
                string nativeKey = GetNativeKey();
                if (nativeKey != null && library.Downloads.Classifiers.ContainsKey(nativeKey))
                {
                    var native = library.Downloads.Classifiers[nativeKey];
                    string nativePath = Path.Combine(librariesPath, native.Path);
                    if (!File.Exists(nativePath) || !VerifyFileSize(nativePath, native.Size))
                    {
                        missingFiles.Add(new MissingFile
                        {
                            Path = nativePath,
                            Url = native.Url,
                            Size = native.Size,
                            Type = MissingFile.FileType.Jar
                        });
                    }
                }
            }
        }

        // 检查游戏本体文件
        string clientPath = Path.Combine(_gamePath, "versions", _versionInfo.GameName, $"{_versionInfo.GameName}.jar");
        if (!File.Exists(clientPath) || !VerifyFileSize(clientPath, _version.Downloads.Client.Size))
        {
            missingFiles.Add(new MissingFile
            {
                Path = clientPath,
                Url = _version.Downloads.Client.Url,
                Size = _version.Downloads.Client.Size,
                Type = MissingFile.FileType.Jar
            });
        }

        return missingFiles;
    }

    private string GetNativeKey()
    {
        var os = RuntimeInformation.OSDescription.ToLower();
        if (os.Contains("windows")) return "natives-windows";
        if (os.Contains("macos") || os.Contains("darwin")) return "natives-macos";
        if (os.Contains("linux")) return "natives-linux";
        return null;
    }

    private bool VerifyFileSize(string filePath, long expectedSize)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        FileInfo fileInfo = new FileInfo(filePath);
        return fileInfo.Length == expectedSize;
    }
}