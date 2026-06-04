namespace DS1Mod.Core;

public sealed record PlayerStats(
    int   CurrentHp,
    int   MaxHp,
    float CurrentStamina,
    float MaxStamina)
{
    public float HpFraction      => MaxHp      > 0 ? (float)CurrentHp / MaxHp     : 0f;
    public float StaminaFraction => MaxStamina > 0 ? CurrentStamina   / MaxStamina : 0f;
}
