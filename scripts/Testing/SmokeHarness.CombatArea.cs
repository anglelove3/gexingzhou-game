using Godot;
using GeXingzhou.Domain;
using System.Reflection;

public partial class SmokeHarness
{
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
        var start = demo.GetNode<Button>("Prepare/Footer/Content/Actions/Start");
        var action = demo.GetNode<Label>("Hud/Top/Content/Action");
        var selectPose = typeof(CombatArena).GetMethod("FootworkPose", BindingFlags.Static | BindingFlags.NonPublic)
            ?.CreateDelegate<Func<CombatFighter, int?>>()
            ?? throw new InvalidOperationException("Footwork pose selector missing");

        async Task Until(Func<bool> reached, string phase)
        {
            for (var i = 0; i < 80 && !reached(); i++)
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
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
