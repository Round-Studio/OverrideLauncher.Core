using OverrideLauncher.Core.Base.Entry.Download.Install.Client;
using OverrideLauncher.Core.Classes.Install;
using OverrideLauncher.Core.Classes.Install.Manifest;
using OverrideLauncher.Core.Base.Dictionary;
using System.Diagnostics;
using OverrideLauncher.Core.Base.Entry.Download.Install;
using OverrideLauncher.Core.Classes.Install.Installer;

var installname = "1.20.1";

Console.WriteLine("=== OverrideLauncher 高速下载测试 ===");
Console.WriteLine($"固定并发数: 512");

// 自动使用 官方 镜像源
DictionaryDownloadHost.SwitchMirror("official"); // 官方: official
                                                 // BMCL API: bmclapi
Console.WriteLine($"使用镜像源: {DictionaryDownloadHost.GetCurrentMirror().Name}");

// 自定义下载版本

Console.Write("请输入要下载的版本号：");
installname = Console.ReadLine();

Console.WriteLine("=== 请选择下载内容 ===");
Console.WriteLine("1. 安装原版游戏");
Console.WriteLine("2. 安装 Fabric");
Console.WriteLine("3. 复合安装器安装原版游戏");

Console.Write("\n你选择：");

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
            Console.Write($"\r[{entry.FileType}] {entry.Status} - 进度: {entry.Progress:F}% ({entry.CompletedFiles}/{entry.TotalFiles}) - 当前文件: {entry.CurrentFileName}");

            lastProgress = entry.Progress;
            lastTime = now;
        }
    };

    await install.Install(new ClientRootInfo()
    {
        InstallName = installname,
        InstallPath = "G:\\testmc"
    });

    stopwatch.Stop();
    Console.WriteLine($"\n下载完成！总耗时: {stopwatch.Elapsed:mm\\:ss}");
}

if (choose.Key == ConsoleKey.D2)
{
    var fabi = new InstallerFabric(InstallHelper.GetVersionFabricManifest(installname).Result.First());
    fabi.Install(new ClientRootInfo()
    {
        InstallName = installname,
        InstallPath = "D:\\testmc"
    }).Wait();
}

if (choose.Key == ConsoleKey.D3)
{
    Console.WriteLine($"\n开始下载 Minecraft {installname}...");
    var stopwatch = Stopwatch.StartNew();

    var fabricman = await InstallHelper.GetVersionFabricManifest(installname);

    InstallerCollection install = new InstallerCollection(new InstallerCollectionEntry()
    {
        VanillaManifest = await InstallHelper.TryingFindVersion(installname),
        FabricVersion = fabricman.First(),
        FabricApiVersion = InstallHelper.TryGetFabricApiVersions(installname).Result.First()
    });

    var lastProgress = 0.0;
    var lastTime = DateTime.Now;

    install.DownloadStatusChanged += (sender, entry) =>
    {
        var now = DateTime.Now;
        var timeDiff = (now - lastTime).TotalSeconds;

        if (timeDiff > 0) 
        {
            Console.Write($"\r[{entry.FileType}] {entry.Status} - 进度: {entry.Progress:F}% ({entry.CompletedFiles}/{entry.TotalFiles}) - 当前文件: {entry.CurrentFileName}" +
                          $"                      ");

            lastProgress = entry.Progress;
            lastTime = now;
        }
    };

    install.Install(new ClientRootInfo()
    {
        InstallName = installname,
        InstallPath = "D:\\testmc"
    });

    stopwatch.Stop();
    Console.WriteLine($"\n下载完成！总耗时: {stopwatch.Elapsed:mm\\:ss}");
}