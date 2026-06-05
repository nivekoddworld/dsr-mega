# src

All DS1 Mega Randomizer C# source projects. See the root `README.md` for
architecture, dependency order, and how to edit each subsystem.

| Project | Purpose |
|---|---|
| **DS1MegaRando.Settings** | User-configurable options |
| **DS1MegaRando.Annotations** | World metadata loaded from YAML |
| **DS1MegaRando.Graph** | Directed world graph and reachability analysis |
| **DS1MegaRando.IO** | Reads/writes DSR game files via SoulsFormats |
| **DS1MegaRando.Data** | Static game data — embedded YAML/XML/ESD resources |
| **DS1MegaRando.FogGate** | Fog gate randomization |
| **DS1MegaRando.Items** | Item placement and shop randomization |
| **DS1MegaRando.Enemies** | Enemy and boss placement, EMEVD patching |
| **DS1MegaRando.Verification** | Softlock checker |
| **DS1MegaRando.Spoiler** | Spoiler log generation |
| **DS1MegaRando.Core** | Orchestrator — coordinates all modules |
| **DS1MegaRando.UI** | WPF frontend |
| **DS1MegaRando.Test** | 500-seed regression test harness |
| **DS1MegaRando.RuntimeTest** | ESD runtime test harness |

Third-party libraries are in [`../lib/`](../lib/).
