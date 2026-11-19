using GeXingzhou.Domain;
using System.Text.Json.Nodes;

namespace GeXingzhou.DomainChecks;

public static class CombatFootworkChecks
{
    private static CombatMatch Bout(CombatCatalog? catalog = null)
    {
        var c = catalog ?? CombatChecks.Catalog();
        var m = new CombatMatch(c, CombatProgress.New(c), "a1");
        var opponent = m.Enemies[0];
        opponent.X = m.Player.X + 30;
        opponent.Y = m.Player.Y;
        opponent.Cooldown = 900;
        return m;
    }

    private static void Until(CombatMatch match, Func<bool> condition)
    {
        for (var i = 0; i < 80 && !condition(); i++) match.Step(default);
        Check.True(condition());
    }

    private static void SecondImpact(CombatMatch m)
    {
        Check.True(m.Player.TryAttack(m.Catalog.Move("light2")));
        Until(m, () => m.HitStopped);
    }

    public static void Register(List<(string, string, Action)> tests)
    {
        tests.Add(("CombatFootwork", "InputsLinkAllThreeStrikesBeforeFullRecovery", () =>
        {
            var m = Bout();
            m.Step(new CombatInput(LightPressed: true));
            foreach (var id in new[] { "light2", "light3" })
            {
                Until(m, () => m.HitStopped);
                var clock = m.TickCount;
                m.Step(new CombatInput(LightPressed: true));
                Until(m, () => m.Player.Attack?.Id == id && m.Player.State == CombatState.Attack);
                Check.True(m.TickCount - clock <= 8);
            }
            Check.Equal(3, m.Statistics.LightAttacks);
            Check.Equal(58d, m.Player.Stamina);
            Until(m, () => m.HitStopped);
            Check.Equal(24d, m.Events.Single(e => e.MoveId == "light3").Displacement);
        }));
        tests.Add(("CombatFootwork", "HeavyPressedInHitstopLeadsToPaidSweepAndDown", () =>
        {
            var m = Bout(); SecondImpact(m);
            var stamina = m.Player.Stamina;
            m.Step(new CombatInput(HeavyPressed: true));
            Until(m, () => m.Player.Attack?.Id == "sweep");
            Check.Equal(stamina - 24, m.Player.Stamina);
            Until(m, () => m.Enemies[0].State == CombatState.Down);
            Check.Equal("sweep", m.Events.Single(e => e.Kind == "down").MoveId);
            Until(m, () => m.Enemies[0].State == CombatState.Rising);
            Check.Equal(0, m.Statistics.BreakHits);
        }));
        tests.Add(("CombatFootwork", "KBuffersInTheLastFourStartupFrames", () =>
        {
            for (var lead = 4; lead >= 1; lead--)
            {
                var m = Bout();
                var move = m.Catalog.Move("light2");
                Check.True(m.Player.TryAttack(move));
                Until(m, () => m.Player.AttackFrame == move.Startup - lead);
                var stamina = m.Player.Stamina;
                m.Step(new CombatInput(HeavyPressed: true));
                Check.Equal(stamina, m.Player.Stamina);
                Check.Equal(0, m.Statistics.HeavyAttacks);
                Until(m, () => m.Player.Attack?.Id == "sweep");
                Check.Equal(stamina - 24, m.Player.Stamina);
                Until(m, () => m.Enemies[0].State == CombatState.Down);
                Check.Equal(1, m.Statistics.HeavyAttacks);
            }
        }));
        tests.Add(("CombatFootwork", "EarlyKRequiresTimelyHitAndSurvivingInput", () =>
        {
            foreach (var condition in new[] { "before-window", "block", "miss", "pause", "tired", "interrupted", "dodge" })
            {
                var m = Bout();
                var move = m.Catalog.Move("light2");
                if (condition == "block") m.Enemies[0].SetGuard(true);
                if (condition == "miss") m.Enemies[0].X += 300;
                if (condition == "tired") move = move with { Cost = 77 };
                Check.True(m.Player.TryAttack(move));
                var lead = condition == "before-window" ? 5 : 4;
                Until(m, () => m.Player.AttackFrame == move.Startup - lead);
                if (condition == "interrupted")
                    Check.True(m.Enemies[0].TryAttack(m.Catalog.Move("heavy") with { Startup = 1 }));
                m.Step(new CombatInput(HeavyPressed: true, DodgePressed: condition == "dodge"));
                if (condition == "interrupted") Check.Equal(CombatState.Hurt, m.Player.State);
                if (condition == "pause") m.SuspendInput();
                Until(m, () => m.Player.CanAct);
                Check.Equal(0, m.Statistics.HeavyAttacks);
                m.Enemies[0].SetGuard(false);
                m.Enemies[0].X = m.Player.X + 30;
                Check.True(m.Player.TryAttack(m.Catalog.Move("light2")));
                Until(m, () => m.HitStopped);
                Until(m, () => m.Player.CanAct);
                Check.Equal(0, m.Statistics.HeavyAttacks);
            }
        }));
        tests.Add(("CombatFootwork", "EarlyKLeavesAffordableKickQueuedWhenSweepIsTooExpensive", () =>
        {
            var m = Bout();
            var move = m.Catalog.Move("light2") with { Cost = 77 };
            Check.True(m.Player.TryAttack(move));
            Until(m, () => m.Player.AttackFrame == move.Startup - 1);
            m.Step(new CombatInput(LightPressed: true, HeavyPressed: true));
            Until(m, () => m.Player.Attack?.Id == "light3");
            Check.Equal(5d, m.Player.Stamina);
            Check.Equal(0, m.Statistics.HeavyAttacks);
        }));
        tests.Add(("CombatFootwork", "EarlyKExpiresFourFramesAfterPressOnLateContact", () =>
        {
            foreach (var renew in new[] { false, true })
            {
                var m = Bout();
                var move = m.Catalog.Move("light2");
                m.Enemies[0].X += 300;
                Check.True(m.Player.TryAttack(move));
                Until(m, () => m.Player.AttackFrame == move.Startup - 4);
                m.Step(new CombatInput(HeavyPressed: true));
                Until(m, () => m.Player.AttackFrame == move.Startup);
                Check.True(!m.HitStopped);

                m.Enemies[0].X = m.Player.X + 30;
                m.Step(new CombatInput(HeavyPressed: renew));
                Check.True(m.HitStopped);
                if (renew) Until(m, () => m.Player.Attack?.Id == "sweep");
                else Until(m, () => m.Player.CanAct);
                Check.Equal(renew ? 1 : 0, m.Statistics.HeavyAttacks);
            }
        }));
        tests.Add(("CombatFootwork", "EarlyKRetainsJOnBlockAndAllowsARevisedChoice", () =>
        {
            foreach (var block in new[] { false, true })
            {
                var m = Bout();
                var move = m.Catalog.Move("light2");
                if (block) m.Enemies[0].SetGuard(true);
                Check.True(m.Player.TryAttack(move));
                if (!block)
                {
                    Until(m, () => m.Player.AttackFrame == move.Startup - 4);
                    m.Step(new CombatInput(HeavyPressed: true));
                }
                Until(m, () => m.Player.AttackFrame == move.Startup - 1);
                m.Step(new CombatInput(LightPressed: true, HeavyPressed: block));
                Until(m, () => m.Player.Attack?.Id == "light3");
                Check.Equal(0, m.Statistics.HeavyAttacks);
            }
        }));
        tests.Add(("CombatFootwork", "PreviousFootworkSnapshotRetainsItsOriginalWindow", () =>
        {
            var current = CombatChecks.Catalog();
            var oldData = JsonNode.Parse(current.SourceJson)!;
            oldData["balanceVersion"] = "combat-footwork-0.1";
            oldData["waves"]![0]!.AsObject().Remove("sweepEarlyBufferFrames");
            var old = CombatCatalog.Parse(oldData.ToJsonString());
            var progress = old.Waves.Aggregate(CombatProgress.New(old), (p, wave) => p.Reward(wave.Id, old));
            var oldRecord = CombatChallenge.Start(old, progress).Record;
            Check.Equal<string?>(null, oldRecord.Validate());
            Check.True(oldRecord.ConfigHash != current.ConfigHash);
            var m = Bout(old);
            var move = old.Move("light2");
            Check.True(m.Player.TryAttack(move));
            Until(m, () => m.Player.AttackFrame == move.Startup - 1);
            m.Step(new CombatInput(HeavyPressed: true));
            Until(m, () => m.HitStopped);
            Until(m, () => m.Player.CanAct);
            Check.Equal(0, m.Statistics.HeavyAttacks);
        }));
        tests.Add(("CombatFootwork", "SweepCannotFollowBlockMissPauseOrLowStamina", () =>
        {
            foreach (var condition in new[] { "block", "miss", "pause", "tired" })
            {
                var m = Bout(); var e = m.Enemies[0]; var move = m.Catalog.Move("light2");
                if (condition == "block") e.SetGuard(true);
                if (condition == "miss") e.X += 300;
                if (condition == "tired") move = move with { Cost = 77 };
                m.Player.TryAttack(move);
                Until(m, () => m.Player.AttackFrame >= move.Startup);
                m.Step(new CombatInput(HeavyPressed: true));
                if (condition == "pause") m.SuspendInput();
                for (var f = 0; f < 30; f++) m.Step(default);
                Check.True(m.Player.Attack?.Id != "sweep");
                Check.Equal(0, m.Statistics.HeavyAttacks);
            }
        }));
        tests.Add(("CombatFootwork", "OldSnapshotKeepsLegacyLinkAndStandaloneKStillCharges", () =>
        {
            var data = JsonNode.Parse(File.ReadAllText("content/combat/combat.json"))!;
            var oldWave = data["waves"]![0]!.AsObject();
            oldWave.Remove("footwork");
            oldWave.Remove("sweepEarlyBufferFrames");
            var moves = data["moves"]!.AsArray();
            var sweep = moves.FirstOrDefault(m => (string?)m!["id"] == "sweep");
            if (sweep != null) moves.Remove(sweep);
            var old = Bout(CombatCatalog.Parse(data.ToJsonString())); SecondImpact(old);
            old.Step(new CombatInput(HeavyPressed: true));
            for (var f = 0; f < 30; f++) old.Step(default);
            Check.True(old.Player.Attack?.Id != "sweep");
            var m = Bout();
            m.Step(new CombatInput(HeavyPressed: true, HeavyHeld: true));
            Check.Equal(CombatState.Charging, m.Player.State);
            for (var f = 0; f < 27; f++) m.Step(new CombatInput(HeavyHeld: true));
            m.Step(new CombatInput(HeavyReleased: true));
            Check.Equal("charged", m.Player.Attack?.Id);
        }));
    }
}
