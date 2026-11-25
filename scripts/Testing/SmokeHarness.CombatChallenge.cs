using Godot;
using GeXingzhou.Domain;
using GeXingzhou.DomainChecks;

public partial class SmokeHarness
{
    private async Task CombatChallengeChecks()
    {
        var scene = GD.Load<PackedScene>("res://scenes/combat/CombatDemo.tscn");
        var demo = scene.Instantiate<CombatDemo>(); AddChild(demo); await Frames(4);
        Require(demo.GetNode<Button>("Prepare/Footer/Content/Actions/Challenge").Disabled, "Fresh challenge must be locked");
        var directory = (string)DiagnosticField(demo, "saveDirectory")!;
        var progress = demo.Progress;
        foreach (var wave in demo.Catalog.Waves) progress = progress.Reward(wave.Id, demo.Catalog);
        progress = progress.Equip("stick", demo.Catalog).Equip("padded", demo.Catalog).Equip("steady", demo.Catalog);
        Require(new CombatRepository(directory, demo.Catalog).Save(progress).Success, "Challenge fixture save failed");
        demo.Free(); await Frames(3);
        demo = scene.Instantiate<CombatDemo>(); AddChild(demo); await Frames(4);
        var session = GetNode<GameSession>("/root/GameSession"); var oldOptions = session.Options;
        if (OS.GetCmdlineUserArgs().Contains("--combat-font=32")) session.SetOptions(oldOptions with { SubtitleSize = 32, ReducedMotion = true }, false);
        await CombatWeaponRuntimeChecks(demo);
        Require(demo.TryEquip("stick"), "Challenge loadout could not be restored");
        await ChallengeExitFailureChecks(demo, directory);
        await UnstartedEndedAttemptChecks(demo, directory);
        demo.GetNode<Button>("Prepare/Footer/Content/Actions/Challenge").EmitSignal(Button.SignalName.Pressed); await Frames(2);
        Require(demo.GetNode<Control>("Choices").Visible, "Challenge confirmation missing");
        demo.GetNode<Button>("Choices/Panel/Content/Options/Option1").GrabFocus();
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.Enter, Keycode = Key.Enter, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.Enter, Keycode = Key.Enter, Pressed = false }); await Frames(4);
        Require(demo.ChallengeRecord != null && demo.Screen == "fight", "Challenge keyboard entry failed");
        var keys = new HashSet<Key>();
        void SetKey(Key key, bool down, bool pulse = false)
        {
            if (keys.Contains(key) && (!down || pulse)) { Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = false }); keys.Remove(key); }
            if (down && keys.Add(key)) Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = true });
        }
        int previousHealth = demo.Match!.Player.Health;
        for (int wave = 0; wave < 6; wave++)
        {
            Require(demo.Match!.Player.Health == previousHealth, "Challenge health inheritance changed");
            Require(demo.Match.Player.Stamina == demo.Match.Player.Stats.Stamina, "Challenge stamina did not reset");
            if (demo.Match.Wave.Id == "b2")
            {
                var hint = demo.GetNode<Label>("Hud/Wave");
                Require(hint.Text.Contains(AreaTimingAdvice, StringComparison.Ordinal) &&
                    hint.Text.Contains("连战 · 生命继承 / 耐力逐波回满", StringComparison.Ordinal),
                    "Fourth-wave challenge must show Q timing and challenge rules");
                Require(hint.GetLineCount() == hint.GetVisibleLineCount() &&
                    demo.GetGlobalRect().Encloses(hint.GetGlobalRect()) &&
                    !hint.GetGlobalRect().Intersects(demo.GetNode<Control>("Hud/Top").GetGlobalRect()),
                    "Fourth-wave challenge timing hint was clipped or overlapped");
                await Capture("area-timing-challenge");
            }
            if (demo.Match.Wave.Id == "c1")
            {
                var hint = demo.GetNode<Label>("Hud/Wave");
                Require(hint.Text.Contains(FifthWaveAreaAdvice, StringComparison.Ordinal) &&
                    hint.Text.Contains("连战 · 生命继承 / 耐力逐波回满", StringComparison.Ordinal) &&
                    hint.GetLineCount() == hint.GetVisibleLineCount(), "Fifth-wave challenge advice is missing or clipped");
                Require(demo.GetNode<Label>("Hud/Top/Content/Action").Text.Contains("Q就绪", StringComparison.Ordinal),
                    "Challenge fifth-wave Q was not ready at the start");
                await Capture("wave5-sweep-challenge");
            }
            System.IO.FileStream? blocked = null;
            for (int tick = 0; tick < 12000 && demo.Match.Result == CombatResult.Running; tick++)
            {
                if (demo.Screen == "pause")
                {
                    foreach (var key in keys.ToArray()) SetKey(key, false);
                    await Frames(2); if (session.ApplicationFocused) demo.ResumeCombat(); continue;
                }
                if (wave == 0 && blocked == null && demo.Match.Enemies.Sum(e => e.Health) < 30)
                    blocked = new System.IO.FileStream(System.IO.Path.Combine(directory, "challenges-v1", "attempt-" + demo.ChallengeRecord!.AttemptId + ".lock"), System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None);
                var input = CombatAutomation.Decide(demo.Match);
                SetKey(Key.A, input.MoveX < 0); SetKey(Key.D, input.MoveX > 0); SetKey(Key.W, input.MoveY < 0); SetKey(Key.S, input.MoveY > 0);
                SetKey(Key.J, input.LightPressed, input.LightPressed); SetKey(Key.K, input.HeavyPressed || input.HeavyHeld, input.HeavyPressed); SetKey(Key.L, input.GuardHeld);
                await Frames(1);
            }
            foreach (var key in keys.ToArray()) SetKey(key, false);
            for (int settle = 0; settle < 30 && demo.Screen == "finish"; settle++) await Frames(1);
            Require(demo.Match.Result == CombatResult.Won, "Challenge physical keyboard failed wave " + wave);
            if (demo.Match.Wave.Id == "c1")
                Require(demo.GetNode<Label>("Result/Panel/Scroll/Content/Statistics").Text.Contains(FifthWaveAreaCount + " 0", StringComparison.Ordinal),
                    "Challenge fifth-wave result must report zero multi-target casts when Q was unused");
            previousHealth = demo.Match.Player.Health;
            Require(demo.ChallengeRecord!.CompletedWaves == wave + 1, "Challenge duplicate or missing completion");
            if (blocked != null)
            {
                Require(demo.GetNode<Button>("Result/Panel/Scroll/Content/Retry").Disabled, "Save failure allowed restart");
                blocked.Dispose();
                var remote = new CombatRepository(directory, demo.Catalog);
                Require(remote.PreserveForNewGame().Success && remote.Save(CombatProgress.New(demo.Catalog)).Success, "Concurrent restart fixture failed");
            }
            await Capture("challenge-result-" + wave);
            demo.GetNode<Button>("Result/Panel/Scroll/Content/Continue").EmitSignal(Button.SignalName.Pressed); await Frames(3);
        }
        Require(demo.Screen == "prepare" && demo.Progress.Experience == 0, "Challenge overwrote or failed to reload new run");
        var history = new CombatChallengeRepository(System.IO.Path.Combine(directory, "challenges-v1")).LoadHistory();
        Require(history.Records.Count == 3 && history.Records.Count(r => r.Outcome == ChallengeOutcome.Won) == 1, "Challenge history missing");
        Require(history.Records[0].Waves.All(w => w.Statistics.TotalFrames > 0), "Challenge statistics lost on reload");
        await Capture("challenge-history-ready");
        var historyRepository = new CombatChallengeRepository(System.IO.Path.Combine(directory, "challenges-v1"));
        for (int i = 0; i < 6; i++) Require(historyRepository.Save(CombatChallenge.Start(demo.Catalog, history.Records[0].Progress).Record).Success, "Long history fixture failed");
        demo.GetNode<Button>("Prepare/Footer/Content/Actions/History").EmitSignal(Button.SignalName.Pressed); await Frames(3);
        Require(demo.Screen == "history" && demo.GetNode<Control>("Result").Visible, "History did not use scrollable result view");
        var historyScroll = demo.GetNode<ScrollContainer>("Result/Panel/Scroll"); historyScroll.ScrollVertical = 0;
        await Capture("challenge-history");
        SetKey(Key.Pagedown, true); SetKey(Key.Pagedown, false); await Frames(2);
        Require(historyScroll.ScrollVertical > 0, "History PageDown did not scroll long records");
        demo.GetNode<Button>("Result/Panel/Scroll/Content/Continue").EmitSignal(Button.SignalName.Pressed); await Frames(2);
        Require(demo.Screen == "prepare", "History return failed");
        demo.Free(); await Frames(3); session.SetOptions(oldOptions, false);
        GD.Print("COMBAT_CHALLENGE_PASS LockedEntry KeyboardSixWaves Snapshot HealthCarry StaminaReset SaveRetry ConcurrentRestart History");
    }

    private async Task ChallengeExitFailureChecks(CombatDemo demo, string directory)
    {
        Require(demo.StartChallenge(), "Exit failure fixture could not start"); await Frames(3);
        demo.PauseCombat(); var tick = demo.Match!.TickCount;
        using (var lease = new System.IO.FileStream(System.IO.Path.Combine(directory, "challenges-v1", "attempt-" + demo.ChallengeRecord!.AttemptId + ".lock"),
            System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None))
        {
            demo.ReturnToPreparation();
            Require(demo.Screen == "result", "Failed end save kept resumable fight screen");
            demo.ResumeCombat(); await Frames(20);
            Require(demo.Screen == "result" && demo.Match.TickCount == tick, "Ended challenge resumed while save pending");
        }
        demo.GetNode<Button>("Result/Panel/Scroll/Content/Continue").EmitSignal(Button.SignalName.Pressed); await Frames(3);
        Require(demo.Screen == "prepare" && demo.ChallengeRecord == null, "Failed end save retry blocked navigation");
        GD.Print("COMBAT_CHALLENGE_EXIT_FAILURE_PASS EndedCandidateFrozen RetryNavigates");
    }

    private async Task UnstartedEndedAttemptChecks(CombatDemo demo, string directory)
    {
        var field = typeof(CombatDemo).GetField("challengeRepository", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var original = (CombatChallengeRepository)field.GetValue(demo)!;
        var blockedPath = System.IO.Path.Combine(directory, "blocked-challenge-fixture");
        System.IO.File.WriteAllText(blockedPath, "occupied");
        field.SetValue(demo, new CombatChallengeRepository(blockedPath));
        Require(!demo.StartChallenge() && demo.Match == null, "Initial I/O fixture did not block challenge");
        var attempt = (CombatChallenge)DiagnosticField(demo, "challenge")!;
        field.SetValue(demo, original);
        Require(original.Save(attempt.Record).Success, "Initial record retry fixture failed");
        attempt.End();
        // Freeze the candidate reached when a later exit write fails after initial-save recovery.
        demo.GetNode<Button>("Result/Panel/Scroll/Content/Continue").EmitSignal(Button.SignalName.Pressed); await Frames(3);
        Require(demo.Screen == "prepare" && demo.ChallengeRecord == null, "Unstarted ended candidate resumed a match");
        GD.Print("COMBAT_CHALLENGE_UNSTARTED_END_PASS RetryReturnsWithoutFight");
    }

    private async Task CombatWeaponRuntimeChecks(CombatDemo demo)
    {
        void KeyEvent(Key key, bool down) => Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = down });
        foreach (var weapon in new[] { "gloves", "stick", "wraps" })
        {
            Require(demo.TryEquip(weapon), "Weapon fixture equip failed");
            var waves = demo.GetNode<OptionButton>("Prepare/Panel/Scroll/Content/Wave");
            var index = weapon == "stick" ? 2 : 0;
            waves.Select(index); waves.EmitSignal(OptionButton.SignalName.ItemSelected, index);
            demo.GetNode<Button>("Prepare/Footer/Content/Actions/Start").EmitSignal(Button.SignalName.Pressed); await Frames(2);
            var match = demo.Match!; var enemy = match.Enemies[0];
            enemy.X = match.Player.X + 30; enemy.Y = match.Player.Y; enemy.Facing = -1;
            bool guardHeld = false;
            for (int frame = 0; frame < 480 && match.Result == CombatResult.Running; frame++)
            {
                if (demo.Screen == "pause") { GetWindow().GrabFocus(); await Frames(3); demo.ResumeCombat(); continue; }
                if (weapon == "gloves" && frame % 4 == 0) { KeyEvent(Key.J, true); KeyEvent(Key.J, false); }
                if (weapon == "stick")
                {
                    if (frame == 0) KeyEvent(Key.K, true);
                    if (frame == 28) KeyEvent(Key.K, false);
                    if (frame == 35) { enemy.SetGuard(false); enemy.SetGuard(true); }
                }
                if (weapon == "wraps")
                {
                    bool incoming = enemy.State == CombatState.Attack && enemy.AttackFrame >= enemy.Attack!.Startup - 4 && enemy.AttackFrame < enemy.Attack.Startup + enemy.LastActiveOffset;
                    if (incoming != guardHeld) { KeyEvent(Key.L, incoming); guardHeld = incoming; }
                    if (match.Statistics.PerfectGuards > 0)
                    {
                        if (guardHeld) { KeyEvent(Key.L, false); guardHeld = false; }
                        if (frame % 4 == 0) { KeyEvent(Key.J, true); KeyEvent(Key.J, false); }
                    }
                }
                await Frames(1);
                if (match.Statistics.StaminaReturns > 0 || match.Statistics.BreakHits > 0 || match.Statistics.RiposteHits > 0) break;
            }
            KeyEvent(Key.J, false); KeyEvent(Key.K, false); KeyEvent(Key.L, false);
            Require(weapon switch { "gloves" => match.Statistics.StaminaReturns > 0, "stick" => match.Statistics.BreakHits > 0,
                _ => match.Statistics.PerfectGuards > 0 && match.Statistics.RiposteHits > 0 }, "Physical weapon trigger failed " + weapon);
            await Capture("build-trigger-" + weapon);
            demo.ReturnToPreparation(); await Frames(3);
            GD.Print("COMBAT_WEAPON_RUNTIME_PASS " + weapon);
        }
    }
}
