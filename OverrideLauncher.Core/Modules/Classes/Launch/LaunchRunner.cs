using System;
using System.Diagnostics;
using OverrideLauncher.Core.Modules.Entry.LaunchEntry;

namespace OverrideLauncher.Core.Modules.Classes.Launch;

public class LaunchRunner
{
    public Process GameProcess { get; set; }
    public Action<string> LogsOutput { get; set; } 
    public LaunchRunner(LaunchRunnerInfo RunnerInfo)
    {
        var g = new GenerateParameters(RunnerInfo);
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