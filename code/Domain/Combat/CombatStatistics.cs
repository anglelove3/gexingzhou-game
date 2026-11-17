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
    public int TotalFrames { get; internal set; }
    public int EffectiveDamage { get; internal set; }
    public int StaminaReturns { get; internal set; }
    public double StaminaRestored { get; internal set; }
    public int PerfectGuards { get; internal set; }
    public int RiposteAttacks { get; internal set; }
    public int RiposteHits { get; internal set; }
    public int BreakAttacks { get; internal set; }
    public int BreakHits { get; internal set; }
    public double ActiveSeconds => ActiveFrames / 60d;
    public double TotalSeconds => TotalFrames / 60d;
}

public sealed record CombatEvent(string Kind, string SourceId, string TargetId, int Damage, double X, double Y);
public enum CombatResult { Running, Won, Lost }
