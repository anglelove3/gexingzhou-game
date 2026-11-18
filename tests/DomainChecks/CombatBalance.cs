using GeXingzhou.Domain;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GeXingzhou.DomainChecks;

public sealed record BalanceWave(string Wave, string Result, int HealthStart, int HealthEnd, CombatStatistics Statistics);
public sealed record BalanceAttempt(string Arm, string Mode, string Equipment, string Strategy, int Layout, string ConfigHash,
    string Result, int CompletedWaves, int Health, List<BalanceWave> Waves)
{
    public double TotalSeconds => Waves.Sum(w => w.Statistics.TotalSeconds);
}

public static class CombatBalance
{
    private static readonly string[] Strategies = { "light", "adaptive", "counter" };

    public static CombatCatalog Baseline(CombatCatalog catalog)
    {
        var data = JsonNode.Parse(catalog.SourceJson)!;
        data["balanceVersion"] = "combat-builds-baseline-0.1";
        foreach (var item in data["items"]!.AsArray())
            foreach (var field in new[] { "comboStaminaRestore", "guardBreakPower", "chargedExtraCost", "perfectGuardFrames", "riposteFrames", "riposteCooldown", "ripostePower" })
                item!.AsObject().Remove(field);
        return CombatCatalog.Parse(data.ToJsonString());
    }

    public static List<CombatProgress> Loadouts(CombatCatalog catalog)
    {
        var p = CombatProgress.New(catalog);
        foreach (var wave in catalog.Waves) p = p.Reward(wave.Id, catalog);
        var result = new List<CombatProgress>();
        foreach (var weapon in catalog.Items.Where(i => i.Slot == "weapon"))
        foreach (var coat in catalog.Items.Where(i => i.Slot == "coat"))
        foreach (var shoes in catalog.Items.Where(i => i.Slot == "shoes"))
            result.Add(p.Equip(weapon.Id, catalog).Equip(coat.Id, catalog).Equip(shoes.Id, catalog));
        return result;
    }

    public static BalanceAttempt Sample(CombatCatalog catalog, CombatProgress progress, int strategy, int layout, bool survival, string arm = "mechanics")
    {
        if (strategy is < 0 or > 2 || layout is < 0 or > 2) throw new ArgumentOutOfRangeException();
        var challenge = survival ? CombatChallenge.Start(catalog, progress) : null;
        var waves = new List<BalanceWave>(); string result = "Won"; int complete = 0, health = catalog.Stats(progress).Health;
        foreach (var wave in catalog.Waves)
        {
            var match = challenge?.CreateMatch() ?? new CombatMatch(catalog, progress, wave.Id);
            int start = match.Player.Health;
            foreach (var enemy in match.Enemies)
            {
                if (layout == 1) enemy.X += 32;
                if (layout == 2) enemy.Y += 12;
                enemy.ClampPosition();
            }
            var policy = new Policy(strategy);
            for (int step = 0; step < 7200 && match.Result == CombatResult.Running; step++) match.Step(policy.Decide(match));
            result = match.Result == CombatResult.Running ? "Timeout" : match.Result.ToString();
            health = match.Player.Health;
            waves.Add(new(wave.Id, result, start, health, match.Statistics.Copy()));
            if (result != "Won") break;
            complete++;
            if (challenge != null)
            {
                challenge.FinishWave(match);
                if (complete < catalog.Waves.Count) challenge.Continue();
            }
        }
        var equipment = string.Join("/", new[] { "weapon", "coat", "shoes" }.Select(slot => progress.Equipment[slot]));
        return new(arm, survival ? "survival" : "replay", equipment, Strategies[strategy], layout, catalog.ConfigHash, result, complete, health, waves);
    }

