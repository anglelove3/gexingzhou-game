namespace GeXingzhou.Domain;

public sealed class CombatStatistics
{
    [System.Text.Json.Serialization.JsonInclude] public int ActiveFrames { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int LightAttacks { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int HeavyAttacks { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int Dodges { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int DodgeCancels { get; internal set; }
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
    [System.Text.Json.Serialization.JsonInclude] public int CriticalHits { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int AreaCasts { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude] public int AreaHits { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? AreaDoubleHits { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? AreaDamage { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public AreaMoveStatistics? GroundStomp { get; internal set; }
    [System.Text.Json.Serialization.JsonInclude]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public AreaMoveStatistics? OpeningPalms { get; internal set; }

    internal void RecordAdditionalArea(string moveId, int casts, int hits, int damage)
    {
        if (moveId == CombatMove.GroundStompId && GroundStomp is { } stomp)
            GroundStomp = stomp.Add(casts, hits, damage);
        if (moveId == CombatMove.OpeningPalmsId && OpeningPalms is { } palms)
            OpeningPalms = palms.Add(casts, hits, damage);
    }

    public CombatStatistics Copy() => (CombatStatistics)MemberwiseClone();
    public bool IsValid() => new[] { ActiveFrames, TotalFrames, LightAttacks, HeavyAttacks, Dodges, Blocks, HitsTaken,
        DamageDealt, DamageTaken, EmptyStamina, EffectiveDamage, StaminaReturns, PerfectGuards, RiposteAttacks,
        RiposteHits, BreakAttacks, BreakHits, DodgeCancels, CriticalHits, AreaCasts, AreaHits }.All(v => v >= 0) &&
        DodgeCancels <= Dodges && TotalFrames >= ActiveFrames &&
        double.IsFinite(StaminaRestored) && StaminaRestored >= 0 && EffectiveDamage <= DamageDealt &&
        ((AreaDoubleHits == null && AreaDamage == null) ||
         (AreaDoubleHits is int doubleHits && AreaDamage is int areaDamage &&
          doubleHits >= 0 && doubleHits <= AreaCasts && doubleHits <= AreaHits / 2 &&
          areaDamage >= 0 && areaDamage <= EffectiveDamage)) &&
        (GroundStomp.HasValue == OpeningPalms.HasValue) &&
        (GroundStomp?.IsValid() ?? true) && (OpeningPalms?.IsValid() ?? true) &&
        (long)(AreaDamage ?? 0) + (GroundStomp?.Damage ?? 0) + (OpeningPalms?.Damage ?? 0) <= EffectiveDamage;
    public double ActiveSeconds => ActiveFrames / 60d;
    public double TotalSeconds => TotalFrames / 60d;
}

public readonly record struct AreaMoveStatistics(int Casts, int Hits, int Damage)
{
    internal AreaMoveStatistics Add(int casts, int hits, int damage) => new(Casts + casts, Hits + hits, Damage + damage);
    internal bool IsValid() => Casts >= 0 && Hits >= 0 && Hits <= 3L * Casts && Damage >= 0 && (Casts > 0 || Damage == 0);
}

public sealed record CombatEvent(string Kind, string SourceId, string TargetId, int Damage, double X, double Y,
    string MoveId = "", int Facing = 1, double Displacement = 0, bool Critical = false);
public enum CombatResult { Running, Won, Lost }
