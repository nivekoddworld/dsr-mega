namespace DS1Mod.Core;

public interface IGameReader
{
    bool          GetEventFlag  (int flagId);
    PlayerState?  GetPlayerState();
    PlayerStats?  GetPlayerStats();
    int           GetSoulLevel  ();
    int           GetSouls      ();
}
