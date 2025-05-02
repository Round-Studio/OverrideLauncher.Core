using Newtonsoft.Json;
using OverrideLauncher.Core.Modules.Entry.GameEntry;

namespace OverrideLauncher.Core.Modules.Classes.Version;

public class VersionParse
{
    public GameJsonEntry GameJson { get; private set; }
    public GameInstancesInfo GameInstances { get; private set; }

    public VersionParse(GameInstancesInfo Info)
    {
        GameInstances = Info;
        var file = Path.Combine(Info.GameCatalog, "versions", Info.GameName, $"{Info.GameName}.json");
        GameJson = JsonConvert.DeserializeObject<GameJsonEntry>(File.ReadAllText(file));
    }
}