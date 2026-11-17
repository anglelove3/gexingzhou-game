namespace GeXingzhou.Domain;

public sealed class CombatStatistics
{
    [System.Text.Json.Serialization.JsonInclude] public int ActiveFrames { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int LightAttacks { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int HeavyAttacks { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int Dodges { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int Blocks { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int HitsTaken { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int DamageDealt { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int DamageTaken { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int EmptyStamina { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int TotalFrames { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int EffectiveDamage { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int StaminaReturns { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public double StaminaRestored { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int PerfectGuards { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int RiposteAttacks { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int RiposteHits { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int BreakAttacks { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int BreakHits { get; internal set; }
    public CombatStatistics Copy() => (CombatStatistics)MemberwiseClone();
    public bool IsValid() => new[] { ActiveFrames, TotalFrames, LightAttacks, HeavyAttacks, Dodges, Blocks, HitsTaken,
        DamageDealt, DamageTaken, EmptyStamina, EffectiveDamage, StaminaReturns, PerfectGuards, RiposteAttacks,
        RiposteHits, BreakAttacks, BreakHits }.All(v => v >= 0) && TotalFrames >= ActiveFrames &&
        double.IsFinite(StaminaRestored) && StaminaRestored >= 0 && EffectiveDamage <= DamageDealt;
    public double ActiveSeconds => ActiveFrames / 60d;
    public double TotalSeconds => TotalFrames / 60d;
}

public sealed record CombatEvent(string Kind, string SourceId, string TargetId, int Damage, double X, double Y);
public enum CombatResult { Running, Won, Lost }