    private sealed class Policy(int strategy)
    {
        private int previousBlocks, counterUntil;
        public CombatInput Decide(CombatMatch match)
        {
            if (strategy == 1) return CombatAutomation.Decide(match);
            var p = match.Player;
            var e = match.Enemies.Where(e => e.Health > 0).OrderBy(e => Math.Abs(e.X - p.X) + Math.Abs(e.Y - p.Y) * 2).FirstOrDefault();
            if (e == null) return default;
            double dx = e.X - p.X, dy = e.Y - p.Y;
            double x = Math.Abs(dx) > 42 ? Math.Sign(dx) : p.Facing != Math.Sign(dx) ? Math.Sign(dx) * .05 : 0;
            double y = Math.Abs(dy) > 7 ? Math.Clamp(dy / 18, -1, 1) : 0;
            if (p.CanAct && p.Stamina < 42)
                return new(MoveX: p.X < 140 ? 1 : p.X > 1140 ? -1 : -Math.Sign(dx), MoveY: match.TickCount % 300 < 150 ? 1 : -1);
            if (p.State == CombatState.Attack)
                return new(LightPressed: p.Attack!.IsLight && p.Attack.Total - p.AttackFrame <= 7 && Math.Abs(dx) <= CombatAutomation.AttackDistance(match));
            if (!p.CanAct) return default;
            if (strategy == 2)
            {
                if (match.Statistics.Blocks > previousBlocks) { previousBlocks = match.Statistics.Blocks; counterUntil = match.TickCount + 90; }
                var incoming = match.Enemies.FirstOrDefault(f => f.Health > 0 && f.State == CombatState.Attack &&
                    f.AttackFrame < f.Attack!.Startup + f.LastActiveOffset && Math.Abs(f.X - p.X) <= 75 && Math.Abs(f.Y - p.Y) <= 26);
                if (incoming != null)
                    return new(MoveX: p.Facing != Math.Sign(incoming.X - p.X) ? Math.Sign(incoming.X - p.X) * .05 : 0,
                        GuardHeld: incoming.AttackFrame >= incoming.Attack!.Startup - 4);
                return new(MoveX: x, MoveY: y, LightPressed: match.TickCount <= counterUntil && Math.Abs(dx) <= CombatAutomation.AttackDistance(match) && Math.Abs(dy) <= 18);
            }
            return new(MoveX: x, MoveY: y, LightPressed: Math.Abs(dx) <= CombatAutomation.AttackDistance(match) && Math.Abs(dy) <= 18);
        }
    }

