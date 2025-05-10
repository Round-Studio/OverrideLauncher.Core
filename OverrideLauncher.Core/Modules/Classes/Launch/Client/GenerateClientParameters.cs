using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        GameJsonEntry = JsonConvert.DeserializeObject<GameJsonEntry>(GameJson);
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
            ["${classpath}"] = $"\"{SplicingCPArguments()}\"",
            ["${main_class}"] = GameJsonEntry.MainClass,
            ["${game_directory}"] = $"\"{Path.Combine(ClientInfo.GameCatalog, "versions", ClientInfo.GameName)}\"",
            ["${assets_root}"] = $"\"{Path.Combine(ClientInfo.GameCatalog, "assets")}\"",
            ["${assets_index_name}"] = GameJsonEntry.AssetIndex.Id,
            ["${version_name}"] = ClientInfo.GameName,
            ["${auth_uuid}"] = ClientRunnerInfo.Account.UUID,
            ["${auth_access_token}"] = ClientRunnerInfo.Account.Token,
            ["${user_type}"] = ClientRunnerInfo.Account.AccountType,
            ["${version_type}"] = $"\"{ClientRunnerInfo.LauncherInfo} - by:OverrideLauncher.Core\"",
            ["${launcher_name}"] = $"\"{ClientRunnerInfo.LauncherInfo} - by:OverrideLauncher.Core\"",
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
        foreach (var cpitem in GameJsonEntry.Libraries)
        {
            if (cpitem.Downloads == null)
            {
                var rpath = FileHelper.GetJarFilePath(cpitem.Name);
                var path = Path.GetFullPath(Path.Combine(librarypath, rpath));
           
                cp.Add(path);
                continue;
            };
            
            if (cpitem.Downloads.Artifact != null)
            {
                var path = Path.GetFullPath(Path.Combine(librarypath, Path.Combine(librarypath,cpitem.Downloads.Artifact.Path).Replace("3.2.1","3.2.2")));
                if (!cp.Contains(path)) cp.Add(path);
            }
            
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
        cp.Add(Path.Combine(ClientInfo.GameCatalog, "versions", ClientInfo.GameName, $"{ClientInfo.GameName}.jar"));
        return string.Join(";", cp);
    }

    private List<string> GetJVMArguments()
    {
        if(GameJsonEntry.Arguments==null) return new List<string>();
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
        
        return args;
    }
    private List<string> GetGameArguments()
    {
        var gameArgs = new List<string>();
        var os = RuntimeInformation.OSDescription.ToLower();
        var args = new List<string>();

        if (GameJsonEntry.Arguments == null)
        {
            args.Add(GameJsonEntry.MinecraftArguments);
        }
        else
        {
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
        }
        
        return args;
    }
}