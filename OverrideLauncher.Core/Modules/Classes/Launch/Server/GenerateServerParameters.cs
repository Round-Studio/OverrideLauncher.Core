using OverrideLauncher.Core.Modules.Entry.LaunchEntry;

namespace OverrideLauncher.Core.Modules.Classes.Launch.Server;

public class GenerateServerParameters
{
    private ServerRunnerInfo _info;
    public GenerateServerParameters(ServerRunnerInfo RunnerInfo)
    {
        _info = RunnerInfo;
    }
    public string SplicingArguments()
    {
        List<string> Args = new List<string>();
        Args.Add("-jar");
        Args.Add($"\"{Path.GetFullPath(Path.Combine(_info.ServerInfo.InstallPath,"server.jar"))}\"");
        
        if (_info.IsNoWindow)
        {
            Args.Add("nogui");
        }

        return string.Join(" ", Args);
    }
}