    public static int Run(string[] args)
    {
        int index = Array.IndexOf(args, "--output");
        if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Supply --output under test-output.");
        var output = Path.GetFullPath(args[index + 1]);
        var boundary = Path.GetFullPath("test-output") + Path.DirectorySeparatorChar;
        if (!output.StartsWith(boundary, StringComparison.OrdinalIgnoreCase) || Directory.Exists(output))
            throw new ArgumentException("Output requires a fresh test-output child directory.");
        Directory.CreateDirectory(output);
        var current = CombatChecks.Catalog();
        int referenceIndex = Array.IndexOf(args, "--reference-config");
        if (referenceIndex >= 0 && referenceIndex + 1 >= args.Length) throw new ArgumentException("Supply a reference JSON path.");
        var baseline = referenceIndex >= 0 ? CombatCatalog.Parse(File.ReadAllText(args[referenceIndex + 1])) : Baseline(current);
        var results = new List<BalanceAttempt>();
        foreach (var arm in new[] { (Name: "baseline", Catalog: baseline), (Name: "mechanics", Catalog: current) })
        {
            foreach (var p in Loadouts(arm.Catalog))
            foreach (var survival in new[] { false, true })
            for (int strategy = 0; strategy < 3; strategy++)
            for (int layout = 0; layout < 3; layout++) results.Add(Sample(arm.Catalog, p, strategy, layout, survival, arm.Name));
            Console.WriteLine("BALANCE_ARM_PASS " + arm.Name + " attempts=486");
        }
        File.WriteAllText(Path.Combine(output, "samples.json"), JsonSerializer.Serialize(new
        {
            protocol = "combat-balance-1", strategyVersion = "policies-20261117-2", level = 5, maximumStepsPerWave = 7200,
            assemblyVersion = typeof(CombatBalance).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                .Cast<System.Reflection.AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion,
            strategySha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes("tests/DomainChecks/CombatBalance.cs"))).ToLowerInvariant(),
            baselineConfig = baseline.SourceJson, mechanicsConfig = current.SourceJson, samples = results
        }, new JsonSerializerOptions { WriteIndented = true }));
        var report = new StringBuilder("# 战斗配置对照测量\n\n自动化控制条件为5级、全装备，3策略、3固定站位，27搭配。每个参数组486尝试，基线与当前合计972尝试，最多5832波。单波上限7200运行步，命中停顿计入。\n\n");
        report.AppendLine(referenceIndex >= 0 ? "本次baseline来自--reference-config指定的完整旧配置，mechanics为当前配置。\n" : "本次baseline移除武器特殊机制，mechanics保留当前机制。\n");
        report.AppendLine($"机制配置SHA256 `{current.ConfigHash}`，基线 `{baseline.ConfigHash}`。策略源码见tests/DomainChecks/CombatBalance.cs及scripts/Testing/CombatAutomation.cs，版本policies-20261117-2。明细及完整派生配置保存在本地samples.json。\n");
        report.AppendLine("| 参数组 | 模式 | 策略 | 武器 | 胜/败/超时 | 胜利平均总秒 | 平均完成波 | 回气次数 | 及时格挡 | 反击命中 | 破势命中 |\n| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var group in results.GroupBy(r => (r.Arm, r.Mode, r.Strategy, Weapon: r.Equipment.Split('/')[0])))
        {
            var wins = group.Where(r => r.Result == "Won").ToArray();
            var seconds = wins.Length > 0 ? wins.Average(r => r.TotalSeconds).ToString("0.0", CultureInfo.InvariantCulture) : "暂无";
            var stats = group.SelectMany(r => r.Waves).Select(w => w.Statistics).ToArray();
            report.AppendLine($"| {group.Key.Arm} | {group.Key.Mode} | {group.Key.Strategy} | {group.Key.Weapon} | {wins.Length}/{group.Count(r => r.Result == "Lost")}/{group.Count(r => r.Result == "Timeout")} | {seconds} | {group.Average(r => r.CompletedWaves):0.00} | {stats.Sum(s => s.StaminaReturns)} | {stats.Sum(s => s.PerfectGuards)} | {stats.Sum(s => s.RiposteHits)} | {stats.Sum(s => s.BreakHits)} |");
        }
        report.AppendLine("\n每行27个确定性样本，覆盖该武器的9种衣鞋组合和3站位。胜利时间只统计六波胜利样本，失败和超时单列。策略读取精确坐标与动作状态，格挡策略精确读取生效帧，结果用于条件对照。真人胜率、手感、阅读时长与首次学习仍待测。伤害明细同时保留计算伤害与有效扣血，总时间包含命中停顿。\n");
        report.AppendLine("## 机制组连战装备组合\n\n每行固定包含3策略×3站位，共9次尝试。均值中的失败/超时样本保留其截至结束时的累计量，完成时间仅采用六波胜利样本。\n\n| 武器/上衣/鞋 | 胜/败/超时 | 平均完成波 | 胜利平均秒 | 平均有效伤害 | 平均承伤 | 平均耐力不足 | 平均结束生命 |\n| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var group in results.Where(r => r.Arm == "mechanics" && r.Mode == "survival").GroupBy(r => r.Equipment))
        {
            var wins = group.Where(r => r.Result == "Won").ToArray();
            var seconds = wins.Length > 0 ? wins.Average(r => r.TotalSeconds).ToString("0.0", CultureInfo.InvariantCulture) : "暂无";
            report.AppendLine($"| {group.Key} | {wins.Length}/{group.Count(r => r.Result == "Lost")}/{group.Count(r => r.Result == "Timeout")} | {group.Average(r => r.CompletedWaves):0.00} | {seconds} | {group.Average(r => r.Waves.Sum(w => w.Statistics.EffectiveDamage)):0.0} | {group.Average(r => r.Waves.Sum(w => w.Statistics.DamageTaken)):0.0} | {group.Average(r => r.Waves.Sum(w => w.Statistics.EmptyStamina)):0.0} | {group.Average(r => r.Health):0.0} |");
        }
        var baselineRows = results.Where(r => r.Arm == "baseline").ToDictionary(r => (r.Mode, r.Equipment, r.Strategy, r.Layout));
        report.AppendLine("\n## 相同条件配对差异\n\n每组采用相同装备、策略、模式和站位配对。时间差为机制减基线，仅采用双方六波胜利的配对。负值表示该配对机制组较快。转胜/转败统计全部27个配对，避免直接把不同胜利样本的均值视为机制收益。\n\n| 模式 | 策略 | 武器 | 双方胜利配对 | 平均时间差秒 | 基线败转胜 | 基线胜转败 |\n| --- | --- | --- | --- | --- | --- | --- |");
        foreach (var group in results.Where(r => r.Arm == "mechanics").GroupBy(r => (r.Mode, r.Strategy, Weapon: r.Equipment.Split('/')[0])))
        {
            var pairs = group.Select(r => (Current: r, Baseline: baselineRows[(r.Mode, r.Equipment, r.Strategy, r.Layout)])).ToArray();
            var bothWon = pairs.Where(p => p.Current.Result == "Won" && p.Baseline.Result == "Won").ToArray();
            var delta = bothWon.Length == 0 ? "暂无" : bothWon.Average(p => p.Current.TotalSeconds - p.Baseline.TotalSeconds).ToString("0.00", CultureInfo.InvariantCulture);
            report.AppendLine($"| {group.Key.Mode} | {group.Key.Strategy} | {group.Key.Weapon} | {bothWon.Length} | {delta} | {pairs.Count(p => p.Current.Result == "Won" && p.Baseline.Result != "Won")} | {pairs.Count(p => p.Current.Result != "Won" && p.Baseline.Result == "Won")} |");
        }
        File.WriteAllText(Path.Combine(output, "summary.md"), report.ToString(), new UTF8Encoding(false));
        Console.WriteLine($"BALANCE_PASS attempts={results.Count} waves={results.Sum(r => r.Waves.Count)} output={output}");
        return 0;
    }
}
