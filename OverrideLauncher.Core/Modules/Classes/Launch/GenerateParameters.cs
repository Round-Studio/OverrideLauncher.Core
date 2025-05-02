using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OverrideLauncher.Core.Modules.Entry.GameEntry;
using OverrideLauncher.Core.Modules.Entry.LaunchEntry;

namespace OverrideLauncher.Core.Modules.Classes.Launch;

public class GenerateParameters
{
    private GameJsonEntry GameJsonEntry { get; set; }
    private GameInstancesInfo GameInfo { get; set; }
    private LaunchRunnerInfo LaunchRunnerInfo { get; set; }
    private string NativePath = "";
    public GenerateParameters(LaunchRunnerInfo launchRunnerInfo)
    {
        this.GameInfo = launchRunnerInfo.GameInstances.GameInstances;
        this.LaunchRunnerInfo = launchRunnerInfo;
        GameInfo.GameCatalog = Path.GetFullPath(GameInfo.GameCatalog);
        string GameJsonPath =
            Path.Combine(GameInfo.GameCatalog, "versions", GameInfo.GameName, $"{GameInfo.GameName}.json");
        
        if (!File.Exists(GameJsonPath))
        {
            throw new FileNotFoundException("找不到 GameJson 文件！");
        }

        NativePath = Path.Combine(GameInfo.GameCatalog, "versions", GameInfo.GameName, "natives");
        
        string GameJson = File.ReadAllText(GameJsonPath);
        GameJsonEntry = JsonConvert.DeserializeObject<GameJsonEntry>(GameJson);
    }
    
    private void UnzipNativePacks(List<Artifact> NativePacks)
    {
        if (Directory.Exists(NativePath)) Directory.CreateDirectory(NativePath);
        foreach (var packs in NativePacks)
        {
            var path = Path.Combine(GameInfo.GameCatalog, "libraries", packs.Path);
            if (File.Exists(path))
            {
                ZipFile.ExtractToDirectory(path, NativePath,true);
            }
            else
            {
                throw new FileNotFoundException($"找不到 NativePack： {path}");
            }
        }

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
    }

    public string SplicingArguments()
    {
        Dictionary<string, string> Args = new Dictionary<string, string>()
        {
            ["${natives_directory}"] = $"\"{NativePath}\"",
            ["${classpath}"] = $"\"{SplicingCPArguments()}\"",
            ["${main_class}"] = GameJsonEntry.MainClass,
            ["${game_directory}"] = $"\"{Path.Combine(GameInfo.GameCatalog, "versions", GameInfo.GameName)}\"",
            ["${assets_root}"] = $"\"{Path.Combine(GameInfo.GameCatalog, "assets")}\"",
            ["${assets_index_name}"] = GameJsonEntry.AssetIndex.Id,
            ["${version_name}"] = GameInfo.GameName,
            ["${auth_uuid}"] = LaunchRunnerInfo.Account.UUID,
            ["${auth_access_token}"] = LaunchRunnerInfo.Account.Token,
            ["${user_type}"] = LaunchRunnerInfo.Account.AccountType,
            ["${version_type}"] = LaunchRunnerInfo.LauncherInfo,
            ["${launcher_name}"] = LaunchRunnerInfo.LauncherInfo,
            ["${launcher_version}"] = LaunchRunnerInfo.LauncherVersion,
            ["${auth_player_name}"] = LaunchRunnerInfo.Account.UserName
        };

        var jvm = GetJVMArguments();
        var game = GetGameArguments();
        var args = new List<string>();
        
        foreach (var item in jvm)
        {
            var tmp = item;
            foreach (var arg in Args)
            {
                if (item.Contains(arg.Key))
                {
                    tmp = item.Replace(arg.Key, arg.Value);
                }
            }
            args.Add(tmp);
        }
        foreach (var item in game)
        {
            var tmp = item;
            foreach (var arg in Args)
            {
                if (item.Contains(arg.Key))
                {
                    tmp = item.Replace(arg.Key, arg.Value);
                }
            }
            args.Add(tmp);
        }
        
        if(LaunchRunnerInfo.IsDemo) args.Add("--demo");
        return string.Join(" ", args);
    }
    private string SplicingCPArguments()
    {
        var librarypath = Path.Combine(GameInfo.GameCatalog, "libraries");
        var cp = new List<string>();
        var natives = new List<Artifact>();
        var os = RuntimeInformation.OSDescription.ToLower();
        foreach (var cpitem in GameJsonEntry.Libraries)
        {
            var path = Path.GetFullPath(Path.Combine(librarypath, cpitem.Downloads.Artifact.Path.Replace("3.2.1","3.2.2")));
            if(!cp.Contains(path)) cp.Add(path);
            
            if (cpitem.Downloads.Classifiers != null)
            {
                if (os.Contains("windows") && cpitem.Downloads.Classifiers.Keys.Contains("natives-windows"))
                {
                    var item = cpitem.Downloads.Classifiers["natives-windows"];
                    if (item != null) natives.Add(item);
                }
                else if (os.Contains("macos") || os.Contains("darwin") && cpitem.Downloads.Classifiers.Keys.Contains("natives-macos"))
                {
                    var item = cpitem.Downloads.Classifiers["natives-macos"];
                    if (item != null) natives.Add(item);
                }
                else if (os.Contains("linux") && cpitem.Downloads.Classifiers.Keys.Contains("natives-linux"))
                {
                    var item = cpitem.Downloads.Classifiers["natives-linux"];
                    if (item != null) natives.Add(item);
                }
            }
        }
        UnzipNativePacks(natives);
        cp.Add(Path.Combine(GameInfo.GameCatalog, "versions", GameInfo.GameName, $"{GameInfo.GameName}.jar"));
        return string.Join(";", cp);
    }

    private List<string> GetJVMArguments()
    {
        var jvmArgs = new List<string>();
        var os = RuntimeInformation.OSDescription.ToLower();
        var args = new List<string>();

        foreach (var jvmItem in GameJsonEntry.Arguments.Jvm)
        {
            if (jvmItem is string)
            {
                args.Add(jvmItem.ToString());
            }else if (jvmItem is JObject ruleObject)
            {
                var rules = ruleObject["rules"]?.ToObject<List<Rule>>();
                var value = ruleObject["value"];

                if (rules != null && value != null)
                {
                    foreach (var rule in rules)
                    {
                        if (rule.Action == "allow")
                        {
                            if (!args.Contains(rule.ToString()))
                            {
                                if (os.Contains("windows") && rule.Os?.Name == "windows")
                                {
                                    if(value is string) args.Add(value.ToString());
                                }
                                else if (os.Contains("macos") || os.Contains("darwin") && rule.Os?.Name == "osx")
                                {
                                    args.Add(value.ToString());
                                }
                                else if (os.Contains("linux") && rule.Os?.Name == "linux")
                                {
                                    args.Add(value.ToString());
                                }
                            }
                        }
                    }
                }
            }
        }
        
        args.Add("${main_class}");
        return args;
    }
    private List<string> GetGameArguments()
    {
        var gameArgs = new List<string>();
        var os = RuntimeInformation.OSDescription.ToLower();
        var args = new List<string>();

        foreach (var jvmItem in GameJsonEntry.Arguments.Game)
        {
            if (jvmItem is string)
            {
                args.Add(jvmItem.ToString());
            }else if (jvmItem is JObject ruleObject)
            {
                var rules = ruleObject["rules"]?.ToObject<List<Rule>>();
                var value = ruleObject["value"];

                if (rules != null && value != null)
                {
                    foreach (var rule in rules)
                    {
                        
                    }
                }
            }
        }
        
        return args;
    }
}