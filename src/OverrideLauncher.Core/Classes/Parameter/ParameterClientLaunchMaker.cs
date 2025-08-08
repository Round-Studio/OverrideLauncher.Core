using OverrideLauncher.Core.Base.Entry.Download.Install.Client;

namespace OverrideLauncher.Core.Classes.Parameter;

public class ParameterClientLaunchMaker
{
    private ClientRootInfo _info;
    public ParameterClientLaunchMaker(ClientRootInfo info)
    {
        _info = info;
    }
    
    public void Make()
    {
        /*Dictionary<string, string> Args = new Dictionary<string, string>()
        {
            ["${natives_directory}"] = $"\"{NativePath}\"",
            ["${library_directory}"] = $"\"{Path.Combine(_info.ClientRootPath, "libraries")}\"",
            ["${classpath}"] = $"\"{SplicingCPArguments()}\"",
            ["${main_class}"] = GameJsonEntry.MainClass,
            ["${game_directory}"] = $"\"{Path.Combine(_info.ClientRootPath, "versions", _info.ClientName)}\"",
            ["${assets_root}"] = $"\"{Path.Combine(_info.ClientRootPath, "assets")}\"",
            ["${assets_index_name}"] = GameJsonEntry.AssetIndex.Id,
            ["${version_name}"] = ClientInfo.GameName,
            ["${auth_uuid}"] = ClientRunnerInfo.Account.UUID,
            ["${auth_access_token}"] = ClientRunnerInfo.Account.Token,
            ["${user_type}"] = ClientRunnerInfo.Account.AccountType,
            ["${version_type}"] = $"\"{ClientRunnerInfo.LauncherInfo}\"", 
            ["${launcher_name}"] = $"\"{ClientRunnerInfo.LauncherInfo}\"",
            ["${launcher_version}"] = ClientRunnerInfo.LauncherVersion,
            ["${auth_player_name}"] = ClientRunnerInfo.Account.UserName,
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
        };*/
    }
}