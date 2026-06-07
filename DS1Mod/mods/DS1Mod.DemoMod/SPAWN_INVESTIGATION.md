# DSR Runtime Enemy-Spawn Investigation

Status as of 2026-06-07. Branch: `claude/sharp-sagan-mTLFK`.

## Goal

Spawn arbitrary enemies (model id + position + AI params) into the live
DSR process at runtime, without pre-placing them in the MSB. The user's
existing approach (MSB-pre-place + EMEVD-trigger) works but cannot
position enemies dynamically around the player.

## Headline finding

**`NS_FRPG::ChrInsFactory` exists in the binary** as a proper MSVC RTTI
class, with a vftable at `0x141321060` and one virtual slot at
`0x14033b880`. The class name strongly implies a runtime entity
construction API.

**However, that one virtual slot turned out to be the destructor**, not
`create`. The actual `create()` is a non-virtual member function, fully
buried inside DSR's VMProtect overlay. We can prove the factory exists
but we cannot — from static analysis alone — locate or call its
construction methods.

## Evidence trail

### RTTI walk (validated end-to-end in-process)

| Step | Address | Notes |
|---|---|---|
| Mangled name | `0x141AFFFD0` | `.?AVChrInsFactory@NS_FRPG@@` — unique anchor |
| TypeDescriptor | `0x141AFFFC0` | 16 bytes before name |
| Complete Object Locator | `0x141649EB0` | found via 4-byte RVA reference |
| vftable | `0x141321060` | found via qword reference to COL |
| vftable[0] | `0x14033B880` | the only real function pointer slot |
| Entry bytes | `E9 D2 7D 91 02 …` | JMP rel32 trampoline into overlay |

This walk runs successfully inside DSR — addresses are bit-identical
between static analysis (capstone/pefile in `/tmp/dsr/`) and live in-game
ImGui panel readout.

### Why vftable[0] is the destructor

