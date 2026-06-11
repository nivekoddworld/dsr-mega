using DS1Mod.Core;
using SoulsFormats;

namespace DS1Mod.EnemyRandomizer.Core;

/// <summary>
/// Reads game data from MSB files to identify boss slots and collect known models.
/// </summary>
public class GameDataReader
{
    private readonly string _gameDir;

    public GameDataReader(string gameDir)
    {
        _gameDir = gameDir;
    }

    /// <summary>
    /// Read all boss slots from map files. Returns a dict of EntityId → BossDef.
    /// </summary>
    public Dictionary<int, BossDef> ReadBossSlots()
    {
        var result = new Dictionary<int, BossDef>();

        foreach (var boss in BossIds.All)
        {
            string msbPath = Path.Combine(_gameDir, "map", "MapStudio", $"{boss.MapId}.msb");
            if (!File.Exists(msbPath)) continue;

            try
            {
                MSB1 msb = MSB1.Read(msbPath);
                var part = msb.Parts.Enemies.FirstOrDefault(e => e.EntityID == boss.EntityId);
                if (part != null)
                {
                    // Found the boss slot in the MSB
                    var def = boss with { }; // Clone
                    result[boss.EntityId] = def;
                }
            }
            catch
            {
                // Silently skip broken files
            }
        }

        return result;
    }

    /// <summary>
    /// Collect all enemy model IDs present in the game's MSB files.
    /// </summary>
    public HashSet<string> ReadKnownEnemyModels()
    {
        var models = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string msbDir = Path.Combine(_gameDir, "map", "MapStudio");
        if (!Directory.Exists(msbDir)) return models;

        foreach (string msbPath in Directory.GetFiles(msbDir, "*.msb"))
        {
            try
            {
                MSB1 msb = MSB1.Read(msbPath);
                foreach (var enemy in msb.Parts.Enemies)
                {
                    if (!string.IsNullOrEmpty(enemy.ModelName))
                    {
                        models.Add(enemy.ModelName);
                    }
                }
            }
            catch
            {
                // Silently skip broken files
            }
        }

        return models;
    }
}
