# DS1Mod.DiscordRPC

Displays your Dark Souls Remastered session in Discord Rich Presence.

## What it shows

- **Activity** — "At the bonfire", "Exploring", "In a boss fight" (updates on fog gate and boss kill events)
- **Deaths** — running count for the session
- **Last boss killed** — name of the most recent boss defeated
- **Session time** — elapsed time since the mod loaded

## Setup

This mod requires a **Discord Application** with two art assets registered
under Rich Presence → Art Assets:

| Key | Suggested image |
|---|---|
| `ds1_bonfire` | Any DS1 artwork (used as the large image) |
| `ds1_skull` | A skull icon (used as the small image) |

The application ID is hardcoded to `1511993705731719268` in `DiscordRpcMod.cs`.
If you fork the mod, create your own application in the
[Discord Developer Portal](https://discord.com/developers/applications) and
update the constant.

## Install

Copy both DLLs into `<game>/mods/`:

```
DS1Mod.DiscordRPC.dll
DiscordRPC.dll        (the discord-rpc-csharp NuGet dependency)
```

`DS1Mod.Core.dll` and `DS1Mod.SDK.dll` are provided by the host.

## Revert

Delete the DLLs from `mods/`. The mod writes no game files.
