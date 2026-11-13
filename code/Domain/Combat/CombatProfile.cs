namespace GeXingzhou.Domain;

public sealed record CombatProgress
{
    public int SchemaVersion { get; init; }
    public string ContentVersion { get; init; } = "";
    public int Experience { get; init; }
    public List<string> Owned { get; init; } = new();
    public Dictionary<string, string> Equipment { get; init; } = new();
    public List<string> Completed { get; init; } = new();
    public string LastWave { get; init; } = "";
    public bool CompletedRun { get; init; }

    public static CombatProgress New(CombatCatalog c) => new()
    {
        SchemaVersion = 1, ContentVersion = c.Version, Owned = new() { "wraps", "jacket", "trainers" },
        Equipment = new() { ["weapon"] = "wraps", ["coat"] = "jacket", ["shoes"] = "trainers" }, LastWave = c.Waves[0].Id
    };

    public bool CanEnter(string id, CombatCatalog c) => c.Waves.FindIndex(w => w.Id == id) is var index && index >= 0 && index <= Completed.Count;

    public CombatProgress Reward(string id, CombatCatalog c)
    {
        if (!CanEnter(id, c)) throw new InvalidOperationException("该波尚未解锁。");
        if (Completed.Contains(id)) return this;
        var wave = c.Wave(id); var completed = Completed.Append(id).ToList();
        return this with
        {
            Experience = Math.Min(480, Experience + wave.Experience), Completed = completed,
            Owned = Owned.Concat(wave.Items).Distinct(StringComparer.Ordinal).ToList(),
            LastWave = c.Waves[Math.Min(completed.Count, c.Waves.Count - 1)].Id, CompletedRun = completed.Count == c.Waves.Count
        };
    }

    public CombatProgress Equip(string id, CombatCatalog c)
    {
        var item = c.Item(id);
        if (!Owned.Contains(id)) throw new InvalidOperationException("尚未获得该装备。");
        var equipment = new Dictionary<string, string>(Equipment) { [item.Slot] = id };
        return this with { Equipment = equipment };
    }

    public string? Validate(CombatCatalog c)
    {
        if (SchemaVersion != 1 || ContentVersion != c.Version) return "战斗存档版本不兼容。";
        if (Owned == null || Equipment == null || Completed == null || Completed.Count > c.Waves.Count ||
            !Completed.SequenceEqual(c.Waves.Take(Completed.Count).Select(w => w.Id)) ||
            Experience != c.Waves.Take(Completed.Count).Sum(w => w.Experience) || CompletedRun != (Completed.Count == c.Waves.Count))
            return "战斗成长记录异常。";
        var expected = new[] { "wraps", "jacket", "trainers" }.Concat(c.Waves.Take(Completed.Count).SelectMany(w => w.Items)).ToHashSet(StringComparer.Ordinal);
        if (Owned.Count != expected.Count || !expected.SetEquals(Owned) || Equipment.Count != 3 ||
            new[] { "weapon", "coat", "shoes" }.Any(slot => !Equipment.TryGetValue(slot, out var id) || !expected.Contains(id) || c.Item(id).Slot != slot) ||
            !CanEnter(LastWave, c)) return "战斗装备或波次记录异常。";
        return null;
    }
}
