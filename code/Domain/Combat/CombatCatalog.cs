using System.Text.Json;

namespace GeXingzhou.Domain;

public sealed record CombatItem
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Slot { get; init; } = "";
    public string Description { get; init; } = "";
    public int Attack { get; init; }
    public int Defense { get; init; }
    public int Health { get; init; }
    public int Stamina { get; init; }
    public double Recovery { get; init; }
    public double Move { get; init; } = 1;
    public double LightTime { get; init; } = 1;
    public double AttackTime { get; init; } = 1;
    public double LightCost { get; init; } = 1;
    public double AttackCost { get; init; } = 1;
    public double Reach { get; init; } = 1;
    public double HeavyPower { get; init; } = 1;
    public double DodgeAdjustment { get; init; }
}

public sealed record CombatMove
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public double Power { get; init; }
    public int Startup { get; init; }
    public int Active { get; init; }
    public int Recovery { get; init; }
    public int Cost { get; init; }
    public double Reach { get; init; }
    public double Depth { get; init; }
    public double Stun { get; init; }
    public double Push { get; init; }
    public bool IsLight => Id.StartsWith("light", StringComparison.Ordinal);
    public int Total => Startup + Active + Recovery;
}

public sealed record CombatEnemy
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public int Health { get; init; }
    public int Attack { get; init; }
    public int Defense { get; init; }
    public double Speed { get; init; }
    public double Warning { get; init; }
    public double Interval { get; init; }
}

public sealed record CombatWave
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Hint { get; init; } = "";
    public string[] Enemies { get; init; } = Array.Empty<string>();
    public int Experience { get; init; }
    public string[] Items { get; init; } = Array.Empty<string>();
}

public sealed record CombatStats(int Health, int Attack, int Defense, int Stamina,
    double Recovery, double MoveSpeed, double DodgeSeconds, CombatItem Weapon);

public sealed class CombatCatalog
{
    public string Version { get; init; } = "";
    public int[] Levels { get; init; } = Array.Empty<int>();
    public List<CombatItem> Items { get; init; } = new();
    public List<CombatMove> Moves { get; init; } = new();
    public List<CombatEnemy> Enemies { get; init; } = new();
    public List<CombatWave> Waves { get; init; } = new();

    public static CombatCatalog Parse(string json)
    {
        try
        {
            var c = JsonSerializer.Deserialize<CombatCatalog>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (c == null || c.Version != "combat-0.1" || c.Levels == null || !c.Levels.SequenceEqual(new[] { 0, 60, 160, 300, 480 }) ||
                c.Items == null || c.Moves == null || c.Enemies == null || c.Waves == null ||
                c.Items.Count != 9 || c.Moves.Count != 5 || c.Enemies.Count != 4 || c.Waves.Count != 6)
                throw new ArgumentException("战斗配置结构或版本异常。");
            Unique(c.Items.Select(i => i.Id)); Unique(c.Moves.Select(i => i.Id));
            Unique(c.Enemies.Select(i => i.Id)); Unique(c.Waves.Select(i => i.Id));
            foreach (var i in c.Items)
            {
                var values = new[] { i.Move, i.LightTime, i.AttackTime, i.LightCost, i.AttackCost, i.Reach, i.HeavyPower };
                if (!new[] { "weapon", "coat", "shoes" }.Contains(i.Slot) || string.IsNullOrWhiteSpace(i.Name) ||
                    values.Any(v => !double.IsFinite(v) || v <= 0 || v > 4) ||
                    !double.IsFinite(i.Recovery) || !double.IsFinite(i.DodgeAdjustment) || .34 + i.DodgeAdjustment < .18)
                    throw new ArgumentException("装备参数异常。");
            }
            foreach (var m in c.Moves)
                if (m.Startup < 1 || m.Active < 1 || m.Recovery < 1 || m.Cost < 1 ||
                    new[] { m.Power, m.Reach, m.Depth, m.Stun, m.Push }.Any(v => !double.IsFinite(v) || v <= 0))
                    throw new ArgumentException("招式参数异常。");
            foreach (var e in c.Enemies)
                if (e.Health <= 0 || e.Attack <= 0 || e.Defense < 0 ||
                    new[] { e.Speed, e.Warning, e.Interval }.Any(v => !double.IsFinite(v) || v <= 0))
                    throw new ArgumentException("对手参数异常。");
            foreach (var w in c.Waves)
                if (w.Experience <= 0 || w.Enemies == null || w.Items == null || w.Enemies.Length is < 1 or > 3 ||
                    w.Enemies.Any(id => !c.Enemies.Any(e => e.Id == id)) || w.Items.Any(id => !c.Items.Any(i => i.Id == id)))
                    throw new ArgumentException("波次配置异常。");
            if (c.Waves.Sum(w => w.Experience) != 480) throw new ArgumentException("经验预算异常。");
            foreach (var id in new[] { "wraps", "jacket", "trainers" }) c.Item(id);
            foreach (var id in new[] { "light1", "light2", "light3", "heavy", "charged" }) c.Move(id);
            return c;
        }
        catch (JsonException ex) { throw new ArgumentException("战斗配置无法解析。", ex); }
    }

    private static void Unique(IEnumerable<string> ids)
    {
        var values = ids.ToArray();
        if (values.Any(string.IsNullOrWhiteSpace) || values.Distinct(StringComparer.Ordinal).Count() != values.Length)
            throw new ArgumentException("战斗配置ID重复或缺失。");
    }

    public CombatItem Item(string id) => Items.FirstOrDefault(i => i.Id == id) ?? throw new ArgumentException("未知装备 " + id);
    public CombatMove Move(string id) => Moves.FirstOrDefault(i => i.Id == id) ?? throw new ArgumentException("未知招式 " + id);
    public CombatWave Wave(string id) => Waves.FirstOrDefault(i => i.Id == id) ?? throw new ArgumentException("未知波次 " + id);
    public int Level(int experience) => Math.Clamp(Levels.Count(x => experience >= x), 1, Levels.Length);
    public static int Damage(int attack, double power, int defense) => Math.Max(1, (int)Math.Floor(attack * power * 100 / (100 + defense) + .5));

    public CombatStats Stats(CombatProgress p)
    {
        var rank = Level(p.Experience) - 1;
        var items = p.Equipment.Values.Select(Item).ToArray();
        return new(100 + rank * 12 + items.Sum(i => i.Health), 14 + rank * 2 + items.Sum(i => i.Attack),
            8 + rank * 2 + items.Sum(i => i.Defense), 100 + rank * 5 + items.Sum(i => i.Stamina),
            24 + items.Sum(i => i.Recovery), 180 * items.Aggregate(1d, (v, i) => v * i.Move),
            .34 + items.Sum(i => i.DodgeAdjustment), Item(p.Equipment["weapon"]));
    }

    public CombatMove EquippedMove(string id, CombatStats stats)
    {
        var m = Move(id); var w = stats.Weapon;
        var time = w.AttackTime * (m.IsLight ? w.LightTime : 1);
        return m with
        {
            Startup = Math.Max(1, (int)Math.Round(m.Startup * time, MidpointRounding.AwayFromZero)),
            Active = Math.Max(1, (int)Math.Round(m.Active * time, MidpointRounding.AwayFromZero)),
            Recovery = Math.Max(1, (int)Math.Round(m.Recovery * time, MidpointRounding.AwayFromZero)),
            Cost = (int)Math.Ceiling(m.Cost * w.AttackCost * (m.IsLight ? w.LightCost : 1)),
            Power = m.Power * (m.IsLight ? 1 : w.HeavyPower), Reach = m.Reach * w.Reach
        };
    }
}
