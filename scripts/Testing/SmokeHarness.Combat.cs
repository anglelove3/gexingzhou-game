using Godot;
using GeXingzhou.Domain;
using GeXingzhou.DomainChecks;

public partial class SmokeHarness
{
    private async Task CombatChecks()
    {
        var scene = GD.Load<PackedScene>("res://scenes/combat/CombatDemo.tscn");
        Require(scene != null, "Combat entry scene missing");
        var session = GetNode<GameSession>("/root/GameSession"); var oldOptions = session.Options;
        if (OS.GetCmdlineUserArgs().Contains("--combat-font=32")) session.SetOptions(oldOptions with { SubtitleSize = 32, ReducedMotion = true }, false);
        var demo = scene!.Instantiate<CombatDemo>(); AddChild(demo); await Frames(5);
        Require(demo.Screen == "prepare" && demo.Progress.Experience == 0, "Fresh combat preparation failed");
        Require(demo.Catalog.Items.Count == 9, "Combat equipment catalog missing");
        Require(demo.GetNode<PanelContainer>("Prepare/Panel").GetGlobalRect().Grow(1).Encloses(
            demo.GetNode<Label>("Prepare/Footer/Content/Message").GetGlobalRect()), "Combat fixed footer overflowed preparation panel");
        await Capture("combat-prepare");
        Require(demo.StartWave(), "Cannot start combat from normal entry"); await Frames(3);
        var x = demo.Match!.Player.X;
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.D, Keycode = Key.D, Pressed = true });
        await Frames(20);
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.D, Keycode = Key.D, Pressed = false });
        Require(demo.Match.Player.X > x, "Combat physical movement key ignored");
        KeyPress(Key.J); await Frames(3);
        Require(demo.Match.Statistics.LightAttacks == 1, "Combat physical attack key ignored");
        KeyPress(Key.Escape); await Frames(2);
        Require(demo.Screen == "pause", "Combat Escape pause failed");
        var before = demo.Match.TickCount; await Frames(12);
        Require(demo.Match.TickCount == before, "Combat advanced while paused");
        var resume = demo.GetNode<Button>("Pause/Panel/Scroll/Content/Resume");
        var menu = demo.GetNode<Button>("Pause/Panel/Scroll/Content/Menu");
        menu.GrabFocus(); KeyPress(Key.Tab); await Frames(2);
        Require(resume.HasFocus(), "Combat pause focus escaped");
        demo.ResumeCombat(); demo.Notification((int)NotificationApplicationFocusOut); await Frames(5);
        Require(demo.Screen == "pause", "Combat focus loss did not pause");
        demo.Notification((int)NotificationApplicationFocusIn); demo.ReturnToPreparation(); await Frames(3);
        Require(demo.Match == null && demo.Progress.Experience == 0, "Abandoning wave granted reward");
        Require(!demo.TryEquip("stick"), "Unowned equipment could be worn");
        Require(demo.StartWave(), "Pause-charge fixture failed");
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.K, Keycode = Key.K, Pressed = true });
        await Frames(29); Require(demo.Match!.Player.State == CombatState.Charging, "Charge fixture failed");
        KeyPress(Key.Escape); await Frames(2);
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.K, Keycode = Key.K, Pressed = false });
        KeyPress(Key.Escape); await Frames(30);
        Require(demo.Match.Statistics.HeavyAttacks == 0, "Pause released a stale charged attack without a new press");
        demo.ReturnToPreparation(); await Frames(3);
        var story = System.Text.Json.JsonSerializer.Serialize(GetNode<GameSession>("/root/GameSession").Snapshot);
        var pressed = new HashSet<Key>();
        void SetKey(Key key, bool down, bool pulse = false)
        {
            if (pressed.Contains(key) && (!down || pulse))
            {
                Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = false }); pressed.Remove(key);
            }
            if (down && pressed.Add(key)) Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = true });
        }
        void ApplyInput(CombatInput input)
        {
            SetKey(Key.A, input.MoveX < 0); SetKey(Key.D, input.MoveX > 0);
            SetKey(Key.W, input.MoveY < 0); SetKey(Key.S, input.MoveY > 0);
            SetKey(Key.J, input.LightPressed, input.LightPressed);
            SetKey(Key.K, input.HeavyPressed || input.HeavyHeld, input.HeavyPressed);
            SetKey(Key.L, input.GuardHeld); SetKey(Key.Space, input.DodgePressed, input.DodgePressed);
        }
        foreach (var wave in demo.Catalog.Waves)
        {
            foreach (var id in new[] { "stick", "padded", "steady" }) if (demo.Progress.Owned.Contains(id)) Require(demo.TryEquip(id), "Equip through UI failed");
            Require(demo.StartWave(), "Cannot start wave " + wave.Id);
            System.IO.FileStream? blockedWrite = null;
            for (int tick = 0; tick < 12000 && demo.Match!.Result == CombatResult.Running; tick++)
            {
                if (captureDirectory != null && demo.Screen == "pause")
                {
                    GetWindow().GrabFocus(); await Frames(3);
                    if (GetNode<GameSession>("/root/GameSession").ApplicationFocused) demo.ResumeCombat();
                    continue;
                }
                var input = CombatAutomation.Decide(demo.Match!);
                if (wave.Id == "a1" && blockedWrite == null && demo.Match.Enemies.Sum(e => e.Health) <= 30)
                {
                    var directory = (string)DiagnosticField(demo, "saveDirectory")!;
                    Require(directory.StartsWith(ProjectSettings.GlobalizePath("res://test-output/")), "Unsafe combat save fixture");
                    blockedWrite = new System.IO.FileStream(System.IO.Path.Combine(directory, "write.lock"), System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None);
                }
                ApplyInput(input);
                await Frames(1);
                if (captureDirectory != null && tick == 100) await Capture("combat-" + wave.Id + "-action");
            }
            foreach (var key in pressed.ToArray()) SetKey(key, false);
            Require(demo.Match!.Result == CombatResult.Won && demo.Screen == "result", "Keyboard combat failed " + wave.Id + " hp=" + demo.Match.Player.Health);
            var finished = demo.Match;
            await Capture("combat-" + wave.Id + "-result");
            if (blockedWrite != null)
            {
                Require(demo.Progress.Experience == 0 && demo.GetNode<Button>("Result/Panel/Scroll/Content/Retry").Disabled, "Failed save lost reward boundary");
                blockedWrite.Dispose();
                GD.Print("COMBAT_REWARD_SAVE_RETRY_READY");
            }
            demo.GetNode<Button>("Result/Panel/Scroll/Content/Continue").EmitSignal(Button.SignalName.Pressed); await Frames(2);
            Require(demo.Progress.Completed.Contains(wave.Id), "Reward was not saved " + wave.Id);
            GD.Print($"COMBAT_KEYBOARD_WAVE {wave.Id} xp={demo.Progress.Experience} hp={finished.Player.Health} seconds={finished.Statistics.ActiveSeconds:0.0}");
        }
        Require(demo.Progress.Experience == 480 && demo.Progress.Owned.Count == 9, "Complete growth/equipment missing");
        Require(story == System.Text.Json.JsonSerializer.Serialize(GetNode<GameSession>("/root/GameSession").Snapshot), "Combat changed story snapshot");
        await Capture("combat-complete-equipment");
        demo.Free(); await Frames(3);
        var loaded = scene.Instantiate<CombatDemo>(); AddChild(loaded); await Frames(3);
        Require(loaded.Progress.Experience == 480 && loaded.Progress.CompletedRun, "Completed combat progress failed to resume");
        loaded.GetNode<Button>("Prepare/Footer/Content/Actions/Reset").EmitSignal(Button.SignalName.Pressed); await Frames(2);
        loaded.GetNode<Button>("Choices/Panel/Content/Options/Option1").EmitSignal(Button.SignalName.Pressed); await Frames(3);
        Require(loaded.Progress.Experience == 0 && loaded.StartWave(), "Concurrent fixture reset failed");
        bool advanced = false;
        for (int tick = 0; tick < 12000 && loaded.Match!.Result == CombatResult.Running; tick++)
        {
            if (loaded.Screen == "pause")
            {
                foreach (var key in pressed.ToArray()) SetKey(key, false);
                await Frames(2);
                if (session.ApplicationFocused) loaded.ResumeCombat();
                continue;
            }
            if (!advanced && loaded.Match.Enemies.Sum(e => e.Health) <= 30)
            {
                var directory = (string)DiagnosticField(loaded, "saveDirectory")!;
                var remote = new CombatRepository(directory, loaded.Catalog);
                Require(remote.Save(loaded.Progress.Reward("a1", loaded.Catalog).Reward("a2", loaded.Catalog)).Success, "Concurrent source advance failed");
                advanced = true;
            }
            ApplyInput(CombatAutomation.Decide(loaded.Match)); await Frames(1);
        }
        foreach (var key in pressed.ToArray()) SetKey(key, false);
        Require(advanced && loaded.Progress.Experience == 60, "Concurrent newer save did not resolve pending reward");
        loaded.GetNode<Button>("Result/Panel/Scroll/Content/Continue").EmitSignal(Button.SignalName.Pressed); await Frames(2);
        Require(loaded.Screen == "prepare" && loaded.Progress.Experience == 60, "Concurrent settlement blocked navigation");
        GD.Print("COMBAT_CONCURRENT_REWARD_PASS NewerProgressIncludesReward NavigationAvailable");
        Require(loaded.StartWave(), "Cross-run fixture could not start");
        bool restarted = false;
        var crossRunDirectory = (string)DiagnosticField(loaded, "saveDirectory")!;
        for (int tick = 0; tick < 12000 && loaded.Match!.Result == CombatResult.Running; tick++)
        {
            if (loaded.Screen == "pause")
            {
                foreach (var key in pressed.ToArray()) SetKey(key, false);
                await Frames(2);
                if (session.ApplicationFocused) loaded.ResumeCombat();
                continue;
            }
            if (!restarted && loaded.Match.Enemies.Sum(e => e.Health) <= 40)
            {
                var remote = new CombatRepository(crossRunDirectory, loaded.Catalog);
                Require(remote.PreserveForNewGame().Success && remote.Save(CombatProgress.New(loaded.Catalog)).Success, "Remote restart failed");
                restarted = true;
            }
            ApplyInput(CombatAutomation.Decide(loaded.Match)); await Frames(1);
        }
        foreach (var key in pressed.ToArray()) SetKey(key, false);
        Require(restarted && loaded.Progress.Experience == 0 && new CombatRepository(crossRunDirectory, loaded.Catalog).Load().Progress!.Experience == 0,
            "Cross-run candidate replaced restarted progress");
        Require(System.IO.Directory.GetFiles(crossRunDirectory, "preserved-attempt-*.json").Length == 1, "Old-run reward candidate was not preserved");
        loaded.GetNode<Button>("Result/Panel/Scroll/Content/Continue").EmitSignal(Button.SignalName.Pressed); await Frames(2);
        Require(loaded.Screen == "prepare" && loaded.Progress.Experience == 0, "Cross-run settlement blocked navigation");
        GD.Print("COMBAT_NEW_RUN_PASS RestartProtected OldCandidatePreserved NavigationAvailable");
        loaded.Free(); await Frames(3);
        session.SetOptions(oldOptions, false);
        GD.Print("COMBAT_GROWTH_PASS SixWaves PhysicalKeys Level5 NineItems SavedResume StorySnapshotPreserved");
        GD.Print("COMBAT_ENTRY_PASS Keyboard Movement Attack Pause Focus Equipment SaveIsolation");
    }
}
