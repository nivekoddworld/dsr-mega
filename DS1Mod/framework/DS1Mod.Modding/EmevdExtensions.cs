using DS1Mod.Core;
using SoulsFormats;

namespace DS1Mod.Modding;

/// <summary>
/// Extensions for <see cref="EmevdEditor"/> to support boss-specific EMEVD patching.
/// </summary>
public static class EmevdExtensions
{
    /// <summary>
    /// Remove all instructions matching the given (bank, id) pairs from an event.
    /// Used to strip model-specific instructions (ForceAnimationPlayback, WarpCharacter, etc.)
    /// when a boss gets a new model. Returns the number of instructions removed.
    /// </summary>
    public static int RemoveInstructions(
        this EmevdEditor editor,
        long eventId,
        params (int Bank, int InstrId)[] instructionsToRemove)
    {
        EMEVD.Event? evt = editor.Event(eventId);
        if (evt == null) return 0;

        int before = evt.Instructions.Count;
        evt.Instructions.RemoveAll(instr =>
            instructionsToRemove.Any(r => instr.Bank == r.Bank && instr.ID == r.InstrId));
        return before - evt.Instructions.Count;
    }

    /// <summary>
    /// Apply EMEVD patches for a boss: remove specified instructions from its intro events.
    /// Returns true if any instructions were removed.
    /// </summary>
    public static bool ApplyBossPatches(
        this EmevdEditor editor,
        IEnumerable<EmevdPatch> patches)
    {
        bool changed = false;
        foreach (var patch in patches)
        {
            int removed = editor.RemoveInstructions(patch.EventId, patch.Remove);
            if (removed > 0) changed = true;
        }
        return changed;
    }
}
