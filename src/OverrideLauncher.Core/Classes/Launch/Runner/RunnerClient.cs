using System.Diagnostics;
using OverrideLauncher.Core.Base.Entry.Info;
using OverrideLauncher.Core.Classes.Parameter;
using OverrideLauncher.Core.Classes.Utilities;

namespace OverrideLauncher.Core.Classes.Launch.Runner;

public class RunnerClient : Process
{
    private ClientRunnerInfo _info;
    public RunnerClient(ClientRunnerInfo info)
    {
        _info = info;
        
        if(_info.JvmInfo == null) throw new NullReferenceException("JvmInfo is null");
        
        var maker = new ParameterClientLaunchMaker(_info);

        var (navfiles, navpath) = maker.GetNativeInfo();
        navfiles.ForEach(x =>
        {
            try
            {
                ZipUtil.UnZip(x,navpath);
            }catch{ }
        });
        
        this.StartInfo = new ProcessStartInfo()
        {
            FileName = _info.JvmInfo.Path,
            Arguments = maker.Make()
        };
    }
}