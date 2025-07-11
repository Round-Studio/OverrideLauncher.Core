﻿using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using OverrideLauncher.Core.Modules.Entry.GameEntry;
using OverrideLauncher.Core.Modules.Entry.LaunchEntry;

namespace OverrideLauncher.Core.Modules.Classes.Launch.Client;

public class GenerateClientParameters
{
    private GameJsonEntry GameJsonEntry { get; set; }
    private ClientInstancesInfo ClientInfo { get; set; }
    private ClientRunnerInfo ClientRunnerInfo { get; set; }
    private string NativePath = "";
    public GenerateClientParameters(ClientRunnerInfo clientRunnerInfo)
    {
        this.ClientInfo = clientRunnerInfo.GameInstances.ClientInstances;
        this.ClientRunnerInfo = clientRunnerInfo;
        ClientInfo.GameCatalog = Path.GetFullPath(ClientInfo.GameCatalog);
        string GameJsonPath =
            Path.Combine(ClientInfo.GameCatalog, "versions", ClientInfo.GameName, $"{ClientInfo.GameName}.json");
        
        if (!File.Exists(GameJsonPath))
        {
            throw new FileNotFoundException("找不到 GameJson 文件！");
        }

        NativePath = Path.Combine(ClientInfo.GameCatalog, "versions", ClientInfo.GameName, "natives");
        
        string GameJson = File.ReadAllText(GameJsonPath);
        GameJsonEntry = JsonSerializer.Deserialize<GameJsonEntry>(GameJson);
    }
    
    private void UnzipNativePacks(List<Artifact> NativePacks)
    {
        if (Directory.Exists(NativePath)) Directory.CreateDirectory(NativePath);
        foreach (var packs in NativePacks)
        {
            var path = Path.Combine(ClientInfo.GameCatalog, "libraries", packs.Path);
            if (File.Exists(path))
            {
                ZipFile.ExtractToDirectory(path, NativePath,true);
            }
            else
            {
                throw new FileNotFoundException($"找不到 NativePack： {path}");
            }
        }

        try
        {
            var dirfiles = Directory.GetFiles(NativePath);
            foreach (var file in dirfiles)
            {
                if (file.EndsWith(".git") || file.EndsWith(".sha1") || file.EndsWith(".LIST") || file.EndsWith(".class"))
                {
                    File.Delete(file);
                }
            }

            if (Directory.Exists(Path.Combine(NativePath, "META-INF")))
                Directory.Delete(Path.Combine(NativePath, "META-INF"),true);
        }catch{ }
    }

    public string SplicingArguments()
    {
        Dictionary<string, string> Args = new Dictionary<string, string>()
        {
            ["${natives_directory}"] = $"\"{NativePath}\"",
            ["${library_directory}"] = $"\"{Path.Combine(ClientInfo.GameCatalog, "libraries")}\"",
            ["${classpath}"] = $"\"{SplicingCPArguments()}\"",
            ["${main_class}"] = GameJsonEntry.MainClass,
            ["${game_directory}"] = $"\"{Path.Combine(ClientInfo.GameCatalog, "versions", ClientInfo.GameName)}\"",
            ["${assets_root}"] = $"\"{Path.Combine(ClientInfo.GameCatalog, "assets")}\"",
            ["${assets_index_name}"] = GameJsonEntry.AssetIndex.Id,
            ["${version_name}"] = ClientInfo.GameName,
            ["${auth_uuid}"] = ClientRunnerInfo.Account.UUID,
            ["${auth_access_token}"] = ClientRunnerInfo.Account.Token,
            ["${user_type}"] = ClientRunnerInfo.Account.AccountType,
            ["${version_type}"] = $"\"{ClientRunnerInfo.LauncherInfo}\"", 
            ["${launcher_name}"] = $"\"{ClientRunnerInfo.LauncherInfo}\"",
            ["${launcher_version}"] = ClientRunnerInfo.LauncherVersion,
            ["${auth_player_name}"] = ClientRunnerInfo.Account.UserName,
            ["${user_properties}"] = "{}",
        };
        
        List<string> RequiredJVMArgs = new()
        {
            "-XX:+UseG1GC -XX:-UseAdaptiveSizePolicy -XX:-OmitStackTraceInFastThrow",
            "-Djava.library.path=${natives_directory}",
            "-Dorg.lwjgl.system.SharedLibraryExtractPath=${natives_directory}",
            "-Dio.netty.native.workdir=${natives_directory}",
            "-Djna.tmpdir=${natives_directory}",
            "-cp",
            "${classpath}"
        };

        var jvm = GetJVMArguments();
        var game = GetGameArguments();
        var args = new List<string>();
        
        RequiredJVMArgs.ForEach(x =>
        {
            if (!jvm.Contains(x)) jvm.Add(x);
        });


        if (!jvm.Contains("${main_class}") || !game.Contains("${main_class}"))
        {
            jvm.Add("${main_class}");
        }

        foreach (var item in jvm)
        {
            var tmp = item;
            foreach (var arg in Args)
            {
                if (item.Contains(arg.Key))
                {
                    tmp = tmp.Replace(arg.Key, arg.Value);
                }
            }
            args.Add(tmp);
        }

        foreach (var item in game)
        {
            if (item == null) continue;
            
            var tmp = item;
            foreach (var arg in Args)
            {
                if (item.Contains(arg.Key))
                {
                    tmp = tmp.Replace(arg.Key, arg.Value);
                }
            }
            args.Add(tmp);
        }

        if (ClientRunnerInfo.IsDemo) args.Add("--demo");
        args.Add($"--width {ClientRunnerInfo.WindowInfo.Width}");
        args.Add($"--height {ClientRunnerInfo.WindowInfo.Height}");
        return string.Join(" ", args);
    }
    private string SplicingCPArguments()
    {
        var librarypath = Path.Combine(ClientInfo.GameCatalog, "libraries");
        var cp = new List<string>();
        var natives = new List<Artifact>();
        var os = RuntimeInformation.OSDescription.ToLower();

        var notos1 = "";
        var notos2 = "";

        if (OperatingSystem.IsWindows())
        {
            os = "windows";
            notos1 = "linux";
            notos2 = "macos";
        }
        if (OperatingSystem.IsMacOS()) 
        {
            os = "macos";
            notos1 = "linux";
            notos2 = "windows";
        }
        if (OperatingSystem.IsLinux()) 
        {
            os = "linux";
            notos1 = "windows";
            notos2 = "macos";
        }
        
        foreach (var cpitem in GameJsonEntry.Libraries)
        {
            if (cpitem.Downloads != null)
            {
                if (cpitem.Name.Contains(os))
                {
                    var rpath = FileHelper.GetJarFilePath(cpitem.Name);
                    var path = Path.GetFullPath(Path.Combine(librarypath, rpath));
           
                    cp.Add(path);
                    continue;
                }
                
                if (cpitem.Downloads.Artifact != null)
                {
                    var path = Path.GetFullPath(Path.Combine(librarypath, Path.Combine(librarypath,cpitem.Downloads.Artifact.Path).Replace("3.2.1","3.2.2")));
                    if (cpitem.Name.Contains("natives") && path.Contains(os)) natives.Add(cpitem.Downloads.Artifact);
                    if (!cp.Contains(path) && !path.Contains("natives") && !path.Contains(notos1) && !path.Contains(notos2)) cp.Add(path);
                }
                
                if (cpitem.Downloads.Classifiers != null)
                {
                    if (os.Contains("windows") && cpitem.Downloads.Classifiers.Keys.Contains("windows"))
                    {
                        var item = cpitem.Downloads.Classifiers["natives-windows"];
                        if (item != null) natives.Add(item);
                    }
                    else if (os.Contains("macos") || os.Contains("darwin") && cpitem.Downloads.Classifiers.Keys.Contains("macos"))
                    {
                        var item = cpitem.Downloads.Classifiers["natives-macos"];
                        if (item != null) natives.Add(item);
                    }
                    else if (os.Contains("linux") && cpitem.Downloads.Classifiers.Keys.Contains("linux"))
                    {
                        var item = cpitem.Downloads.Classifiers["natives-linux"];
                        if (item != null) natives.Add(item);
                    }
                }
            };
        }
        UnzipNativePacks(natives);
        cp.Add(Path.Combine(ClientInfo.GameCatalog, "versions", ClientInfo.GameName, $"{ClientInfo.GameName}.jar"));
        return string.Join(";", cp);
    }

