namespace GeXingzhou.Domain;

public sealed class CombatStatistics
{
    public int ActiveFrames { get; internal set; }
    public int LightAttacks { get; internal set; }
    public int HeavyAttacks { get; internal set; }
    public int Dodges { get; internal set; }
    public int Blocks { get; internal set; }
    public int HitsTaken { get; internal set; }
    public int DamageDealt { get; internal set; }
    public int DamageTaken { get; internal set; }
    public int EmptyStamina { get; internal set; }
    public double ActiveSeconds => ActiveFrames / 60d;
}

public sealed record CombatEvent(string Kind, string SourceId, string TargetId, int Damage, double X, double Y);
public enum CombatResult { Running, Won, Lost }
