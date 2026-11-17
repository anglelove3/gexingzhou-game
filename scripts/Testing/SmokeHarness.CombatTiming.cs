using Godot;

public partial class SmokeHarness
{
    private async Task CombatTimingChecks()
    {
        var originalRate = Engine.PhysicsTicksPerSecond;
        var scene = GD.Load<PackedScene>("res://scenes/combat/CombatDemo.tscn");
        try
        {
            foreach (var rate in new[] { 30, 60, 120 })
            {
                Engine.PhysicsTicksPerSecond = rate;
                var demo = scene.Instantiate<CombatDemo>(); AddChild(demo); await Frames(4);
                Require(demo.StartWave(), "Timing fixture failed");
                var match = demo.Match!; var before = match.TickCount; var x = match.Player.X;
                Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.A, Keycode = Key.A, Pressed = true });
                for (int i = 0; i < rate; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.A, Keycode = Key.A, Pressed = false });
                await Frames(2);
                Require(Math.Abs(match.TickCount - before - 60) <= 4, "Fixed combat clock drift at physics rate " + rate);
                Require(Math.Abs(match.Player.X - x + 180) <= 12, "Movement speed changed with physics rate " + rate);
                demo.PauseCombat(); before = match.TickCount;
                for (int i = 0; i < rate / 4; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                Require(match.TickCount == before, "Paused combat clock advanced at physics rate " + rate);
                demo.ResumeCombat(); var stoppedX = match.Player.X; await Frames(5);
                Require(match.Player.X == stoppedX, "Released input resumed after pause at physics rate " + rate);
                var player = match.Player; player.Facing = -1; player.SetGuard(true);
                var source = GeXingzhou.Domain.CombatFighter.FromEnemy("timing", demo.Catalog.Enemies[0], player.X - 30, player.Y);
                Require(player.ReceiveHit(source, demo.Catalog.Move("light1")).PerfectGuard, "Timing guard fixture failed");
                for (int i = 0; i < rate / 2; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                Require(Math.Abs(player.RiposteRemaining - 60) <= 4, "Riposte clock drift at physics rate " + rate);
                demo.PauseCombat(); var opportunity = player.RiposteRemaining;
                for (int i = 0; i < rate / 4; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                Require(player.RiposteRemaining == opportunity, "Paused riposte opportunity expired");
                demo.ResumeCombat();
                for (int i = 0; i < rate + rate / 4; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                Require(player.RiposteRemaining == 0, "Riposte opportunity failed to expire");
                demo.Free(); await Frames(3);
                GD.Print("COMBAT_TIMING_PASS physics=" + rate + " fixed_model=60 movement=180 pause=frozen riposte=timed_and_frozen");
            }
        }
        finally { Engine.PhysicsTicksPerSecond = originalRate; }
    }
}
