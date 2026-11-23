using Godot;
using GeXingzhou.Domain;
using System.Reflection;

public partial class SmokeHarness
{
    private const string AreaTimingAdvice = "两人快靠拢时提前按 Q 回身横扫，留意对手起手。";

    private async Task CombatAreaChecks()
    {
        var session = GetNode<GameSession>("/root/GameSession");
        var options = session.Options;
        if (OS.GetCmdlineUserArgs().Contains("--combat-font=32"))
            session.SetOptions(options with { SubtitleSize = 32, ReducedMotion = true }, false);

        const string prefix = "--test-save-root=";
        var root = OS.GetCmdlineUserArgs().First(a => a.StartsWith(prefix, StringComparison.Ordinal))[prefix.Length..];
        Require(root.StartsWith("res://test-output/", StringComparison.Ordinal) && !root.Contains(".."), "Unsafe area fixture save root");
        var catalog = CombatCatalog.Parse(Godot.FileAccess.GetFileAsString("res://content/combat/combat.json"));
        var progress = catalog.Waves.Take(3).Aggregate(CombatProgress.New(catalog), (p, wave) => p.Reward(wave.Id, catalog));
        var repository = new CombatRepository(ProjectSettings.GlobalizePath(root + "/combat-v1"), catalog);
        Require(repository.Save(progress).Success, "Could not seed fourth-wave progress");

        var demo = GD.Load<PackedScene>("res://scenes/combat/CombatDemo.tscn").Instantiate<CombatDemo>();
        AddChild(demo);
        await Frames(3);
        var preparationHint = demo.GetNode<Label>("Prepare/Panel/Scroll/Content/WaveHint");
        Require(preparationHint.IsVisibleInTree() && preparationHint.Text.Contains(AreaTimingAdvice, StringComparison.Ordinal) &&
            preparationHint.Text.Contains("J 有效命中偶尔暴击。", StringComparison.Ordinal) &&
            preparationHint.Text.Contains("横移让快拳手挡在前，游走手会停步绕侧。", StringComparison.Ordinal),
            "Fourth-wave preparation must teach when to anticipate with Q");
        Require(preparationHint.GetLineCount() == preparationHint.GetVisibleLineCount(), "Fourth-wave preparation hint was clipped");
        await Capture("area-timing-prepare");
        var start = demo.GetNode<Button>("Prepare/Footer/Content/Actions/Start");
        var action = demo.GetNode<Label>("Hud/Top/Content/Action");
        var selectPose = typeof(CombatArena).GetMethod("FootworkPose", BindingFlags.Static | BindingFlags.NonPublic)
            ?.CreateDelegate<Func<CombatFighter, int?>>()
            ?? throw new InvalidOperationException("Footwork pose selector missing");

        async Task Until(Func<bool> reached, string phase)
        {
            for (var i = 0; i < 80 && !reached(); i++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                if (demo.Screen == "pause" && session.ApplicationFocused) demo.ResumeCombat();
            }
            Require(reached(), "Q did not reach " + phase);
        }

        for (var targets = 0; targets <= 2; targets++)
        {
            start.GrabFocus();
            KeyPress(Key.Enter);
            await Frames(3);
            Require(demo.Screen == "fight" && demo.Match?.Wave.Id == "b2", "Fourth wave did not start with the saved progress");
            var match = demo.Match!;
            Require(action.Text.Contains("Q就绪", StringComparison.Ordinal), "Fourth wave HUD did not show a ready skill");
            if (targets == 0)
            {
                var hint = demo.GetNode<Label>("Hud/Wave");
                Require(hint.IsVisibleInTree() && hint.Text == $"{match.Wave.Name}\n{preparationHint.Text}",
                    "Fourth-wave HUD and preparation must show the same timing advice");
                Require(hint.GetLineCount() == hint.GetVisibleLineCount() &&
                    demo.GetGlobalRect().Encloses(hint.GetGlobalRect()) &&
                    !hint.GetGlobalRect().Intersects(demo.GetNode<Control>("Hud/Top").GetGlobalRect()),
                    "Fourth-wave HUD timing hint must fit beside the combat HUD");
                await Capture("area-timing-hud");
            }
            for (var i = 0; i < match.Enemies.Count; i++)
            {
                var e = match.Enemies[i];
                e.X = match.Player.X + (i >= targets ? 300 : i == 0 ? -42 : 41);
                e.Y = match.Player.Y + (i == 0 ? -4 : 6);
                e.Cooldown = 900;
            }

            KeyPress(Key.Q);
            await Until(() => match.Player.Attack?.IsAreaSkill == true, "startup");
            Require(match.Player.Attack?.IsAreaSkill == true && match.Player.Stamina == 75, "Q did not start a paid area skill");
            Require(match.Player.AttackFrame <= 1 && selectPose(match.Player) == 7,
                "Q must lower its stance as soon as the sweep starts");
            var move = match.Player.Attack!;
            await Until(() => match.Player.AttackFrame == move.Startup / 2, "raised knee");
            Require(selectPose(match.Player) == 3 && match.Statistics.AreaHits == 0,
                "Q must show the raised knee before the first hit");
            Require(action.Text.Contains("Q冷却", StringComparison.Ordinal), "The skill cooldown was not displayed");
            if (targets == 2) await Capture("area-range");
            await Until(() => match.Player.AttackFrame == move.Startup, "first active frame");
            Require(selectPose(match.Player) == 6 && match.Statistics.AreaHits == targets,
                "Q must extend the sweeping leg on the same frame as the hit check");
            if (targets == 2) await Capture("area-double-hit");
            await Until(() => match.Player.AttackFrame == move.Startup + move.Active, "recovery");
            Require(selectPose(match.Player) == 7, "Q must pull the leg back into its low stance after contact");
            if (targets == 2) await Capture("area-recovery");
            await Until(() => match.Player.CanAct, "end of recovery");
            Require(match.Statistics.AreaHits == targets, "Unexpected number of Q hits");
            KeyPress(Key.Q);
            await Frames(3);
            Require(match.Statistics.AreaCasts == 1, "Q bypassed cooldown via keyboard");
            if (targets == 2)
            {
                // End the fixture after the real Q hit so the result screen follows its normal path.
                match.Player.ReceiveHit(match.Enemies[0], demo.Catalog.Move("light1") with { Power = 100 });
                await Until(() => demo.Screen == "result", "result");
                var panel = "Result/Panel/Scroll/Content/";
                var statistics = demo.GetNode<Label>(panel + "Statistics");
                Require(statistics.Text.Contains("横扫双中次数 1", StringComparison.Ordinal) &&
                    statistics.Text.Contains("范围伤害 27", StringComparison.Ordinal),
                    "Fourth-wave results must show the actual Q double hits and damage");
                Require(statistics.GetLineCount() == statistics.GetVisibleLineCount(), "Q statistics were clipped");
                await Capture("area-result");
                demo.GetNode<Button>(panel + "Export").EmitSignal(Button.SignalName.Pressed);
                var export = System.IO.Directory.GetFiles(ProjectSettings.GlobalizePath(root + "/combat-v1/exports"), "combat-*.json").Single();
                using var json = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(export));
                var totals = json.RootElement.GetProperty("statistics");
                Require(totals.GetProperty("AreaDoubleHits").GetInt32() == 1 &&
                    totals.GetProperty("AreaDamage").GetInt32() == 27, "Q export differs from the result screen");
            }
            demo.ReturnToPreparation();
            await Frames(3);
        }

