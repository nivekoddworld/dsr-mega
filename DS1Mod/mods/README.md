# DS1Mod — Mods

Bundled mods that ship with or alongside the randomizer. Build with
`DS1Mod.Mods.slnx` (from the `DS1Mod/` directory).

| Mod | Purpose |
|---|---|
| **DS1Mod.DemoMod** | SDK exercise — template covering every API surface |
| **DS1Mod.FogLogger** | Logs every fog wall crossed (animation-based detection) |
| **DS1Mod.HpLogger** | Polls and logs HP changes each tick |
| **DS1Mod.DiscordRPC** | Discord Rich Presence — activity, deaths, last boss, session time |
| **DS1Mod.AsylumSlam** | Asylum Demon slam-only AI (patches luabnd at launch) |
| **DS1Mod.GoofyDemon** | Asylum Demon 10-mood gag mod with on-screen HUD, fart entrance, and trinket drop (v1.3) |
| **DS1Mod.ImGuiDemo** | Minimal ImGui overlay — stats panel + debug window, demonstrating `IGuiMod` |

Each mod targets `net8.0` and references `../../framework/DS1Mod.SDK`. The SDK
pulls in `DS1Mod.Core` transitively — do not reference Core directly unless you
need its types (e.g. `IGuiMod`, `DS1ImGui`).

Mods that patch game files at launch also reference `DS1Mod.Modding` for the
idempotent SoulsFormats helper API.

> `DS1Mod.AsylumSlam` and `DS1Mod.GoofyDemon` both patch the same AI script —
> do not load both at once. GoofyDemon supersedes AsylumSlam.