    private List<string> GetJVMArguments()
    {
        if(GameJsonEntry.Arguments==null) return new List<string>();
        var os = RuntimeInformation.OSDescription.ToLower();
        var args = new List<string>();

        foreach (var jvmItem in GameJsonEntry.Arguments.Jvm)
        {
            if (jvmItem is string)
            {
                args.Add(jvmItem.ToString());
            }else if (jvmItem is JsonObject ruleObject)
            {
                // 尝试获取 "rules" 数组并反序列化为 List<Rule>
                if (ruleObject.TryGetPropertyValue("rules", out var rulesNode) &&
                    rulesNode is JsonArray rulesArray)
                {
                    var rules = rulesArray.Deserialize<List<Rule>>(new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // 可选：不区分大小写
                    });

                    // 获取 "value" 字段
                    if (ruleObject.TryGetPropertyValue("value", out var valueNode) &&
                        valueNode != null)
                    {
                        string? value = valueNode.ToString();

                        if (rules != null && value != null)
                        {
                            foreach (var rule in rules)
                            {
                                if (rule.Action == "allow" && !args.Contains(rule.ToString()))
                                {
                                    if (os.Contains("windows") && rule.Os?.Name == "windows")
                                    {
                                        args.Add(value);
                                    }
                                    else if ((os.Contains("macos") || os.Contains("darwin")) && rule.Os?.Name == "osx")
                                    {
                                        args.Add(value);
                                    }
                                    else if (os.Contains("linux") && rule.Os?.Name == "linux")
                                    {
                                        args.Add(value);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        return args;
    }
    private List<string> GetGameArguments()
    {
        var args = new List<string>();

        if (GameJsonEntry.MinecraftArguments != null)
        {
            args.Add(GameJsonEntry.MinecraftArguments);
        }
        else
        {
            Console.WriteLine(GameJsonEntry.Arguments.Game.Count);
            foreach (var jvmItem in GameJsonEntry.Arguments.Game)
            {
                if (jvmItem is JsonElement strruleElement && strruleElement.ValueKind == JsonValueKind.String)
                {
                    args.Add(jvmItem.ToString());
                }else if (jvmItem is JsonElement ruleElement && ruleElement.ValueKind == JsonValueKind.Object)
                {
                    if (ruleElement.TryGetProperty("rules", out var rulesElement) &&
                        rulesElement.ValueKind == JsonValueKind.Array)
                    {
                        var rules = JsonSerializer.Deserialize<List<Rule>>(rulesElement.GetRawText());
        
                        if (ruleElement.TryGetProperty("value", out var valueElement) &&
                            valueElement.ValueKind != JsonValueKind.Null)
                        {
                            foreach (var rule in rules ?? Enumerable.Empty<Rule>())
                            {
                                // 处理每个 rule
                            }
                        }
                    }
                }
            }
        }
        
        return args;
    }
}