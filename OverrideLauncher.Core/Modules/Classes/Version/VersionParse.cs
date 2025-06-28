
using System.Text.Json;
using OverrideLauncher.Core.Modules.Entry.GameEntry;

namespace OverrideLauncher.Core.Modules.Classes.Version;

public class VersionParse
{
    public GameJsonEntry GameJson { get; private set; }
    public ClientInstancesInfo ClientInstances { get; private set; }

    public VersionParse(ClientInstancesInfo Info)
    {
        ClientInstances = Info;
        var file = Path.Combine(Info.GameCatalog, "versions", Info.GameName, $"{Info.GameName}.json");
        GameJson = JsonSerializer.Deserialize<GameJsonEntry>(File.ReadAllText(file));
    }
}