Disassembly of `FrpgEntityFactory::~FrpgEntityFactory` at `0x14015B4A0`
(the parent class's slot[0]) is the textbook MSVC scalar-deleting-dtor
signature:

```
push rbx; sub rsp, 0x20
lea  rax, [rip + vftable]      ; reload vftable address
mov  rbx, rcx                  ; save this
mov  [rcx], rax                ; *this = vftable  ← vptr restore
test dl, 1                     ; check delete flag (low bit of arg1)
mov  edx, 0x10                 ; sizeof(class) = 16
call operator_delete
ret
```

The same pattern (with different vftable + size) is at the start of
`ChrInsFactory::~ChrInsFactory` inside the obfuscated overlay.

**Confirmation from runtime behaviour**:
- Hooked vftable[0] with capture stub → counter stays at 0 across full
  gameplay session (fog gates, kills, bonfires, warps). Destructors only
  run at process shutdown, so this is consistent.
- Called vftable[0] with null `this` → crash. Dtor immediately writes
  `*rcx = vftable`, so null this dereferences immediately.

### Why static analysis hit a wall

VMProtect specifically protects every class-lifecycle code path:

| Target | LEA refs in unobfuscated `.text` (`0x140001000–0x14129C400`) | LEA refs in obfuscated overlay (`0x142019000+`) |
|---|---|---|
| `ChrInsFactory` vftable (`0x141321060`) | **0** | 3 |
| `ChrIns` instance vftable (`0x14131E008`) | **0** | 2 |

Every constructor, destructor, and (presumably) the factory's non-virtual
methods are inside the overlay. From this side we can find addresses; we
cannot follow the code that USES them.

### Other classes inspected

| Class | Virtual slots | Notes |
|---|---|---|
| `NS_FRPG::ChrIns` | **50+** | Real polymorphic class — useful if we ever obtain a `ChrIns*` |
| `NS_FRPG::ChrInsFactory` | 1 (dtor) | The factory itself |
| `NS_FRPG::ChrInsManThread` | (not walked) | Thread that ticks chr lifecycle |
| `NS_FRPG::FrpgEntityFactory` | 1 (dtor) | Base class |
| `NS_FRPG::WorldChrManImp` | 1 (dtor) | Manager — also only a virtual dtor |

The single-virtual-dtor pattern across managers and factories is From's
deliberate design. Almost all member functions in their engine are
non-virtual.

## What's been built

### Static analysis pipeline (`/tmp/dsr/`)
- `dsr_re.py` — section walker, disassembler, address resolver
- `enum_classes.py` — extracts every `.?AV...@@` RTTI class name
- `vftable.py`, `find_refs.py`, `wcm_vftable.py`, `explore_vftables.py` —
  RTTI walkers
- `aob.py` — AOB uniqueness checker

Headless Python equivalent of a Ghidra targeted query. Validated against
metal-crow's published addresses for the binary version we have.

### In-process probe (this commit)
- `ChrInsFactoryProbe.cs` — RTTI walk resolver. Scans mangled-name string,
  derives TypeDescriptor → COL → vftable → function pointer. Reports all
  intermediate addresses and a verification byte.
- `ChrInsFactoryHook.cs` — vftable hijack. Allocates RWX trampoline that
  captures `RCX`/`RDX`/`R8`/`R9` + four stack args into a 64-byte buffer,
  then tail-jumps to original. Install does one atomic 8-byte write into
  `.rdata` after `VirtualProtect(RW)`. Uninstall writes the original back
  (stub + buffer deliberately leaked to avoid race with in-flight calls).
- `DemoMod.cs` — adds `IGuiMod` surface with a `ChrInsFactory probe`
  window: Resolve button, hook install/uninstall, live capture display.
- `DS1ImGui.cs` — `Button` P/Invoke binding added (also fixed by another
  agent on `claude/serene-curie-wui5b` — `igButton` is now properly
  exported from `dinput8.dll`).

## What's blocked

To call `ChrInsFactory::create` we need three things, none of which we
have:

1. **The address of `create`** — it's a non-virtual method, inside
   VMProtect. Not findable by RTTI walk.
2. **The address of the singleton `g_chrInsFactory`** — needed for `this`.
   Its setter is also in VMProtect (the ctor → singleton store path).
3. **The parameter signature** — unrecoverable without either decompiling
   the body or capturing a real call.

All three are gated on getting eyes on `create()`'s body. That means
either:
- a tool that can follow VMProtect overlay control flow (Ghidra with
  RTTI + a willingness to manually trace), OR
- a live debugger setting a breakpoint inside the overlay during a known
  spawn event, OR
- an inline detour on a related unobfuscated function in the call chain
  (e.g. `ChrAsmModelRes_Load_PartsbndFileCap_Entry` at `0x14020A280`),
  hooked to log call-stack return addresses.

## Next steps

### Immediate (Ghidra-side, on user's machine)

1. **Enable Windows PE RTTI Analyzer + MSVC Demangler.** Re-run Auto
   Analyze. After this, vftables and class members will be labelled with
   their mangled names instead of `DAT_...` / `FUN_...`.

2. **Decompile `FUN_142C53657`** (the ctor). Its containing function is
   what allocates the factory. The caller one level up does
   `g_chrInsFactory = operator_new(0x10); ctor(g_chrInsFactory)` — that
   caller has the singleton storage address as a RIP-relative `mov`
   destination.

3. **Find `create`** by either:
   - searching for refs to `g_chrInsFactory` after step 2, then sorting
     by call frequency (create will be heavily used), OR
   - tracing upward from `ChrAsmModelRes_Load_PartsbndFileCap_Entry`
     at `0x14020A280` (which IS in unobfuscated `.text`), OR
   - searching for wide-string literals `c0000` / `c1201` etc. and
     following xrefs to the function that takes them as args.

Hand back: the address of `create` + first ~20 lines of Ghidra's
decompiled signature.

### Infrastructure to build (this side)

Once `create`'s address is known:

- **Inline detour primitive** in `DS1Mod.Core.Memory` —
  `InlineHook.Install(targetVA, stubVA)` that overwrites the first 5–14
  bytes with a JMP, allocates a trampoline that replays the displaced
  instructions, and returns a callable "call original" pointer. Need to
  handle RIP-relative operand relocation. This becomes the hammer for
  every non-virtual hook we want.

- **`SpawnHelper.cs`** — wraps the inline-detoured `create` call. Models
  the captured signature as `delegate* unmanaged<…>` and exposes
  `SpawnEnemy(string modelId, int npcParam, int thinkParam, Vector3 pos)`
  to mod authors.

### Plan B if `create` proves uncallable

If the parameter struct is opaque (e.g. an internal MSB-part-descriptor
that we can't fabricate cleanly), fall back to:

- **Snapshot-and-respawn**: copy an existing `ChrIns` struct, mutate its
  position/model/AI fields, splice into `WorldChrManImp`'s entity list.
  Risks documented in the earlier research report.

- **MSB-reserved slots**: in the MSB build pipeline, allocate N
  "scratch" entities per map that start disabled at sentinel coordinates.
  Mods enable + teleport them as needed. Doesn't solve "arbitrary model"
  but solves "arbitrary position around the player" for a fixed pool.

## File layout

```
DS1Mod/mods/DS1Mod.DemoMod/
  ChrInsFactoryProbe.cs       — RTTI walk resolver
  ChrInsFactoryHook.cs        — vftable hijack with native stub
  DemoMod.cs                  — IGuiMod panel
  SPAWN_INVESTIGATION.md      — this file
DS1Mod/framework/DS1Mod.Core/ImGui/
  DS1ImGui.cs                 — Button binding added
```

## Reproducing the static analysis

The Python pipeline that drove this investigation ran on a Linux
container with `pefile`, `capstone`, `lief`. Same scripts will work on
Windows with the same deps. The user's binary (50,286,344 bytes,
ImageBase `0x140000000`) matches metal-crow's overhaul reference build,
which means addresses in this doc are directly usable for any DSR
binary of the same Steam version.
