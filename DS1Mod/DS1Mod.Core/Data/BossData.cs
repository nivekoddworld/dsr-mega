namespace DS1Mod.Core.Data;

internal static class BossData
{
    public static readonly IReadOnlyDictionary<int, string> All =
        new Dictionary<int, string>
        {
            // ── Undead Asylum ─────────────────────────────────────────
            { 11010000, "Asylum Demon" },

            // ── Undead Parish ─────────────────────────────────────────
            { 11010100, "Bell Gargoyles" },

            // ── The Depths ────────────────────────────────────────────
            { 11010200, "Gaping Dragon" },

            // ── Undead Burg ───────────────────────────────────────────
            { 11010400, "Taurus Demon" },
            { 11010500, "Capra Demon" },

            // ── Blighttown ────────────────────────────────────────────
            { 11010600, "Chaos Witch Quelaag" },

            // ── Painted World ─────────────────────────────────────────
            { 11510100, "Crossbreed Priscilla" },

            // ── Darkroot Garden ───────────────────────────────────────
            { 11210000, "Sif the Great Wolf" },
            { 11210100, "Moonlight Butterfly" },

            // ── Demon Ruins ───────────────────────────────────────────
            { 11410100, "Ceaseless Discharge" },
            { 11410200, "Demon Firesage" },
            { 11410300, "Centipede Demon" },

            // ── Tomb of the Giants ────────────────────────────────────
            { 11310000, "Pinwheel" },
            { 11310100, "Gravelord Nito" },

            // ── New Londo ─────────────────────────────────────────────
            { 11600000, "Four Kings" },

            // ── Sen's Fortress ────────────────────────────────────────
            { 11410000, "Iron Golem" },

            // ── Anor Londo ────────────────────────────────────────────
            { 11500000, "Ornstein and Smough" },
            { 11500100, "Dark Sun Gwyndolin" },

            // ── Duke's Archives ───────────────────────────────────────
            { 11700000, "Seath the Scaleless" },

            // ── Kiln of the First Flame ───────────────────────────────
            { 11800000, "Gwyn, Lord of Cinder" },

            // ── DLC: Oolacile ─────────────────────────────────────────
            { 12000000, "Sanctuary Guardian" },
            { 12000100, "Knight Artorias" },
            { 12000200, "Manus, Father of the Abyss" },
            { 12000300, "Black Dragon Kalameet" },
        };
}
