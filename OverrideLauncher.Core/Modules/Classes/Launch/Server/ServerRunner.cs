﻿using System;
using System.Diagnostics;
using OverrideLauncher.Core.Modules.Classes.Launch.Server;
using OverrideLauncher.Core.Modules.Entry.LaunchEntry;

namespace OverrideLauncher.Core.Modules.Classes.Launch.Client;

public class ServerRunner
{
    public Process GameProcess { get; set; }
    public Action<string> LogsOutput { get; set; } 
    public ServerRunner(ServerRunnerInfo RunnerInfo)
    {
        File.WriteAllText(Path.Combine(RunnerInfo.ServerInfo.InstallPath,"eula.txt"),"eula=true");
        var g = new GenerateServerParameters(RunnerInfo);
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
                RedirectStandardInput = true,
                WorkingDirectory = RunnerInfo.ServerInfo.InstallPath
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