using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OverrideLauncher.Core.Base.Dictionary;
using OverrideLauncher.Core.Base.Entry.Download;
using OverrideLauncher.Core.Base.Entry.Download.Install;
using OverrideLauncher.Core.Base.Entry.Download.Install.Client;
using OverrideLauncher.Core.Base.Entry.Download.Install.Forge;
using OverrideLauncher.Core.Base.Entry.Download.Install.Manifest;
using OverrideLauncher.Core.Base.Enum;
using OverrideLauncher.Core.Base.Enum.Download;
using OverrideLauncher.Core.Classes.Install.Manifest;
using OverrideLauncher.Core.Interface.Download;

namespace OverrideLauncher.Core.Classes.Install.Installer;

public class InstallerForge : IDownload
{
    private DownloadListEntry downloadList { get; set; } = new();
    private string _installVersion;
    private string _temppath { get; set; }
    private string _installfileurl { get; set; }
    private ClientRootInfo _rootInfo { get; set; }
    private string _installerJarPath { get; set; }

    // 按文件类型分别统计
    private readonly Dictionary<FileType, FileTypeStats> _fileTypeStats = new();

    private class FileTypeStats
    {
        public int TotalFiles { get; set; }
        public int CompletedFiles { get; set; }
        public int FailedFiles { get; set; }
        public int SkippedFiles { get; set; }
        public int NeedDownloadFiles { get; set; }
    }

    public InstallerForge(string forgeVersionId)
    {
        _installVersion = forgeVersionId;
        var forgeversion = forgeVersionId.Replace($"{forgeVersionId.Split('-')[0]}-", "");
        var clientversion = forgeVersionId.Split('-')[0];

        var installerUrl = DictionaryDownloadHost.Sources.ForgeResourceHost
            .Replace("{FORGE_VERSION_ID}", forgeVersionId)
            .Replace("{FORGE_VERSION}",forgeversion)
            .Replace("{CLIENT_VERSION}",clientversion);

        Console.WriteLine(installerUrl);
        _installfileurl = installerUrl;
    }

    public async Task Install(ClientRootInfo rootInfo)
    {
        _rootInfo = rootInfo;
        _temppath = Path.Combine(rootInfo.InstallPath, DictionaryGameRoot.VersionsPath, rootInfo.InstallName,
            $"install_temp_dir");

        _installerJarPath = Path.Combine(rootInfo.InstallPath, DictionaryGameRoot.LibrariesPath,"net","minecraftforge",
            "forge",_installVersion, $"forge-{_installVersion}-installer.jar");

        // 步骤1: 下载 installer jar 文件
        await DownloadInstallFile(_installerJarPath);
        await Task.Delay(100); // 防止文件被占用

        // 步骤2: 解压 jar 文件，保留 version.json, install_profile.json, data 文件夹
        ExtractJar(_installerJarPath, _temppath);

        // 步骤3: 处理 version.json
        ProcessVersionJson();

        // 步骤4: 下载 libraries 类库
        await DownloadLibraries();

        // 步骤5: 运行 processors
        await RunProcessors();

        // 步骤6: 确保 universal jar 存在
        await EnsureUniversalJarExists();

        // 步骤7: 验证安装结果
        ValidateInstallation();

        // 清理临时文件
        CleanupTempFiles();
    }

