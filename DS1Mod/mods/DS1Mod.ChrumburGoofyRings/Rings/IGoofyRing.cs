using DS1Mod.Core;
using DS1Mod.Modding;
using DS1Mod.SDK;

namespace DS1Mod.ChrumburGoofyRings.Rings;

/// <summary>
/// One ring in the Chrumbur Goofy Rings collection. Each ring lives in its
/// own file under Rings/ and owns its full vertical slice: config options,
/// game-file patches (param row, text, pickup), and runtime behavior.
/// The mod entrypoint just iterates the registered rings.
/// </summary>
internal interface IGoofyRing
{
    /// <summary>Display name, used in logs.</summary>
    string Name { get; }

    /// <summary>
    /// EquipParamAccessory row id, assigned during <see cref="Patch"/>.
    /// The mod uses it for the shared equipped-ring check.
    /// </summary>
    int AccessoryId { get; }

    /// <summary>Add this ring's options to the mod-wide config schema.</summary>
    void InitializeConfig(ModConfig config);

    /// <summary>
    /// Define the ring in the game files: accessory row, FMG text, item lot,
    /// world pickup. <paramref name="paramdefs"/> is the shared paramdef BND.
    /// </summary>
    void Patch(IPatchContext ctx, GamePatch g, byte[] paramdefs);

    /// <summary>Runtime startup — subscribe hooks, start threads.</summary>
    void OnLoad(IModContext ctx, ModConfig config);

    /// <summary>Runtime shutdown — unwind anything OnLoad started.</summary>
    void OnUnload();

    /// <summary>
    /// EventPump tick (~500 ms). <paramref name="equipped"/> is the shared
    /// equip check for <see cref="AccessoryId"/> (always true when the
    /// requireRingEquipped config is off, or while the inventory signature
    /// is still unresolved... false in that case).
    /// </summary>
    void OnTick(bool equipped);
}