        foreach (var moveInDepth in new[] { false, true })
        {
            start.GrabFocus();
            KeyPress(Key.Enter);
            await Frames(3);
            Require(demo.Screen == "fight" && demo.Match?.Wave.LaneBlocking == true, "Borrow-position fixture did not start b2");
            var lane = demo.Match!;
            var roamer = lane.Enemies[0];
            var quick = lane.Enemies[1];
            lane.Player.X = 500;
            lane.Player.Y = 440;
            roamer.X = 544; roamer.Y = 478;
            quick.X = 528; quick.Y = 492;
            roamer.Cooldown = quick.Cooldown = 900;
            if (moveInDepth)
            {
                Input.ParseInputEvent(new InputEventKey { Keycode = Key.S, PhysicalKeycode = Key.S, Pressed = true });
                await Frames(8);
                Input.ParseInputEvent(new InputEventKey { Keycode = Key.S, PhysicalKeycode = Key.S, Pressed = false });
                Require(lane.Player.Y > 440, "Depth movement did not alter the line of approach");
                await Until(() => lane.IsSidestepping(roamer), "borrow-position sidestep");
                var detourX = roamer.X;
                var detourY = roamer.Y;
                await Frames(3);
                Require(roamer.X == detourX && Math.Abs(roamer.Y - detourY) > 0, "Roamer did not move laterally after yielding");
                await Capture("borrow-position-sidestep");
            }
            else
            {
                for (var frame = 0; frame < 40; frame++)
                {
                    await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                    Require(!lane.IsSidestepping(roamer), "Stationary player should leave this lane open");
                }
            }
            demo.ReturnToPreparation();
            await Frames(3);
        }

        if (captureDirectory != null)
        {
            var index = demo.Catalog.Waves.FindIndex(w => w.Id == "b2");
            var configured = demo.Catalog.Waves[index];
            demo.Catalog.Waves[index] = configured with { CriticalChance = 1 };
            start.GrabFocus();
            KeyPress(Key.Enter);
            await Frames(3);
            Require(demo.Screen == "fight", "Critical capture could not start");
            var match = demo.Match!;
            var enemy = match.Enemies[0];
            var before = enemy.Health;
            enemy.X = match.Player.X + 28;
            enemy.Y = match.Player.Y;
            enemy.Cooldown = 900;
            KeyPress(Key.J);
            for (var i = 0; i < 20 && match.Statistics.CriticalHits == 0; i++)
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            Require(match.Statistics.CriticalHits == 1 && before - enemy.Health == 18, "Visible J hit did not apply critical damage");
            await Capture("critical-hit");
            demo.Catalog.Waves[index] = configured;
        }

        demo.Free();
        await Frames(3);
        session.SetOptions(options, false);
        GD.Print("COMBAT_AREA_PASS FourthWave KeyboardQ ZeroOneTwo Cooldown StartupContactRecoveryPoses");
    }
}
