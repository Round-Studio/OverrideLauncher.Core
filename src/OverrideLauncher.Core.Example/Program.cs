using OverrideLauncher.Core.Base.Entry.Download.Install.Client;
using OverrideLauncher.Core.Classes.Install;
using OverrideLauncher.Core.Classes.Install.Manifest;
using OverrideLauncher.Core.Base.Dictionary;
using System.Diagnostics;
using OverrideLauncher.Core.Base.Entry.Download.Install;
using OverrideLauncher.Core.Base.Entry.Info;
using OverrideLauncher.Core.Base.Entry.Info.Java;
using OverrideLauncher.Core.Classes.Account;
using OverrideLauncher.Core.Classes.Install.Installer;
using OverrideLauncher.Core.Classes.Launch.Runner;
using OverrideLauncher.Core.Classes.Parameter;
using OverrideLauncher.Core.Classes.Reader;
using OverrideLauncher.Core.Classes.Utilities;

var installname = "1.20.1";
var installroot = "D:\\.minecraft";

Console.WriteLine("=== OverrideLauncher 高速下载测试 ===");

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
Console.WriteLine("3. 复合安装器安装游戏");
Console.WriteLine("4. 安装 Forge");
Console.WriteLine("5. 安装 LiteLoader");
Console.WriteLine("6. 读取游戏信息");
Console.WriteLine("7. 测试启动类");

Console.Write("\n你选择：");

var choose = Console.ReadKey();

Console.WriteLine("");

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
        ClientName = installname,
        ClientRootPath = installroot
    });

    stopwatch.Stop();
    Console.WriteLine($"\n下载完成！总耗时: {stopwatch.Elapsed:mm\\:ss}");
}

if (choose.Key == ConsoleKey.D2)
{
    var fabi = new InstallerFabric(InstallHelper.GetVersionFabricManifest(installname).Result.First());
    fabi.Install(new ClientRootInfo()
    {
        ClientName = installname,
        ClientRootPath = installroot
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
        ClientName = installname,
        ClientRootPath = installroot
    });

    stopwatch.Stop();
    Console.WriteLine($"\n下载完成！总耗时: {stopwatch.Elapsed:mm\\:ss}");
}

if (choose.Key == ConsoleKey.D4)
{
    var fori = new InstallerForge(InstallHelper.TryGetInstallForgeMeta(installname).Result.First());
    fori.DownloadStatusChanged += (sender, entry) =>
    {
        Console.Write(
            $"\r[{entry.FileType}] {entry.Status} - 进度: {entry.Progress:F}% ({entry.CompletedFiles}/{entry.TotalFiles}) - 当前文件: {entry.CurrentFileName}" +
            $"                      ");
    };
    await fori.Install(new ClientRootInfo()
    {
        ClientName = installname,
        ClientRootPath = installroot
    });
}

if (choose.Key == ConsoleKey.D5)
{
    var fori = new InstallerLiteLoader(InstallHelper.TryGetVersionLiteLoaderVersions(installname).Result.First());
    fori.DownloadStatusChanged += (sender, entry) =>
    {
        Console.Write(
            $"\r[{entry.FileType}] {entry.Status} - 进度: {entry.Progress:F}% ({entry.CompletedFiles}/{entry.TotalFiles}) - 当前文件: {entry.CurrentFileName}" +
            $"                      ");
    };
    await fori.Install(new ClientRootInfo()
    {
        ClientName = installname,
        ClientRootPath = installroot
    });
}

if (choose.Key == ConsoleKey.D6)
{
    var config = new ReadClient(new ClientRootInfo()
    {
        ClientName = installname,
        ClientRootPath = installroot
    });
    
    Console.WriteLine($"游戏版本: {config.ClientName}");
    Console.WriteLine($"游戏根目录: {config.ClientRootPath}");
    Console.WriteLine($"游戏版本: {config.ClientVersion}");
    Console.WriteLine($"系统: {config.System}");
    Console.WriteLine($"加载器: {string.Join(", ", config.ModLoaders)}");
    Console.WriteLine($"文件是否完整: {config.FilesFullRange}");
}

if (choose.Key == ConsoleKey.D7)
{
    var info = new RunnerClient(new ClientRunnerInfo()
    {
        ClientRootInfo = new ClientRootInfo()
        {
            ClientName = installname,
            ClientRootPath = installroot
        },
        Account = new AccountOffline("Ove").Authenticate(),
        LauncherVersion = "2.0",
        LauncherInfo = "RMCL 4.0 (By Override.Core)",
        JvmInfo = JavaUtil.GetJavaListAsync().Result[0],
        WindowInfo = new ClientWindowInfo().GetWindowSize(ClientWindowInfo.WindowInfo.w1920h1080)
    });
    info.OutputDataReceived += (sender, e) =>
    {
        Console.WriteLine(e.Data);
    };
    info.Start();
    info.WaitForExit();
}