using Godot;
using GeXingzhou.Domain;

public partial class CombatDemo
{
    private CombatChallenge? challenge;
    private CombatChallengeRepository challengeRepository = null!;
    private bool challengeSavePending;
    private string? historyRecoveryId;
    public CombatChallengeRecord? ChallengeRecord => challenge?.Record;
    private const string ChallengeRules = "通关后街头连战 · 六波\n5级，整轮锁定当前装备；生命逐波继承，每波耐力回满。\n失败或退出后从第一波重新挑战。完成后记录成绩，成长沿用普通练习。";

    private void BindChallengeUi()
    {
        challengeRepository = new(System.IO.Path.Combine(saveDirectory, "challenges-v1"));
        GetNode<Button>(PrepActions + "Challenge").Pressed += () => choices.Open(ChallengeRules,
            new (string, Action)[] { ("锁定装备并开始", () => StartChallenge()), ("返回准备", () => { }) });
        GetNode<Button>(PrepActions + "History").Pressed += ShowChallengeHistory;
    }

    public bool StartChallenge()
    {
        if (challenge != null || blockedSave || !Progress.CompletedRun || Screen != "prepare") return false;
        challenge = CombatChallenge.Start(Catalog, Progress);
        if (!SaveChallenge())
        {
            ChangeScreen("result");
            GetNode<Label>(ResultPath + "Title").Text = "挑战记录待保存";
            GetNode<Label>(ResultPath + "Reward").Text = ChallengeRules;
            GetNode<Label>(ResultPath + "Statistics").Text = "";
            GetNode<Button>(ResultPath + "Continue").Text = "重试保存并开始";
            GetNode<Button>(ResultPath + "Retry").Disabled = true;
            GetNode<Button>(ResultPath + "Export").Disabled = true;
            BindFocus(ResultPath, new[] { "Continue", "Export", "Menu" });
            return false;
        }
        return StartChallengeWave();
    }

    private bool SaveChallenge()
    {
        var saved = challengeRepository.Save(challenge!.Record);
        challengeSavePending = !saved.Success;
        GetNode<Label>(ResultPath + "Message").Text = saved.Message;
        return saved.Success;
    }

    private bool StartChallengeWave()
    {
        if (challenge == null || challengeSavePending) return false;
        var snapshot = challenge.Record;
        attemptProgress = snapshot.Progress;
        Match = challenge.CreateMatch(); arena.Match = Match;
        ChangeScreen("fight"); return true;
    }

    private void ShowChallengeResult()
    {
        challenge!.FinishWave(Match!);
        ChangeScreen("result");
        var record = challenge.Record;
        var saved = SaveChallenge();
        GetNode<Label>(ResultPath + "Title").Text = record.Outcome == ChallengeOutcome.Won ? "六波连战完成" :
            record.Outcome == ChallengeOutcome.Lost ? "本次连战结束" : "连战波次完成";
        GetNode<Label>(ResultPath + "Reward").Text = $"已完成 {record.CompletedWaves}/6 波 · 剩余生命 {Match!.Player.Health}/{Match.Player.Stats.Health}\n" +
            (record.Outcome == ChallengeOutcome.Running ? "下一波继承当前生命，耐力回满。装备保持开场快照。" : "本次成绩保留，可以回准备界面调整装备。");
        GetNode<Label>(ResultPath + "Statistics").Text = StatisticsText(Match.Statistics) + $"\n连战累计战斗总时间 {record.TotalSeconds:0.0} 秒";
        GetNode<Button>(ResultPath + "Continue").Text = saved ? record.Outcome == ChallengeOutcome.Running ? "继续下一波" : "返回准备界面" : "重试保存并继续";
        GetNode<Button>(ResultPath + "Retry").Text = "重新挑战六波";
        GetNode<Button>(ResultPath + "Retry").Disabled = !saved;
        BindFocus(ResultPath, new[] { "Continue", "Retry", "Export", "Menu" });
    }

    private void ContinueChallengeResult()
    {
        if (challengeSavePending && !SaveChallenge()) return;
        if (challenge!.Record.Outcome != ChallengeOutcome.Running) { ReturnToPreparation(); return; }
        if (Match == null) { StartChallengeWave(); return; }
        challenge.Continue(); StartChallengeWave();
    }

    private bool EndChallenge()
    {
        if (challenge == null) return true;
        if (challengeSavePending && !SaveChallenge()) return false;
        challenge.End();
        if (!SaveChallenge())
        {
            ChangeScreen("result");
            GetNode<Label>(ResultPath + "Title").Text = "挑战已结束，记录待保存";
            GetNode<Label>(ResultPath + "Reward").Text = $"已完成 {challenge.Record.CompletedWaves}/6 波，保存后可返回准备界面。";
            GetNode<Label>(ResultPath + "Statistics").Text = Match == null ? "" : StatisticsText(Match.Statistics);
            GetNode<Button>(ResultPath + "Continue").Text = "重试保存并返回";
            GetNode<Button>(ResultPath + "Retry").Disabled = true;
            BindFocus(ResultPath, new[] { "Continue", "Export", "Menu" });
            return false;
        }
        challenge = null; challengeSavePending = false;
        var loaded = repository.Load();
        blockedSave = loaded.Status != LoadStatus.Loaded && (loaded.Status != LoadStatus.NotFound || repository.LoadBackup().Status != LoadStatus.NotFound);
        Progress = loaded.Progress ?? CombatProgress.New(Catalog); selectedWave = Progress.LastWave;
        return true;
    }

