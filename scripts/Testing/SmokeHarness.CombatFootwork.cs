using Godot;
using GeXingzhou.Domain;

public partial class SmokeHarness
{
    private async Task CombatFootworkChecks()
    {
        var session = GetNode<GameSession>("/root/GameSession");
        var options = session.Options;
        if (OS.GetCmdlineUserArgs().Contains("--combat-font=32"))
            session.SetOptions(options with { SubtitleSize = 32, ReducedMotion = true }, false);
        var scene = GD.Load<PackedScene>("res://scenes/combat/CombatDemo.tscn");
        var demo = scene.Instantiate<CombatDemo>();
        AddChild(demo);
        await Frames(4);

        async Task Until(Func<bool> predicate, string label)
        {
            for (var frame = 0; frame < 80 && !predicate(); frame++) await Frames();
            Require(predicate(), "Footwork did not reach " + label);
        }

        foreach (var sweep in new[] { false, true })
        {
            demo.GetNode<Button>("Prepare/Footer/Content/Actions/Start").EmitSignal(Button.SignalName.Pressed);
            Require(demo.Screen == "fight" && demo.Match?.Wave.Footwork == true, "First wave must open footwork through normal Start");
            var match = demo.Match!;
            var opponent = match.Enemies.Single();
            Require(opponent.Name == "快拳手", "First wave opponent changed");
            opponent.X = match.Player.X + 27; opponent.Y = match.Player.Y; opponent.Cooldown = 900;
            KeyPress(Key.J);
            await Until(() => match.Player.Attack?.Id == "light1" && match.HitStopped, "jab impact");
            KeyPress(Key.J);
            await Until(() => match.Player.Attack?.Id == "light2" && match.SweepCanBuffer, "turning punch and K prompt");
            if (!sweep) await Capture("footwork-hook");
            KeyPress(sweep ? Key.K : Key.J);
            var id = sweep ? "sweep" : "light3";
            await Until(() => match.Player.Attack?.Id == id && match.HitStopped, id + " impact");
            Require(match.Player.Stamina == (sweep ? 52 : 58), "Displayed branch deducted wrong stamina");
            Require(sweep ? opponent.State == CombatState.Down : opponent.State == CombatState.Hurt, "Branches must produce different target states");
            if (sweep) await Capture("footwork-sweep");
            else await Capture("footwork-kick");
            demo.ReturnToPreparation();
            await Frames(3);
        }
        demo.Free();
        await Frames(3);
        session.SetOptions(options, false);
        GD.Print("COMBAT_FOOTWORK_PASS NormalStart QuickOpponent KeyboardEarlyJ HitConfirmedK Stamina SeparateHitStates");
    }
}
