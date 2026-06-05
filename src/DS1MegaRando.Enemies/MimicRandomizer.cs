using DS1MegaRando.Data.Enemies;
using DS1MegaRando.IO;
using SoulsFormats;

namespace DS1MegaRando.Enemies;

/// <summary>
/// Swaps mimic positions with normal chest positions within each map.
/// The chest's loot lives on its Treasure event (bound by part name), so a
/// position-only swap preserves loot: the player opens what looks like a chest
/// at the mimic's old spot and gets the original chest's loot there, while the
/// mimic now sits where the chest used to be.
/// </summary>
public static class MimicRandomizer
{
    private const string MimicModelId = EnemyIds.Mimic;

    public static void Randomize(GameData gameData, Random rng)
    {
        foreach (var (_, msb) in gameData.Maps)
        {
            var mimics = msb.Parts.Enemies
                .Where(e => string.Equals(e.ModelName, MimicModelId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (mimics.Count == 0) continue;

            // Treasure events bind loot to a specific part by name. Anything
            // referenced from a Treasure event that exists as an Object is a
            // lootable chest in this map.
            var treasurePartNames = msb.Events.Treasures
                .Select(t => t.TreasurePartName)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var chests = msb.Parts.Objects
                .Where(o => treasurePartNames.Contains(o.Name))
                .ToList();
            if (chests.Count == 0) continue;

            // Pair-shuffle: take min(mimics, chests) random pairs and swap
            // their physical placement. Excess entities on either side stay
            // in their original positions.
            int pairCount = Math.Min(mimics.Count, chests.Count);
            var shuffledMimics = mimics.OrderBy(_ => rng.Next()).Take(pairCount).ToList();
            var shuffledChests = chests.OrderBy(_ => rng.Next()).Take(pairCount).ToList();

            for (int i = 0; i < pairCount; i++)
            {
                var m = shuffledMimics[i];
                var c = shuffledChests[i];

                (m.Position, c.Position) = (c.Position, m.Position);
                (m.Rotation, c.Rotation) = (c.Rotation, m.Rotation);
                // Move the collision link with the position so the entity rests
                // on the correct terrain at its new home.
                (m.CollisionName, c.CollisionName) = (c.CollisionName, m.CollisionName);
            }
        }
    }
}
