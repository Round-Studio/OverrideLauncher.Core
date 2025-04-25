using System.Diagnostics;
using OverrideLauncher.Core.Modules.Entry.LaunchEntry;

namespace OverrideLauncher.Core.Modules.Classes.Launch;

public class LaunchRunner
{
    public Process GameProcess { get; set; }
    public LaunchRunner(LaunchRunnerInfo RunnerInfo)
    {
        var g = new GenerateParameters(RunnerInfo);
        string arg = g.SplicingArguments();
        Console.WriteLine(arg);
        GameProcess = Process.Start(RunnerInfo.JavaInfo.JavaPath, arg);
        GameProcess.WaitForExit();
    }
}