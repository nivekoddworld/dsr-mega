# DS1MegaRando.Data

Static game data embedded as resources in the randomizer. No runtime I/O — all
files are compiled into the DLL and accessed via `Assembly.GetManifestResourceStream`.

## Folders

| Folder | Format | Contents |
|---|---|---|
| `Annotations/` | YAML | `ds1-fog.yaml` — area graph, entrances, key item requirements used by FogGate and Graph modules |
| `Enemies/` | C# | `BossIds.cs` — all 32 boss slot definitions (EntityID, ModelID, EMEVD patches, CanReplace flags) |
| `Enemies/` | C# | `EnemyIds.cs` — full enemy model catalogue with IsIgnored flags |
| `Items/` | C# | Item ID constants and pool definitions |
| `Areas/` | C# | Area metadata (map IDs, names, connections) |
| `Params/` | XML | PARAM layout definitions (paramdef) consumed by SoulsFormats to make PARAM cells writable by name |
| `ESDs/` | ESD binary | Replacement bonfire ESD that always offers Level Up, regardless of Firekeeper state |

## Ground truth

`BossIds.cs` EntityIDs must match `FogMod-master/dist/fog.txt` — see the
`@ENTITYID` field on each entry. A mismatch means the boss slot is silently
skipped during randomization.