    private void RetryCombat()
    {
        if (Screen == "history")
        {
            if (historyRecoveryId == null) return;
            var message = challengeRepository.RecoverBackup(historyRecoveryId).Message;
            ShowChallengeHistory(); GetNode<Label>(ResultPath + "Message").Text = message; return;
        }
        if (challenge == null) { StartWave(); return; }
        if (!EndChallenge()) return;
        Match = null; arena.Match = null; ShowPreparation();
        if (Progress.CompletedRun) StartChallenge();
        else Message("普通周目已变化，完成当前周目后可再次挑战连战。");
    }

    private void ShowChallengeHistory()
    {
        var history = challengeRepository.LoadHistory();
        var compatible = history.Records.Where(r => r.ConfigHash == Catalog.ConfigHash &&
            r.Progress.Equipment.OrderBy(x => x.Key).SequenceEqual(Progress.Equipment.OrderBy(x => x.Key))).ToArray();
        var best = compatible.Where(r => r.Outcome == ChallengeOutcome.Won).OrderBy(r => r.TotalSeconds).FirstOrDefault();
        var lines = new List<string> { $"最近 {Math.Min(8, history.Records.Count)} 条，共 {history.Records.Count} 条记录。\n可用滚轮或PgUp/PgDn查看，历史文件持续保留。" };
        foreach (var record in history.Records.Take(8))
        {
            var status = record.Outcome switch { ChallengeOutcome.Won => "完成", ChallengeOutcome.Lost => "失败", ChallengeOutcome.Abandoned => "主动结束", _ => "中断" };
            var frozenCatalog = CombatCatalog.Parse(record.ConfigJson);
            var names = record.Progress.Equipment.Values.Select(id => frozenCatalog.Item(id).Name);
            lines.Add($"{record.StartedAt.ToOffset(TimeSpan.FromHours(8)):MM-dd HH:mm} · {status} {record.CompletedWaves}/6 · {record.TotalSeconds:0.0}秒\n{string.Join(" / ", names)} · {record.BalanceVersion}");
        }
        if (history.Records.Count == 0) lines.Add("暂无挑战记录，普通六波通关后可开始连战。");
        lines.AddRange(history.Issues.Take(4));
        historyRecoveryId = null;
        foreach (var issue in history.Issues.Take(4))
        {
            var name = issue.Split(' ')[0];
            var id = name.StartsWith("attempt-") && name.EndsWith(".json") ? name[8..^5] : "";
            if (Guid.TryParseExact(id, "N", out _) && historyRecoveryId == null) historyRecoveryId = id;
        }
        ChangeScreen("history");
        GetNode<Label>(ResultPath + "Title").Text = "街头连战记录";
        GetNode<Label>(ResultPath + "Reward").Text = best == null ? "当前配置与装备尚无六波完成成绩。" : $"当前配置/装备最快六波 {best.TotalSeconds:0.0} 秒";
        GetNode<Label>(ResultPath + "Statistics").Text = string.Join("\n\n", lines);
        GetNode<Label>(ResultPath + "Message").Text = "";
        GetNode<Button>(ResultPath + "Continue").Text = "返回准备界面";
        GetNode<Button>(ResultPath + "Retry").Visible = historyRecoveryId != null;
        GetNode<Button>(ResultPath + "Retry").Disabled = historyRecoveryId == null;
        GetNode<Button>(ResultPath + "Retry").Text = "保留原文件并恢复首条异常记录备份";
        GetNode<Button>(ResultPath + "Export").Disabled = true;
        BindFocus(ResultPath, new[] { "Continue", "Retry", "Menu" });
        GetNode<ScrollContainer>("Result/Panel/Scroll").ScrollVertical = 0;
    }

    private static string StatisticsText(CombatStatistics s) =>
        $"可操作时间 {s.ActiveSeconds:0.0}秒 · 战斗总时间 {s.TotalSeconds:0.0}秒\n" +
        $"轻击 {s.LightAttacks} · 重击 {s.HeavyAttacks} · 闪避 {s.Dodges}（衔接{s.DodgeCancels}）· 格挡 {s.Blocks}\n" +
        $"造成伤害 {s.DamageDealt} · 有效伤害 {s.EffectiveDamage}\n受击 {s.HitsTaken}次 · 承受伤害 {s.DamageTaken}\n" +
        $"有效伤害/秒 {(s.TotalSeconds > 0 ? (s.EffectiveDamage / s.TotalSeconds).ToString("0.0") : "暂无")} · 耐力不足 {s.EmptyStamina}次\n" +
        $"回气 {s.StaminaReturns}次/{s.StaminaRestored:0}耐力 · 及时格挡 {s.PerfectGuards}次\n" +
        $"反击发动/命中 {s.RiposteAttacks}/{s.RiposteHits} · 破势蓄力发动/命中 {s.BreakAttacks}/{s.BreakHits}" +
        (s.CriticalHits > 0 || s.AreaCasts > 0 ? $"\n暴击 {s.CriticalHits} · 横扫发动/有效命中 {s.AreaCasts}/{s.AreaHits}" : "") +
        (s.AreaDoubleHits is int doubleHits && s.AreaDamage is int areaDamage ?
            $"\n横扫双中次数 {doubleHits} · 范围伤害 {areaDamage}" : "");
}
