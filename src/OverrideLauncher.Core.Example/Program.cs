using OverrideLauncher.Core.Base.Entry.Download.Install.Client;
using OverrideLauncher.Core.Classes.Install;
using OverrideLauncher.Core.Classes.Install.Manifest;
using OverrideLauncher.Core.Base.Dictionary;
using System.Diagnostics;

var installname = "1.21.8";

Console.WriteLine("=== OverrideLauncher 高速下载测试 ===");
Console.WriteLine($"固定并发数: 512");

// 自动使用 BMCLAPI 镜像源
DictionaryDownloadHost.SwitchMirror("official");
Console.WriteLine($"使用镜像源: {DictionaryDownloadHost.GetCurrentMirror().Name}");

Console.WriteLine("=== 请选择下载内容 ===");
Console.WriteLine("1. 安装原版游戏");
Console.WriteLine("2. 安装 Fabric");

var choose = Console.ReadKey();

if (choose.Key == ConsoleKey.D1)
{

    Console.WriteLine($"\n开始下载 Minecraft {installname}...");
    var stopwatch = Stopwatch.StartNew();

    InstallClient install = new InstallClient(await InstallHelper.TryingFindVersion(installname));

    var lastProgress = 0.0;
    var lastTime = DateTime.Now;

    install.DownloadStatusChanged += (sender, entry) =>
    {
        var now = DateTime.Now;
        var timeDiff = (now - lastTime).TotalSeconds;

        if (timeDiff > 0)
        {
            Console.WriteLine($"[{entry.FileType}] {entry.Status} - 进度: {entry.Progress:F}% ({entry.CompletedFiles}/{entry.TotalFiles}) - 当前文件: {entry.CurrentFileName}");

            lastProgress = entry.Progress;
            lastTime = now;
        }
    };

    await install.Install(new InstallClientInfo()
    {
        InstallName = installname,
        InstallPath = "G:\\testmc"
    });

    stopwatch.Stop();
    Console.WriteLine($"\n下载完成！总耗时: {stopwatch.Elapsed:mm\\:ss}");
}

if (choose.Key == ConsoleKey.D2)
{
    
}