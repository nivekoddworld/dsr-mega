namespace DS1Mod.Core.Data;

/// <summary>
/// DS1 boss-defeat event flags. The main bosses use low global flags (2–16);
/// secondary/optional bosses use 11xxxxxx flags. Values from the soulsmodding
/// wiki (DS1 boss flags reference) and cross-checked against the bundled enemy
/// randomizer eventscripts (e.g. Pinwheel=6, Iron Golem=11, Seath=14,
/// Demon Firesage=11410410 all match the scripts' set_event_flag calls).
/// </summary>
internal static class BossData
{
    public static readonly IReadOnlyDictionary<int, string> All =
        new Dictionary<int, string>
        {
            // ── Main bosses (low global flags) ────────────────────────
            { 2,  "Gaping Dragon"        },
            { 3,  "Bell Gargoyles"       },
            { 4,  "Crossbreed Priscilla" },
            { 5,  "Sif, the Great Grey Wolf" },
            { 6,  "Pinwheel"             },
            { 7,  "Gravelord Nito"       },
            { 9,  "Chaos Witch Quelaag"  },
            { 10, "Bed of Chaos"         },
            { 11, "Iron Golem"           },
            { 12, "Ornstein & Smough"    },
            { 13, "Four Kings"           },
            { 14, "Seath the Scaleless"  },
            { 15, "Gwyn, Lord of Cinder" },
            { 16, "Asylum Demon"         },

            // ── Secondary / optional bosses (11xxxxxx flags) ──────────
            { 11810900, "Stray Demon"          },
            { 11010901, "Taurus Demon"         },
            { 11010902, "Capra Demon"          },
            { 11200900, "Moonlight Butterfly"  },
            { 11410900, "Ceaseless Discharge"  },
            { 11410410, "Demon Firesage"       },
            { 11410901, "Centipede Demon"      },
            { 11510900, "Dark Sun Gwyndolin"   },

            // ── DLC: Oolacile ─────────────────────────────────────────
            { 11210000, "Sanctuary Guardian"        },
            { 11210001, "Knight Artorias"           },
            { 11210002, "Manus, Father of the Abyss"},
            { 11210005, "Black Dragon Kalameet"     },
        };
}
