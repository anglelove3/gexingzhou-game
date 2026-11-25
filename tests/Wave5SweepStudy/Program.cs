using System.Text.Json;
using GeXingzhou.Domain;
using GeXingzhou.DomainChecks;

if (args.Length != 3) throw new ArgumentException("Expected the official configuration, candidate configuration, and output JSON path.");

var original = CombatCatalog.Parse(File.ReadAllText(args[0]));
var candidate = CombatCatalog.Parse(File.ReadAllText(args[1]));
var layouts = new[] { (Id: "center", Depth: 0d, Approach: 0), (Id: "upper", Depth: -1d, Approach: 140), (Id: "lower", Depth: 1d, Approach: 140) };
var rows = new List<Trial>();
foreach (var mode in new[] { "normal-lv3", "challenge-lv5-full" })
foreach (var weapon in new[] { "wraps", "gloves", "stick" })
foreach (var layout in layouts)
{
    rows.Add(Run(original, mode, weapon, layout, "current"));
    rows.Add(Run(candidate, mode, weapon, layout, "same-JK"));
    rows.Add(Run(candidate, mode, weapon, layout, "selective-Q"));
    rows.Add(Run(candidate, mode, weapon, layout, "Q-on-ready"));
}
if (rows.Chunk(4).Any(pair => pair[0] with { Arm = pair[1].Arm } != pair[1]))
    throw new InvalidOperationException("J/K control changed between configs.");
var routes = ScanRoutes(candidate);
var result = new { current = original.ConfigHash, candidate = candidate.ConfigHash, rows, routes };
File.WriteAllText(args[2], JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"WAVE5_MATRIX_PASS trials={rows.Count} matched_jk={rows.Count / 4} route_windows={routes.Count}");

static CombatMatch NewBout(CombatCatalog catalog, string mode, string weapon)
{
    var progress = CombatProgress.New(catalog);
    foreach (var wave in catalog.Waves.Take(mode == "normal-lv3" ? 4 : 6)) progress = progress.Reward(wave.Id, catalog);
    return new CombatMatch(catalog, progress.Equip(weapon, catalog), "c1", randomSeed: 1);
}
static bool CanDamage(CombatMatch m, CombatFighter e, CombatMove move) =>
    e.Health > 0 && !e.Protected && m.Player.InRange(e, move) &&
    !(e.State == CombatState.Guard && (m.Player.X - e.X) * e.Facing >= 0);

static bool AttackDue(CombatMatch m, CombatMove sweep) =>
    m.Enemies.Any(e => e.Attack is { } enemyMove && e.InRange(m.Player, enemyMove) &&
        (e.ActivePulse >= 0 || e.AttackFrame < enemyMove.Startup && enemyMove.Startup - e.AttackFrame <= sweep.Startup));

static Trial Run(CombatCatalog catalog, string mode, string weapon, (string Id, double Depth, int Approach) layout, string arm)
{
    var m = NewBout(catalog, mode, weapon);
    var move = catalog.Move(CombatMove.AreaSkillId);
    var hitsByCast = new Dictionary<int,int>();
    for (int frame = 0; frame < 7200 && m.Result == CombatResult.Running; frame++)
    {
        var input = frame < layout.Approach
            ? new CombatInput(MoveX: 1, MoveY: layout.Depth)
            : CombatAutomation.Decide(m);
        if (frame >= layout.Approach && (arm == "selective-Q" || arm == "Q-on-ready") &&
            m.Player.CanAct && m.Player.Stamina >= move.Cost && m.AreaCooldownRemaining == 0 &&
            (arm == "Q-on-ready" || m.Enemies.Count(e => CanDamage(m, e, move)) >= 2 && !AttackDue(m, move)))
            input = new CombatInput(AreaPressed: true);
        m.Step(input);
        int hits = m.Events.Count(e => e.SourceId == "player" && e.MoveId == CombatMove.AreaSkillId && e.Kind is "hit" or "down" or "break");
        if (hits > 0)
        {
            int sequence = m.Player.AttackSequence;
            hitsByCast[sequence] = hitsByCast.GetValueOrDefault(sequence) + hits;
        }
    }
    var s = m.Statistics;
    return new(arm, mode, weapon, layout.Id, m.Result.ToString(), s.TotalFrames, s.DamageTaken, s.EffectiveDamage,
        s.AreaCasts, s.AreaHits, s.AreaDoubleHits ?? 0, s.AreaDamage ?? 0, hitsByCast.Values.Count(h => h == 3), s.BreakHits);
}

static List<Window> ScanRoutes(CombatCatalog catalog)
{
    var windows = new List<Window>();
    foreach (int direction in new[] { -1, 0, 1 })
    for (int walk = 25; walk <= 160; walk += 5)
    {
        int previous = -1, start = 0;
        for (int wait = 0; wait <= 175; wait++)
        {
            int hits = 4;
            foreach (int lead in new[] { 2, 3, 4 })
            {
                var m = NewBout(catalog, "normal-lv3", "wraps");
                for (int i = 0; i < lead; i++) m.Step(default);
                for (int i = 0; i < walk; i++) m.Step(new CombatInput(MoveX: 1, MoveY: direction));
                for (int i = 0; i < wait; i++) m.Step(default);
                m.Step(new CombatInput(AreaPressed: true));
                for (int i = 0; i < 44; i++) m.Step(default);
                hits = Math.Min(hits, m.Statistics.AreaHits);
            }
            if (previous == hits) continue;
            if (previous >= 2) windows.Add(new(direction, walk, start, wait - 1, previous));
            (previous, start) = (hits, wait);
        }
        if (previous >= 2) windows.Add(new(direction, walk, start, 175, previous));
    }
    return windows.OrderByDescending(w => w.Hits).ThenByDescending(w => w.WaitEnd - w.WaitStart).ToList();
}
record Trial(string Arm, string Mode, string Weapon, string Layout, string Outcome, int Frames, int Taken,
    int Effective, int Casts, int Hits, int Multi, int AreaDamage, int Triples, int Breaks);
record Window(int Depth, int Walk, int WaitStart, int WaitEnd, int Hits);
