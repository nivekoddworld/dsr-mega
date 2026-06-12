namespace DS1Mod.Core;

/// <summary>
/// Fired by <see cref="IGameHooks.EnemyDamaged"/> when any loaded non-player
/// character loses HP. One event per poll cycle per character — multiple hits
/// inside one 500 ms window coalesce into a single event with the summed damage.
/// </summary>
/// <param name="Character">Address of the ChrIns that was damaged.</param>
/// <param name="Damage">HP lost since the previous poll.</param>
/// <param name="CurrentHp">The character's HP after the damage.</param>
/// <param name="MaxHp">The character's max HP.</param>
/// <param name="DistanceToPlayer">
/// Straight-line distance from the player in meters, or -1 when either
/// position could not be read.
/// </param>
public sealed record EnemyDamage(
    nint  Character,
    int   Damage,
    int   CurrentHp,
    int   MaxHp,
    float DistanceToPlayer);
