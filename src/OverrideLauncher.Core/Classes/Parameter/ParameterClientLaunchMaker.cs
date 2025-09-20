using System.ComponentModel;
using System.Text.Json;
using OverrideLauncher.Core.Base.Dictionary;
using OverrideLauncher.Core.Base.Entry.Download.Install.Client;
using OverrideLauncher.Core.Base.Entry.Info;
using OverrideLauncher.Core.Classes.Install.Manifest;
using OverrideLauncher.Core.Classes.Reader;

namespace OverrideLauncher.Core.Classes.Parameter;

public class ParameterClientLaunchMaker
{
    private ClientRunnerInfo _info;
    private ClientInfo _ClientInfo;
    private string _nativePath = "";

    private List<string> _nativeFiles = new();
    public ParameterClientLaunchMaker(ClientRunnerInfo info)
    {
        _info = info;
        _ClientInfo = new ReadClient(info.ClientRootInfo);

        _nativePath = Path.Combine(_info.ClientRootInfo.ClientRootPath, DictionaryGameRoot.VersionsPath,
            _info.ClientRootInfo.ClientName, DictionaryGameRoot.NativesPath);
        
        Console.WriteLine(Make());
#if DEBUG
        File.WriteAllText("D:\\test.bat",Make());
#endif
    }
    
    public (List<string>,string) GetNativeInfo()
    {
        return (_nativeFiles, _nativePath);
    }
    
    public string Make()
    {
        var ResultArgs = new List<string>();
        
        Dictionary<string, string> Args = new Dictionary<string, string>()
        {
            ["${natives_directory}"] = $"\"{_nativePath}\"",
            ["${library_directory}"] =
                $"\"{Path.Combine(_info.ClientRootInfo.ClientRootPath, DictionaryGameRoot.LibrariesPath)}\"",
            ["${classpath}"] = $"\"{SplicingCPArguments()}\"",
            ["${main_class}"] = _ClientInfo.ManifestClientJson.MainClass,
            ["${game_directory}"] =
                $"\"{Path.Combine(_info.ClientRootInfo.ClientRootPath, DictionaryGameRoot.VersionsPath, _info.ClientRootInfo.ClientName)}\"",
            ["${assets_root}"] = $"\"{Path.Combine(_info.ClientRootInfo.ClientRootPath, "assets")}\"",
            ["${assets_index_name}"] = _ClientInfo.ManifestClientJson.AssetIndex.Id,
            ["${version_name}"] = _ClientInfo.ClientName,
            ["${auth_uuid}"] = _info.Account.UUID,
            ["${auth_access_token}"] = _info.Account.Token,
            ["${user_type}"] = _info.Account.AccountType.ToString(),
            ["${version_type}"] = $"\"{_info.LauncherInfo}\"",
            ["${launcher_name}"] = $"\"{_info.LauncherInfo}\"",
            ["${launcher_version}"] = _info.LauncherVersion,
            ["${auth_player_name}"] = _info.Account.UserName,
            ["${user_properties}"] = "{}",
            ["${clientid}"] = _ClientInfo.ClientName
        };

        List<string> RequiredJVMArgs = new()
        {
            "${main_class}",
            "${classpath}",
            "-cp",
            "-Djna.tmpdir=${natives_directory}",
            $"{(_info.JvmInfo.IsGC ? "-XX:+UseG1GC" : "")} -XX:-UseAdaptiveSizePolicy -XX:-OmitStackTraceInFastThrow",
            $"-Xmx{_info.JvmInfo.MemorySize}m",
            "-Djava.library.path=${natives_directory}",
            "-Dorg.lwjgl.librarypath=${natives_directory}",
            "-Dorg.lwjgl.system.SharedLibraryExtractPath=${natives_directory}",
            "-Dio.netty.native.workdir=${natives_directory}"
        };

        var spi = SplicingArgs();
        RequiredJVMArgs.ForEach(x =>
        {
            if (!spi.Contains(x))
            {
                spi.Insert(0, x);
            }
        });
        
        spi.ForEach(x =>
        {
            var res_str = x;
            foreach (var (key, value) in Args)
            {
                res_str = res_str.Replace(key, value);
            }
            
            ResultArgs.Add(res_str);
        });
        
        if(_info.IsDemo) ResultArgs.Add("--demo");

        return string.Join(' ', ResultArgs);
    }

    private string SplicingCPArguments()
    {
        var res = new List<string>();

        _ClientInfo.ManifestClientJson.Libraries.ForEach(lib =>
        {
            if (lib?.Downloads != null)
            {
                if (lib.Downloads?.Artifact != null)
                    if (InstallHelper.IsThisSystemFile(lib.Downloads.Artifact.Path))
                    {
                        if (!lib.Downloads.Artifact.Path.Contains("native"))
                        {
                            res.Add(Path.Combine(_ClientInfo.ClientRootPath, DictionaryGameRoot.LibrariesPath,
                                lib.Downloads.Artifact.Path).Replace("3.2.1","3.2.2"));
                        }
                        else
                        {
                            _nativeFiles.Add(Path.Combine(_ClientInfo.ClientRootPath, DictionaryGameRoot.LibrariesPath,
                                lib.Downloads.Artifact.Path));
                        }
                    }

                if (lib.Downloads?.Classifiers != null)
                {
                    foreach (var classifier in lib.Downloads?.Classifiers)
                    {
                        if (InstallHelper.IsThisSystemFile(classifier.Value.Path))
                        {
                            _nativeFiles.Add(Path.Combine(_ClientInfo.ClientRootPath, DictionaryGameRoot.LibrariesPath,
                                classifier.Value.Path));
                        }
                    }
                }
            }

            if (lib.Downloads == null)
            {
                if (!string.IsNullOrEmpty(lib.Name))
                {
                    res.Add(Path.Combine(_ClientInfo.ClientRootPath, DictionaryGameRoot.LibrariesPath,
                        InstallHelper.ConvertToMavenPath(lib.Name)));
                }
            }
        });
        
        res.Add(Path.Combine(_ClientInfo.ClientRootPath, DictionaryGameRoot.VersionsPath,
            _ClientInfo.ClientName, $"{_ClientInfo.ClientName}.jar"));

        return string.Join(';', res);
    }

    private List<string> SplicingArgs()
    {
        var result = new List<string>();

        if (_ClientInfo.ManifestClientJson?.Arguments == null)
        {
            result.Add(_ClientInfo?.ManifestClientJson.MinecraftArguments);
        }
        else
        {
            if (_ClientInfo.ManifestClientJson.Arguments?.Jvm != null)
                _ClientInfo.ManifestClientJson.Arguments.Jvm.ForEach(x =>
                {
                    if (x is JsonElement strruleElement && strruleElement.ValueKind == JsonValueKind.String)
                        result.Add(x.ToString());
                });

            result.Add("${main_class}");
            
            if (_ClientInfo.ManifestClientJson.Arguments?.Game != null)
                _ClientInfo.ManifestClientJson.Arguments.Game.ForEach(x =>
                {
                    if (x is JsonElement strruleElement && strruleElement.ValueKind == JsonValueKind.String)
                        result.Add(x.ToString());
                });
        }

        return result;
    }
}