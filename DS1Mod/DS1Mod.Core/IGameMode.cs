namespace DS1Mod.Core;

public interface IGameMode
{
    string Name { get; }

    void OnAttached(GameSession session);
    void OnDetached();

    void OnBossKilled(BossKill kill);
    void OnFogGateOpened(FogGate gate);
    void OnPlayerMoved(float x, float y, float z, string mapId);
}
