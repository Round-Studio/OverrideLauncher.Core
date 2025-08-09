using System.ComponentModel;
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
    public ParameterClientLaunchMaker(ClientRunnerInfo info)
    {
        _info = info;
        _ClientInfo = new ReadClient(info.ClientRootInfo);

        _nativePath = Path.Combine(_info.ClientRootInfo.ClientRootPath, DictionaryGameRoot.VersionsPath,
            _info.ClientRootInfo.ClientName, DictionaryGameRoot.NativesPath);
        
        Console.WriteLine(SplicingCPArguments());
    }
    
    public void Make()
    {
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
        };
        
        List<string> RequiredJVMArgs = new()
        {
            "-XX:+UseG1GC -XX:-UseAdaptiveSizePolicy -XX:-OmitStackTraceInFastThrow",
            "-Djava.library.path=${natives_directory}",
            "-Dorg.lwjgl.system.SharedLibraryExtractPath=${natives_directory}",
            "-Dio.netty.native.workdir=${natives_directory}",
            "-Djna.tmpdir=${natives_directory}",
            "-cp",
            "${classpath}"
        };
    }

    private string SplicingCPArguments()
    {
        var res = new List<string>();

        _ClientInfo.ManifestClientJson.Libraries.ForEach(lib =>
        {
            if (lib?.Downloads != null)
            {
                if (lib.Downloads?.Artifact != null)
                {
                    if (!lib.Downloads.Artifact.Path.Contains("native"))
                    {
                        if (InstallHelper.IsThisSystemFile(lib.Downloads.Artifact.Path))
                        {
                            res.Add(Path.Combine(_ClientInfo.ClientRootPath, DictionaryGameRoot.LibrariesPath,
                                lib.Downloads.Artifact.Path));
                        }
                    }
                }
            }
        });
        res.Add(Path.Combine(_ClientInfo.ClientRootPath, DictionaryGameRoot.VersionsPath,
            _ClientInfo.ClientName, $"{_ClientInfo.ClientName}.jar"));

        return string.Join(';', res);
    }
}