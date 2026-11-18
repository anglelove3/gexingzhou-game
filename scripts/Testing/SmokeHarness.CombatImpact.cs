using Godot;
using GeXingzhou.Domain;

public partial class SmokeHarness
{
    private async Task CombatImpactChecks()
    {
        var scene = GD.Load<PackedScene>("res://scenes/combat/CombatDemo.tscn");
        var session = GetNode<GameSession>("/root/GameSession"); var options = session.Options;
        if (OS.GetCmdlineUserArgs().Contains("--combat-font=32")) session.SetOptions(options with { SubtitleSize = 32 }, false);
        foreach (var reduced in new[] { false, true })
        {
            session.SetOptions(session.Options with { ReducedMotion = reduced }, false);
            var demo = scene.Instantiate<CombatDemo>(); AddChild(demo); await Frames(4);
            var waveSelect = demo.GetNode<OptionButton>("Prepare/Panel/Scroll/Content/Wave");
            waveSelect.Select(0); waveSelect.EmitSignal(OptionButton.SignalName.ItemSelected, 0L);
            Require(demo.StartWave(), "Impact fixture cannot start");
            var match = demo.Match!; var enemy = match.Enemies[0]; enemy.X = match.Player.X + 30; enemy.Y = match.Player.Y;
            match.Player.TryAttack(demo.Catalog.Move("light1") with { Power = 100 });
            for (int i = 0; i < 20 && match.Result == CombatResult.Running; i++) await Frames(1);
            Require(match.Result == CombatResult.Won, "Impact fixture failed to end");
            Require(demo.Screen == "finish" && !demo.GetNode<Control>("Result").Visible, "Final hit obscured by immediate result");
            var directory = (string)DiagnosticField(demo, "saveDirectory")!;
            Require(new CombatRepository(directory, demo.Catalog).Load().Progress!.Completed.Contains("a1"), "Reward not persisted during finish");
            var clock = match.TickCount;
            demo._Notification((int)Control.NotificationApplicationFocusOut); await Frames(25);
            Require(demo.Screen == "finish" && match.TickCount == clock, "Unfocused finish advanced");
            demo._Notification((int)Control.NotificationApplicationFocusIn); await Frames(reduced ? 8 : 15);
            Require(demo.Screen == "finish", "Finish duration too short");
            await Frames(12); Require(demo.Screen == "result" && match.TickCount == clock, "Finish never resolved or advanced model");
            Require(demo.GetViewport().GuiGetFocusOwner() is Button b && b.GetParent().Name == "Content", "Result focus not restored");
            demo.Free(); await Frames(3);
        }
        var failed = scene.Instantiate<CombatDemo>(); AddChild(failed); await Frames(3);
        // A new controller reads the immediately committed reward even if the old one closes during finish.
        Require(failed.Progress.Completed.Contains("a1"), "Immediate reward was not retained on reopen");
        var next = failed.GetNode<OptionButton>("Prepare/Panel/Scroll/Content/Wave");
        next.Select(1); next.EmitSignal(OptionButton.SignalName.ItemSelected, 1L);
        Require(failed.StartWave(), "Save failure fixture cannot start");
        var m = failed.Match!; foreach (var e in m.Enemies) { e.X = m.Player.X + 30; e.Y = m.Player.Y; }
        var root = (string)DiagnosticField(failed, "saveDirectory")!;
        using (var lease = new System.IO.FileStream(System.IO.Path.Combine(root, "write.lock"), System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None))
        {
            m.Player.TryAttack(failed.Catalog.Move("light1") with { Power = 100 });
            for (int i = 0; i < 20 && m.Result == CombatResult.Running; i++) await Frames(1);
            Require(failed.Screen == "result" && failed.GetNode<Button>("Result/Panel/Scroll/Content/Retry").Disabled,
                "Save failure hidden behind finish screen=" + failed.Screen + " result=" + m.Result);
        }
        failed.GetNode<Button>("Result/Panel/Scroll/Content/Continue").EmitSignal(Button.SignalName.Pressed); await Frames(3);
        Require(failed.Screen == "prepare", "Failed reward retry lost candidate");
        var completed = failed.Progress;
        foreach (var w in failed.Catalog.Waves) completed = completed.Reward(w.Id, failed.Catalog);
        Require(new CombatRepository(root, failed.Catalog).Save(completed).Success, "Challenge setup save failed");
        failed.Free(); await Frames(3);
        var challengeDemo = scene.Instantiate<CombatDemo>(); AddChild(challengeDemo); await Frames(3);
        Require(challengeDemo.StartChallenge(), "Challenge impact fixture failed");
        var cm = challengeDemo.Match!; cm.Enemies[0].X = cm.Player.X + 30; cm.Enemies[0].Y = cm.Player.Y;
        cm.Player.TryAttack(challengeDemo.Catalog.Move("light1") with { Power = 100 });
        for (int i = 0; i < 20 && cm.Result == CombatResult.Running; i++) await Frames(1);
        Require(challengeDemo.Screen == "finish" && challengeDemo.ChallengeRecord!.CompletedWaves == 1, "Challenge result not committed before presentation");
        var attempt = challengeDemo.ChallengeRecord!.AttemptId;
        var stored = new CombatChallengeRepository(System.IO.Path.Combine(root, "challenges-v1")).LoadHistory().Records.Single(r => r.AttemptId == attempt);
        Require(stored.CompletedWaves == 1, "Challenge result not on disk during finish");
        challengeDemo.Free(); await Frames(3);
        var reopened = scene.Instantiate<CombatDemo>(); AddChild(reopened); await Frames(3);
        Require(reopened.Progress.CompletedRun, "Close during finish changed ordinary progress");
        stored = new CombatChallengeRepository(System.IO.Path.Combine(root, "challenges-v1")).LoadHistory().Records.Single(r => r.AttemptId == attempt);
        Require(stored.CompletedWaves == 1, "Close during finish lost challenge result");
        await CombatFeedbackChecks(reopened);
        reopened.Free(); await Frames(3); session.SetOptions(options, false);
        GD.Print("COMBAT_IMPACT_PASS ImmediateReward FinishDelay FocusFreeze ReducedMotion SaveRetry Reopen ChallengeImmediate CloseDuringFinish");
    }

