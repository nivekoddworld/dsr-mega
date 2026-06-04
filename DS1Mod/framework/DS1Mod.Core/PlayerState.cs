namespace DS1Mod.Core;

/// <summary>
/// Player world state. <see cref="MapId"/> is currently always empty: DSR has
/// no simple, verified pointer for the live map ID (even DSR-Gadget doesn't
/// expose one), so rather than report a fabricated/guessed value it is left
/// blank until a reliable source is found. X/Y/Z are live and accurate.
/// </summary>
public sealed record PlayerState(
    float X, float Y, float Z,
    string MapId);