    private async Task DownloadInstallFile(string file)
    {
        // 单文件类型，提供实时进度回调
        var progress = new Progress<double>(percentage =>
        {
            DownloadStatusChanged?.Invoke(this, new DownloadStatusChangedEntry()
            {
                Progress = percentage,
                Status = GetStatusForFileType(FileType.JarFile),
                FileType = FileType.JarFile,
                CurrentFileName = Path.GetFileName(file),
                CompletedFiles = percentage >= 100 ? 1 : 0,
                TotalFiles = 1
            });
        });
        await DownloadFileAsync(new DownloadFileInfo()
        {
            Url = _installfileurl,
            FileName = file
        }, progress);
    }
    private void ExtractJar(string jarPath, string extractTo)
    {
        Directory.CreateDirectory(extractTo);

        using (var archive = ZipFile.OpenRead(jarPath))
        {
            // 只提取需要的文件：version.json, install_profile.json, data 文件夹
            var requiredFiles = new[] { "version.json", "install_profile.json" };

            foreach (var entry in archive.Entries)
            {
                var shouldExtract = false;

                // 检查是否是必需的文件
                if (requiredFiles.Contains(entry.FullName))
                {
                    shouldExtract = true;
                }
                // 检查是否在 data 文件夹中
                else if (entry.FullName.StartsWith("data/"))
                {
                    shouldExtract = true;
                }

                if (shouldExtract && !string.IsNullOrEmpty(entry.Name))
                {
                    var destinationPath = Path.Combine(extractTo, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                    entry.ExtractToFile(destinationPath, true);
                }
            }
        }
    }

    private void ProcessVersionJson()
    {
        var forgejsonfile = Path.Combine(_rootInfo.InstallPath, DictionaryGameRoot.VersionsPath, _rootInfo.InstallName,
            "install_temp_dir", "version.json");

        var valjson = InstallHelper.GetClientJsonEntry(_rootInfo);
        var forgjson = ForgeVersionInfo.FromJson(forgejsonfile);

        forgjson.Arguments.Game.ForEach(x => valjson.Arguments.Game.Add(x));
        forgjson.Arguments.Jvm.ForEach(x => valjson.Arguments.Jvm.Add(x));

        valjson.MainClass = forgjson.MainClass;
        forgjson.Libraries.ForEach(x =>
        {
            bool hany = false;
            valjson.Libraries.ForEach(x1 =>
            {
                if (x1.Name == x.Name) hany = true;
            });
            if (!hany)
            {
                var url = string.IsNullOrEmpty(x.Downloads.Artifact.Url) ?
                    DictionaryDownloadHost.Sources.LibrariesHost + x.Downloads.Artifact.Path :
                    x.Downloads.Artifact.Url;

                valjson.Libraries.Add(new ManifestClientJson.Library()
                {
                    Name = x.Name,
                    Downloads = new ManifestClientJson.LibraryDownloads()
                    {
                        Artifact = new ManifestClientJson.Artifact()
                        {
                            Path = x.Downloads.Artifact.Path,
                            Sha1 = x.Downloads.Artifact.Sha1,
                            Size = (int)x.Downloads.Artifact.Size,
                            Url = url
                        }
                    }
                });
            }
        });

        // 确保添加 Forge universal jar 引用
        EnsureForgeUniversalJar(valjson);

        InstallHelper.SaveClientJson(valjson, _rootInfo);
    }

    private void EnsureForgeUniversalJar(ManifestClientJson valjson)
    {
        // 检查多种可能的 Forge jar 引用
        var jarVariants = new[]
        {
            $"net.minecraftforge:forge:{_installVersion}:universal",
            $"net.minecraftforge:forge:{_installVersion}",
            $"net.minecraftforge:forge:{_installVersion}:client"
        };

        bool hasForgeJar = false;
        string? existingJarName = null;

        // 检查是否已经存在任何 Forge jar 引用
        foreach (var jarName in jarVariants)
        {
            foreach (var lib in valjson.Libraries)
            {
                if (lib.Name == jarName)
                {
                    hasForgeJar = true;
                    existingJarName = jarName;
                    Console.WriteLine($"找到现有的 Forge jar 引用: {jarName}");
                    break;
                }
            }
            if (hasForgeJar) break;
        }

        if (!hasForgeJar)
        {
            // 默认添加 universal jar 引用
            var universalJarName = $"net.minecraftforge:forge:{_installVersion}:universal";
            Console.WriteLine($"添加 Forge universal jar 引用: {universalJarName}");

            var universalJarPath = $"net/minecraftforge/forge/{_installVersion}/forge-{_installVersion}-universal.jar";
            var universalJarUrl = DictionaryDownloadHost.Sources.LibrariesHost + universalJarPath;

            valjson.Libraries.Add(new ManifestClientJson.Library()
            {
                Name = universalJarName,
                Downloads = new ManifestClientJson.LibraryDownloads()
                {
                    Artifact = new ManifestClientJson.Artifact()
                    {
                        Path = universalJarPath,
                        Sha1 = "", // 这个会在 processors 运行后生成
                        Size = 0,  // 这个会在 processors 运行后确定
                        Url = universalJarUrl
                    }
                }
            });
        }
        else
        {
            Console.WriteLine($"Forge jar 引用已存在: {existingJarName}");
        }
    }

    private async Task DownloadLibraries()
    {
        var installProfilePath = Path.Combine(_temppath, "install_profile.json");
        if (!File.Exists(installProfilePath))
        {
            Console.WriteLine("install_profile.json 不存在，跳过 libraries 下载");
            return;
        }

        var installProfile = ForgeInstallProfile.FromJson(installProfilePath);
        if (installProfile?.Libraries == null || installProfile.Libraries.Count == 0)
        {
            Console.WriteLine("没有需要下载的 libraries");
            return;
        }

        // 准备下载列表
        var librariesToDownload = new List<DownloadListEntry.DownloadFileItem>();

        foreach (var library in installProfile.Libraries)
        {
            if (library.Downloads?.Artifact != null)
            {
                var libraryPath = Path.Combine(_rootInfo.InstallPath, DictionaryGameRoot.LibrariesPath, library.Downloads.Artifact.Path);

                librariesToDownload.Add(new DownloadListEntry.DownloadFileItem()
                {
                    Type = FileType.JarFile,
                    FileInfo = new DownloadFileInfo()
                    {
                        FileName = libraryPath,
                        Url = library.Downloads.Artifact.Url,
                        Size = (ulong)library.Downloads.Artifact.Size,
                        Hash = library.Downloads.Artifact.Sha1
                    }
                });
            }
        }

        if (librariesToDownload.Count == 0)
        {
            Console.WriteLine("没有有效的 libraries 需要下载");
            return;
        }

        // 统计文件类型
        var stats = new FileTypeStats { TotalFiles = librariesToDownload.Count };
        _fileTypeStats[FileType.JarFile] = stats;

        // 预检查文件
        foreach (var file in librariesToDownload)
        {
            if (File.Exists(file.FileInfo.FileName))
            {
                var fileInfo = new FileInfo(file.FileInfo.FileName);
                if (fileInfo.Length == (long)file.FileInfo.Size)
                {
                    stats.SkippedFiles++;
                }
                else
                {
                    stats.NeedDownloadFiles++;
                }
            }
            else
            {
                stats.NeedDownloadFiles++;
            }
        }

        Console.WriteLine($"[Libraries] 总文件 {stats.TotalFiles} 个，需要下载 {stats.NeedDownloadFiles} 个，跳过 {stats.SkippedFiles} 个");

        // 下载文件
        var semaphore = new SemaphoreSlim(MaxParallelDownloads);
        var lockObj = new object();
        var downloadTasks = librariesToDownload.Select(file => DownloadLibraryFileWithRetry(file, semaphore, lockObj));

        await Task.WhenAll(downloadTasks);

        Console.WriteLine($"[Libraries] 下载完成: 成功 {stats.CompletedFiles}/{stats.TotalFiles} 个，失败 {stats.FailedFiles} 个");
    }

    private async Task DownloadLibraryFileWithRetry(DownloadListEntry.DownloadFileItem file, SemaphoreSlim semaphore, object lockObj)
    {
        await semaphore.WaitAsync();
        try
        {
            var success = false;
            var skipped = false;
            Exception? lastException = null;

            // 检查文件是否已存在且大小正确
            if (File.Exists(file.FileInfo.FileName))
            {
                var fileInfo = new FileInfo(file.FileInfo.FileName);
                if (fileInfo.Length == (long)file.FileInfo.Size)
                {
                    success = true;
                    skipped = true;
                }
            }

            if (!skipped)
            {
                // 重试机制
                for (int attempt = 0; attempt < RetryAttempts; attempt++)
                {
                    try
                    {
                        await DownloadFileAsync(file.FileInfo);
                        success = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        if (attempt < RetryAttempts - 1)
                        {
                            await Task.Delay(RetryDelay);
                        }
                    }
                }
            }

            lock (lockObj)
            {
                var stats = _fileTypeStats[FileType.JarFile];
                if (success)
                {
                    stats.CompletedFiles++;
                }
                else
                {
                    stats.FailedFiles++;
                    Console.WriteLine($"下载失败: {Path.GetFileName(file.FileInfo.FileName)} - {lastException?.Message}");
                }

                // 计算进度
                var progress = (double)stats.CompletedFiles / stats.TotalFiles * 100;
                var status = success ? DownloadStatusType.DownloadLibrary : DownloadStatusType.Error;

                DownloadStatusChanged?.Invoke(this, new DownloadStatusChangedEntry()
                {
                    Progress = progress,
                    Status = status,
                    FileType = FileType.JarFile,
                    CurrentFileName = Path.GetFileName(file.FileInfo.FileName),
                    CompletedFiles = stats.CompletedFiles,
                    TotalFiles = stats.TotalFiles
                });
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task RunProcessors()
    {
        var installProfilePath = Path.Combine(_temppath, "install_profile.json");
        if (!File.Exists(installProfilePath))
        {
            Console.WriteLine("install_profile.json 不存在，跳过 processors 运行");
            return;
        }

        var installProfile = ForgeInstallProfile.FromJson(installProfilePath);
        if (installProfile?.Processors == null || installProfile.Processors.Count == 0)
        {
            Console.WriteLine("没有需要运行的 processors");
            return;
        }

        Console.WriteLine($"开始运行 {installProfile.Processors.Count} 个 processors");

        for (int i = 0; i < installProfile.Processors.Count; i++)
        {
            var processor = installProfile.Processors[i];

            // 检查 sides 字段，如果只有 server 则跳过
            if (processor.Sides != null && processor.Sides.Contains("server") && !processor.Sides.Contains("client"))
            {
                Console.WriteLine($"跳过服务端 processor [{i + 1}/{installProfile.Processors.Count}]");
                continue;
            }

            Console.WriteLine($"运行 processor [{i + 1}/{installProfile.Processors.Count}]: {processor.Jar}");

            try
            {
                await RunSingleProcessor(processor, installProfile.Data);
                Console.WriteLine($"Processor [{i + 1}/{installProfile.Processors.Count}] 运行成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Processor [{i + 1}/{installProfile.Processors.Count}] 运行失败: {ex.Message}");
                throw;
            }
        }

        Console.WriteLine("所有 processors 运行完成");
    }

    private async Task RunSingleProcessor(ForgeInstallProfile.Processor processor, Dictionary<string, ForgeInstallProfile.DataEntry> data)
    {
        Console.WriteLine($"处理 processor: {processor.Jar}");

        // 1. 获取主类
        var mainClass = await GetMainClassFromJar(processor.Jar);
        if (string.IsNullOrEmpty(mainClass))
        {
            throw new Exception($"无法从 {processor.Jar} 获取主类");
        }
        Console.WriteLine($"主类: {mainClass}");

        // 2. 构建 classpath
        var classpath = BuildClasspath(processor);
        Console.WriteLine($"Classpath 包含 {processor.Classpath.Count + 1} 个 jar 文件");

        // 3. 替换参数中的字符串模板
        var args = ReplaceTemplateStrings(processor.Args, data);
        Console.WriteLine($"参数: {string.Join(" ", args)}");

        // 4. 构建完整的 Java 命令
        var javaCommand = BuildJavaCommand(mainClass, classpath, args);
        Console.WriteLine($"执行命令: {javaCommand}");

        // 5. 运行命令
        await RunJavaCommand(javaCommand);

        // 6. 验证输出文件（如果有）
        if (processor.Outputs != null)
        {
            Console.WriteLine($"验证 {processor.Outputs.Count} 个输出文件...");
            ValidateOutputs(processor.Outputs, data);
        }

        Console.WriteLine($"Processor {processor.Jar} 完成");
    }

    private async Task<string> GetMainClassFromJar(string jarName)
    {
        var jarPath = Path.Combine(_rootInfo.InstallPath, DictionaryGameRoot.LibrariesPath, ConvertMavenNameToPath(jarName));

        if (!File.Exists(jarPath))
        {
            throw new FileNotFoundException($"找不到 jar 文件: {jarPath}");
        }

        using var archive = ZipFile.OpenRead(jarPath);
        var manifestEntry = archive.GetEntry("META-INF/MANIFEST.MF");

        if (manifestEntry == null)
        {
            throw new Exception($"jar 文件中没有 MANIFEST.MF: {jarPath}");
        }

        using var stream = manifestEntry.Open();
        using var reader = new StreamReader(stream);
        var manifestContent = await reader.ReadToEndAsync();

        // 解析 Main-Class
        var lines = manifestContent.Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith("Main-Class:"))
            {
                return line.Substring("Main-Class:".Length).Trim();
            }
        }

        throw new Exception($"在 MANIFEST.MF 中找不到 Main-Class: {jarPath}");
    }

    private string BuildClasspath(ForgeInstallProfile.Processor processor)
    {
        var classpathItems = new List<string>();

        // 添加 classpath 中的所有 jar
        foreach (var item in processor.Classpath)
        {
            var jarPath = Path.Combine(_rootInfo.InstallPath, DictionaryGameRoot.LibrariesPath, ConvertMavenNameToPath(item));
            classpathItems.Add(jarPath);
        }

        // 添加主 jar
        var mainJarPath = Path.Combine(_rootInfo.InstallPath, DictionaryGameRoot.LibrariesPath, ConvertMavenNameToPath(processor.Jar));
        classpathItems.Add(mainJarPath);

        return string.Join(Path.PathSeparator, classpathItems);
    }

    private List<string> ReplaceTemplateStrings(List<string> args, Dictionary<string, ForgeInstallProfile.DataEntry> data)
    {
        var replacedArgs = new List<string>();

        foreach (var arg in args)
        {
            var replacedArg = arg;

            // 替换 data 中的模板
            foreach (var kvp in data)
            {
                var template = "{" + kvp.Key + "}";
                if (replacedArg.Contains(template))
                {
                    // 使用客户端数据
                    replacedArg = replacedArg.Replace(template, kvp.Value.Client);
                }
            }

            // 替换固定模板
            replacedArg = replacedArg.Replace("{INSTALLER}", _installerJarPath);
            replacedArg = replacedArg.Replace("{ROOT}", _rootInfo.InstallPath);
            replacedArg = replacedArg.Replace("{MINECRAFT_JAR}", Path.Combine(_rootInfo.InstallPath, DictionaryGameRoot.VersionsPath, _rootInfo.InstallName, $"{_rootInfo.InstallName}.jar"));
            replacedArg = replacedArg.Replace("{SIDE}", "client");

            // 处理相对路径（以 / 开头的路径是相对于 data 目录的）
            if (replacedArg.StartsWith("/data/"))
            {
                replacedArg = Path.Combine(_temppath, replacedArg.Substring(1).Replace('/', Path.DirectorySeparatorChar));
            }

            // 处理 Maven 坐标格式的路径
            if (replacedArg.StartsWith("[") && replacedArg.EndsWith("]"))
            {
                var mavenCoord = replacedArg.Substring(1, replacedArg.Length - 2);
                replacedArg = Path.Combine(_rootInfo.InstallPath, DictionaryGameRoot.LibrariesPath, ConvertMavenNameToPath(mavenCoord));
            }

            replacedArgs.Add(replacedArg);
        }

        return replacedArgs;
    }

    private string BuildJavaCommand(string mainClass, string classpath, List<string> args)
    {
        var javaArgs = new List<string>
        {
            "java",
            "-cp",
            $"\"{classpath}\"",
            mainClass
        };

        javaArgs.AddRange(args.Select(arg => $"\"{arg}\""));

        return string.Join(" ", javaArgs);
    }

    private async Task RunJavaCommand(string command)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {command}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _rootInfo.InstallPath
        };

        using var process = new Process { StartInfo = processStartInfo };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                outputBuilder.AppendLine(e.Data);
                Console.WriteLine($"[Processor] {e.Data}");
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                errorBuilder.AppendLine(e.Data);
                Console.WriteLine($"[Processor Error] {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Processor 运行失败，退出代码: {process.ExitCode}\n错误输出: {errorBuilder}");
        }
    }

    private void ValidateOutputs(Dictionary<string, string> outputs, Dictionary<string, ForgeInstallProfile.DataEntry> data)
    {
        foreach (var output in outputs)
        {
            var filePath = ReplaceTemplateStrings(new List<string> { output.Key }, data)[0];
            var expectedSha1 = ReplaceTemplateStrings(new List<string> { output.Value }, data)[0].Trim('\'');

            if (!File.Exists(filePath))
            {
                throw new Exception($"输出文件不存在: {filePath}");
            }

            var actualSha1 = CalculateFileSha1(filePath);
            if (!string.Equals(actualSha1, expectedSha1, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"输出文件 SHA1 校验失败: {filePath}\n期望: {expectedSha1}\n实际: {actualSha1}");
            }

            Console.WriteLine($"输出文件校验成功: {Path.GetFileName(filePath)}");
        }
    }

    private string ConvertMavenNameToPath(string mavenName)
    {
        // 转换 Maven 坐标为文件路径
        // 支持格式：
        // group:artifact:version -> group/artifact/version/artifact-version.jar
        // group:artifact:version:classifier -> group/artifact/version/artifact-version-classifier.jar
        // group:artifact:version:classifier@type -> group/artifact/version/artifact-version-classifier.type

        // 处理 @type 后缀
        string extension = "jar";
        string coordinates = mavenName;

        if (mavenName.Contains("@"))
        {
            var atIndex = mavenName.LastIndexOf('@');
            extension = mavenName.Substring(atIndex + 1);
            coordinates = mavenName.Substring(0, atIndex);
        }

        var parts = coordinates.Split(':');

        if (parts.Length < 3)
        {
            throw new ArgumentException($"无效的 Maven 坐标: {mavenName}");
        }

        var groupId = parts[0].Replace('.', '/');
        var artifactId = parts[1];
        var version = parts[2];
        var classifier = "";

        // 处理分类器：如果有超过3个部分，将第4部分及之后的所有部分作为分类器
        if (parts.Length > 3)
        {
            classifier = string.Join(":", parts.Skip(3));
        }

        string fileName;
        if (string.IsNullOrEmpty(classifier))
        {
            fileName = $"{artifactId}-{version}.{extension}";
        }
        else
        {
            fileName = $"{artifactId}-{version}-{classifier}.{extension}";
        }

        return Path.Combine(groupId, artifactId, version, fileName);
    }

    private string CalculateFileSha1(string filePath)
    {
        using var sha1 = SHA1.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha1.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private void ValidateInstallation()
    {
        Console.WriteLine("验证 Forge 安装结果...");

        // 检查多种可能的 Forge jar 文件
        var forgeLibPath = Path.Combine(_rootInfo.InstallPath, DictionaryGameRoot.LibrariesPath,
            "net", "minecraftforge", "forge", _installVersion);

        var jarVariants = new[]
        {
            $"forge-{_installVersion}-universal.jar",
            $"forge-{_installVersion}.jar",
            $"forge-{_installVersion}-client.jar"
        };

        bool foundForgeJar = false;
        foreach (var jarName in jarVariants)
        {
            var jarPath = Path.Combine(forgeLibPath, jarName);
            if (File.Exists(jarPath))
            {
                Console.WriteLine($"✓ 找到 Forge jar 文件: {jarName}");
                foundForgeJar = true;
            }
        }

        if (!foundForgeJar)
        {
            Console.WriteLine($"❌ 没有找到任何 Forge jar 文件！");
            Console.WriteLine("这可能是导致启动失败的原因！");
            Console.WriteLine($"检查目录: {forgeLibPath}");
        }

        // 检查版本 JSON 文件
        var versionJsonPath = Path.Combine(_rootInfo.InstallPath, DictionaryGameRoot.VersionsPath,
            _rootInfo.InstallName, $"{_rootInfo.InstallName}.json");

        if (!File.Exists(versionJsonPath))
        {
            Console.WriteLine($"警告: 版本 JSON 文件不存在: {versionJsonPath}");
        }
        else
        {
            Console.WriteLine($"✓ 版本 JSON 文件存在: {Path.GetFileName(versionJsonPath)}");

            // 检查 JSON 内容
            try
            {
                var jsonContent = File.ReadAllText(versionJsonPath);
                if (jsonContent.Contains("net.minecraftforge.fml.loading.FMLClientLaunchProvider"))
                {
                    Console.WriteLine("✓ 版本 JSON 包含正确的 Forge 主类");
                }
                else
                {
                    Console.WriteLine("警告: 版本 JSON 可能不包含正确的 Forge 主类");
                }

                // 检查是否包含 universal jar 引用
                if (jsonContent.Contains($"forge-{_installVersion}-universal"))
                {
                    Console.WriteLine("✓ 版本 JSON 包含 universal jar 引用");
                }
                else
                {
                    Console.WriteLine("❌ 版本 JSON 缺少 universal jar 引用");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"警告: 无法读取版本 JSON 文件: {ex.Message}");
            }
        }

        Console.WriteLine("Forge 安装验证完成");
    }

    private async Task EnsureUniversalJarExists()
    {
        // 检查多种可能的 jar 文件名
        var jarVariants = new[]
        {
            $"forge-{_installVersion}-universal.jar",
            $"forge-{_installVersion}.jar",
            $"forge-{_installVersion}-client.jar"
        };

        var forgeLibPath = Path.Combine(_rootInfo.InstallPath, DictionaryGameRoot.LibrariesPath,
            "net", "minecraftforge", "forge", _installVersion);

        string? existingJarPath = null;
        string? targetJarName = null;

        // 检查哪种 jar 文件已经存在
        foreach (var jarName in jarVariants)
        {
            var jarPath = Path.Combine(forgeLibPath, jarName);
            if (File.Exists(jarPath))
            {
                existingJarPath = jarPath;
                Console.WriteLine($"✓ 找到现有的 Forge jar: {jarName}");
                break;
            }
        }

        // 确定需要的目标 jar 文件名（根据 version.json 中的引用）
        var versionJsonPath = Path.Combine(_rootInfo.InstallPath, DictionaryGameRoot.VersionsPath,
            _rootInfo.InstallName, $"{_rootInfo.InstallName}.json");

        if (File.Exists(versionJsonPath))
        {
            try
            {
                var jsonContent = File.ReadAllText(versionJsonPath);

                // 检查 JSON 中引用的是哪种 jar 文件
                foreach (var jarName in jarVariants)
                {
                    if (jsonContent.Contains(jarName))
                    {
                        targetJarName = jarName;
                        Console.WriteLine($"版本 JSON 引用的 jar 文件: {jarName}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取版本 JSON 失败: {ex.Message}");
            }
        }

        // 如果没有找到引用，默认使用 universal jar
        if (string.IsNullOrEmpty(targetJarName))
        {
            targetJarName = $"forge-{_installVersion}-universal.jar";
        }

        var targetJarPath = Path.Combine(forgeLibPath, targetJarName);

        // 如果目标 jar 不存在，尝试创建或下载
        if (!File.Exists(targetJarPath))
        {
            Console.WriteLine($"目标 jar 不存在，尝试创建: {targetJarName}");

            if (!string.IsNullOrEmpty(existingJarPath))
            {
                try
                {
                    // 创建目录
                    Directory.CreateDirectory(forgeLibPath);

                    // 复制现有的 jar 文件
                    File.Copy(existingJarPath, targetJarPath, true);
                    Console.WriteLine($"✓ 已从 {Path.GetFileName(existingJarPath)} 创建 {targetJarName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"复制 jar 文件失败: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"❌ 没有找到任何现有的 Forge jar 文件");

                // 尝试下载目标 jar
                await TryDownloadForgeJar(targetJarPath, targetJarName);
            }
        }
        else
        {
            Console.WriteLine($"✓ 目标 jar 已存在: {targetJarName}");
        }
    }

    private async Task TryDownloadForgeJar(string jarPath, string jarName)
    {
        try
        {
            Console.WriteLine($"尝试下载 {jarName}...");

            // 尝试多个可能的下载 URL
            var possibleUrls = new[]
            {
                DictionaryDownloadHost.Sources.LibrariesHost + $"net/minecraftforge/forge/{_installVersion}/{jarName}",
                DictionaryDownloadHost.Sources.LibrariesHost + $"net/minecraftforge/forge/{_installVersion}/forge-{_installVersion}-universal.jar",
                DictionaryDownloadHost.Sources.LibrariesHost + $"net/minecraftforge/forge/{_installVersion}/forge-{_installVersion}.jar"
            };

            foreach (var url in possibleUrls)
            {
                try
                {
                    var downloadInfo = new DownloadFileInfo
                    {
                        FileName = jarPath,
                        Url = url,
                        Size = 0, // 未知大小
                        Hash = "" // 未知哈希
                    };

                    await DownloadFileAsync(downloadInfo);
                    Console.WriteLine($"✓ {jarName} 下载成功，来源: {url}");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"从 {url} 下载失败: {ex.Message}");
                }
            }

            Console.WriteLine($"❌ 所有下载尝试都失败了");
            Console.WriteLine("这可能导致启动器无法找到必需的 Forge 文件");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"下载 {jarName} 时发生错误: {ex.Message}");
        }
    }

    private void CleanupTempFiles()
    {
        try
        {
            if (Directory.Exists(_temppath))
            {
                Directory.Delete(_temppath, true);
                Console.WriteLine("清理临时文件完成");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"清理临时文件失败: {ex.Message}");
        }
    }
}