    private async Task CombatFeedbackChecks(CombatDemo demo)
    {
        var session = GetNode<GameSession>("/root/GameSession");
        session.SetOptions(session.Options with { ReducedMotion = false }, false);
        var arena = demo.GetNode<CombatArena>("Arena"); var audio = demo.GetNode<AudioDirector>("Audio");
        var select = demo.GetNode<OptionButton>("Prepare/Panel/Scroll/Content/Wave");
        select.Select(4); select.EmitSignal(OptionButton.SignalName.ItemSelected, 4L);
        foreach (var reverse in new[] { false, true })
        {
            Require(demo.StartWave(), "Multi-target feedback fixture failed");
            var mixed = demo.Match!;
            foreach (var e in mixed.Enemies) { e.X = mixed.Player.X + 30; e.Y = mixed.Player.Y; }
            if (reverse) mixed.Enemies.Reverse();
            audio.StopTransient(); mixed.Player.TryAttack(demo.Catalog.Move("heavy"));
            for (int i = 0; i < 40 && !mixed.HitStopped; i++) await Frames(1);
            await Frames(2);
            Require(audio.GetNode<AudioStreamPlayer>("Effects/Slot1").Stream == audio.CombatHitStream,
                "Blocked target suppressed real hit audio reverse=" + reverse);
            Require(arena.CameraOffset.Length() > .01, "Blocked target suppressed real hit camera reverse=" + reverse);
            demo.ReturnToPreparation(); await Frames(3);
        }
        select.Select(5); select.EmitSignal(OptionButton.SignalName.ItemSelected, 5L);
        foreach (var id in new[] { "light1", "light2", "light3", "heavy", "charged" })
        {
            Require(demo.StartWave(), "Feedback fixture cannot start " + id);
            var m = demo.Match!; foreach (var e in m.Enemies) { e.X = m.Player.X + 30; e.Y = m.Player.Y; e.SetGuard(false); }
            m.Player.TryAttack(demo.Catalog.EquippedMove(id, m.Player.Stats));
            for (int i = 0; i < 40 && !m.HitStopped; i++) await Frames(1);
            Require(m.HitStopped, "Feedback fixture missed " + id);
            await Capture("impact-" + id);
            if (id == "light1")
            {
                demo.PauseCombat(); await Frames(1);
                var impacts = (System.Collections.IList)DiagnosticField(arena, "impacts")!;
                var effect = impacts[0]!; var ageField = effect.GetType().GetField("Age")!; var age = (double)ageField.GetValue(effect)!;
                await Frames(20); Require((double)ageField.GetValue(effect)! == age, "Paused impact aged");
                Require(arena.CameraOffset == Vector2.Zero, "Paused camera shake remained");
                if (DisplayServer.GetName() != "headless")
                {
                    RenderingServer.ForceDraw(); var before = GetViewport().GetTexture().GetImage().GetData();
                    session.SetOptions(session.Options with { ReducedMotion = true }, false); await Frames(3);
                    RenderingServer.ForceDraw(); var after = GetViewport().GetTexture().GetImage().GetData();
                    Require(!before.SequenceEqual(after), "Paused reduced motion retained cached impact drawing");
                    await Capture("impact-paused-reduced-motion");
                    session.SetOptions(session.Options with { ReducedMotion = false }, false); await Frames(2);
                }
                demo.ResumeCombat();
                for (int i = 0; i < 20 && m.Player.State != CombatState.Dodge; i++)
                {
                    Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.Space, Pressed = true });
                    Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.Space, Pressed = false });
                    await Frames(1);
                }
                Require(m.Statistics.DodgeCancels == 1, "Physical Space did not cancel confirmed light recovery");
                await Capture("impact-dodge-cancel");
            }
            if (id == "charged")
            {
                session.SetOptions(session.Options with { ReducedMotion = true }, false); await Frames(3);
                Require(arena.CameraOffset == Vector2.Zero, "Reduced motion kept camera offset");
                await Capture("impact-reduced-motion");
                session.SetOptions(session.Options with { ReducedMotion = false }, false);
            }
            demo.ReturnToPreparation(); await Frames(3);
            Require(((System.Collections.IList)DiagnosticField(arena, "impacts")!).Count == 0, "Previous wave effects leaked");
        }
        audio.StopTransient(); Require(audio.PlayImpactCue(AudioCue.CombatHit, .75f, .4f), "Layered hit audio failed");
        var slot = audio.GetNode<AudioStreamPlayer>("Effects/Slot1");
        Require(Math.Abs(slot.PitchScale - .75f) < .001, "Impact pitch ignored");
        var adjusted = session.Options with { MasterVolume = .5, EffectsVolume = .6 };
        audio.ApplySettings(adjusted); Require(Math.Abs(slot.VolumeLinear - .12f) < .001, "Settings discarded layer gain");
        audio.StopTransient(); Require(audio.PlayCue(AudioCue.CombatHit), "Default audio reuse failed");
        Require(slot.PitchScale == 1 && Math.Abs(slot.VolumeLinear - .3f) < .001, "Reused slot retained impact pitch/gain");
        audio.StopTransient(); audio.ApplySettings(session.Options);
        GD.Print("COMBAT_FEEDBACK_PASS FiveMoves KeyboardCancel PauseFreeze ReducedMotion ClearEffects AudioGain PitchReset");
    }
}
