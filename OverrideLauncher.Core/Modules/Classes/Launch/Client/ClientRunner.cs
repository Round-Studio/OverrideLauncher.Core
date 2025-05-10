using System;
using System.Diagnostics;
using OverrideLauncher.Core.Modules.Entry.LaunchEntry;

namespace OverrideLauncher.Core.Modules.Classes.Launch.Client;

public class ClientRunner
{
    public Process GameProcess { get; set; }
    public Action<string> LogsOutput { get; set; } 
    public ClientRunner(ClientRunnerInfo RunnerInfo)
    {
        var g = new GenerateClientParameters(RunnerInfo);
        string arg = g.SplicingArguments();
        GameProcess = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = RunnerInfo.JavaInfo.JavaPath,
                Arguments = arg,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true
            }
        };
        File.WriteAllText("a.bat",$"\"{RunnerInfo.JavaInfo.JavaPath}\" {arg}");
        GameProcess.OutputDataReceived += (sender, args) =>
        {
            LogsOutput.Invoke(args.Data);
        };
        GameProcess.ErrorDataReceived += (sender, args) =>
        {
            LogsOutput.Invoke(args.Data);
        };
    }
    public void Start()
    {
        GameProcess.Start();
        GameProcess.BeginOutputReadLine();
        GameProcess.BeginErrorReadLine();
        
        GameProcess.WaitForExit();
    }
}