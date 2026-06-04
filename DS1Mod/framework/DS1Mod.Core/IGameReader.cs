namespace DS1Mod.Core;

public interface IGameReader
{
    bool          GetEventFlag  (int flagId);
    PlayerState?  GetPlayerState();
    PlayerStats?  GetPlayerStats();
    int           GetSoulLevel  ();
    int           GetSouls      ();

    /// <summary>The player's current animation id (0 if not loaded).</summary>
    int           GetCurrentAnimation();
}
