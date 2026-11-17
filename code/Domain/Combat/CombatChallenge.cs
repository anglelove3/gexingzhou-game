using System.Reflection;
using System.Text.Json;

namespace GeXingzhou.Domain;

public enum ChallengeOutcome { Running, Won, Lost, Abandoned }

public sealed record CombatChallengeWave(string WaveId, ChallengeOutcome Outcome, int HealthStart, int HealthEnd,
    double StaminaStart, CombatStatistics Statistics);

public sealed record CombatChallengeRecord
{
    public int SchemaVersion { get; init; } = 1;
    public string AttemptId { get; init; } = "";
    public string ModeId { get; init; } = "street-survival-1";
    public string BalanceVersion { get; init; } = "";
    public string AssemblyVersion { get; init; } = "";
    public string ConfigHash { get; init; } = "";
    public string ConfigJson { get; init; } = "";
    public CombatProgress Progress { get; init; } = new();
    public CombatStats? Stats { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public ChallengeOutcome Outcome { get; init; }
    public int Revision { get; init; }
    public List<CombatChallengeWave> Waves { get; init; } = new();
    public int CompletedWaves => Waves.Count(w => w.Outcome == ChallengeOutcome.Won);
    public double TotalSeconds => Waves.Sum(w => w.Statistics.TotalSeconds);

    public CombatChallengeRecord Copy() => JsonSerializer.Deserialize<CombatChallengeRecord>(JsonSerializer.Serialize(this))!;

    public string? Validate()
    {
        if (SchemaVersion != 1 || ModeId != "street-survival-1" || !Guid.TryParseExact(AttemptId, "N", out _) ||
            string.IsNullOrWhiteSpace(AssemblyVersion) || ConfigJson == null || ConfigJson.Length > 131072 ||
            Progress == null || Stats == null || Waves == null || Waves.Count > 6 || StartedAt == default || !Enum.IsDefined(Outcome))
            return "挑战记录结构异常。";
        try
        {
            var c = CombatCatalog.Parse(ConfigJson);
            if (c.ConfigHash != ConfigHash || c.BalanceVersion != BalanceVersion || Progress.Validate(c) != null ||
                !Progress.CompletedRun || c.Stats(Progress) != Stats) return "挑战快照校验失败。";
            int health = Stats.Health;
            for (int i = 0; i < Waves.Count; i++)
            {
                var w = Waves[i];
                if (w == null || w.WaveId != c.Waves[i].Id || w.HealthStart != health || w.HealthEnd < 0 || w.HealthEnd > health ||
                    w.StaminaStart != Stats.Stamina || w.Statistics == null || !w.Statistics.IsValid() ||
                    w.Outcome is not (ChallengeOutcome.Won or ChallengeOutcome.Lost or ChallengeOutcome.Abandoned) ||
                    w.Outcome == ChallengeOutcome.Won && w.HealthEnd == 0 || w.Outcome == ChallengeOutcome.Lost && w.HealthEnd != 0 ||
                    i < Waves.Count - 1 && w.Outcome != ChallengeOutcome.Won) return "挑战波次记录异常。";
                health = w.HealthEnd;
            }
            bool valid = Outcome switch
            {
                ChallengeOutcome.Running => CompletedWaves == Waves.Count && Waves.Count < 6 && Revision == Waves.Count,
                ChallengeOutcome.Won => CompletedWaves == 6 && Revision == 6,
                ChallengeOutcome.Lost => Waves.Count > 0 && Waves[^1].Outcome == ChallengeOutcome.Lost && Revision == Waves.Count,
                ChallengeOutcome.Abandoned => CompletedWaves < 6 &&
                    (CompletedWaves == Waves.Count && Revision == Waves.Count + 1 ||
                     Waves.Count > 0 && Waves[^1].Outcome == ChallengeOutcome.Abandoned && Revision == Waves.Count),
                _ => false
            };
            return valid ? null : "挑战结果与完成记录不一致。";
        }
        catch (ArgumentException) { return "挑战配置校验失败。"; }
    }
}

public sealed class CombatChallenge
{
    private readonly CombatCatalog catalog;
    private CombatChallengeRecord record;
    private CombatMatch? current;
    private bool awaitingContinue;
    public CombatChallengeRecord Record => record.Copy();
    public int CompletedWaves => record.CompletedWaves;

    private CombatChallenge(CombatCatalog catalog, CombatChallengeRecord record)
    { this.catalog = catalog; this.record = record; }

    public static CombatChallenge Start(CombatCatalog catalog, CombatProgress progress)
    {
        if (progress.Validate(catalog) != null || !progress.CompletedRun) throw new InvalidOperationException("完成普通六波后解锁街头连战。");
        var frozen = JsonSerializer.Deserialize<CombatProgress>(JsonSerializer.Serialize(progress))!;
        var copy = CombatCatalog.Parse(catalog.SourceJson);
        var record = new CombatChallengeRecord
        {
            AttemptId = Guid.NewGuid().ToString("N"), BalanceVersion = catalog.BalanceVersion,
            AssemblyVersion = typeof(CombatChallenge).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown",
            ConfigHash = catalog.ConfigHash, ConfigJson = catalog.SourceJson, Progress = frozen,
            Stats = copy.Stats(frozen), StartedAt = DateTimeOffset.UtcNow
        };
        return new(copy, record);
    }

    public CombatMatch CreateMatch()
    {
        if (record.Outcome != ChallengeOutcome.Running || current != null || awaitingContinue)
            throw new InvalidOperationException("请先完成当前波结算。");
        int health = record.Waves.Count == 0 ? record.Stats!.Health : record.Waves[^1].HealthEnd;
        current = new(catalog, record.Progress, catalog.Waves[record.CompletedWaves].Id, health);
        return current;
    }

    public void FinishWave(CombatMatch match)
    {
        if (match != current || match.Result == CombatResult.Running) throw new InvalidOperationException("当前波尚未结束。");
        var outcome = match.Result == CombatResult.Won ? ChallengeOutcome.Won : ChallengeOutcome.Lost;
        AppendWave(outcome);
        if (outcome == ChallengeOutcome.Lost) record = record with { Outcome = ChallengeOutcome.Lost };
        else if (record.CompletedWaves == catalog.Waves.Count) record = record with { Outcome = ChallengeOutcome.Won };
        else awaitingContinue = true;
    }

    public void Continue()
    {
        if (!awaitingContinue || record.Outcome != ChallengeOutcome.Running) throw new InvalidOperationException("当前挑战无法继续。");
        awaitingContinue = false;
    }

    public void End()
    {
        if (record.Outcome != ChallengeOutcome.Running) return;
        if (current != null) AppendWave(ChallengeOutcome.Abandoned);
        else record = record with { Revision = record.Revision + 1 };
        record = record with { Outcome = ChallengeOutcome.Abandoned };
        awaitingContinue = false;
    }

    private void AppendWave(ChallengeOutcome outcome)
    {
        var match = current!;
        int start = record.Waves.Count == 0 ? record.Stats!.Health : record.Waves[^1].HealthEnd;
        var wave = new CombatChallengeWave(match.Wave.Id, outcome, start, match.Player.Health, match.Player.Stats.Stamina, match.Statistics.Copy());
        record = record with { Waves = record.Waves.Append(wave).ToList(), Revision = record.Revision + 1 };
        current = null;
    }
}
