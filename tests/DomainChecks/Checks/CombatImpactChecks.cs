using GeXingzhou.Domain;
using System.Text.Json.Nodes;

namespace GeXingzhou.DomainChecks;

public static class CombatImpactChecks
{
    private static CombatMatch Match(string wave = "b1", bool block = false, double distance = 30)
    {
        var c = CombatChecks.Catalog(); var p = CombatProgress.New(c);
        foreach (var w in c.Waves) p = p.Reward(w.Id, c);
        var m = new CombatMatch(c, p, wave);
        foreach (var e in m.Enemies) { e.X = m.Player.X + distance; e.Y = m.Player.Y; e.SetGuard(block); }
        return m;
    }

    private static void Until(CombatMatch m, Func<bool> done)
    {
        for (int i = 0; i < 150 && !done(); i++) m.Step(default);
        Check.True(done());
    }

    public static void Register(List<(string, string, Action)> tests)
    {
        tests.Add(("CombatImpact", "DodgeCooldownDoesNotReportEmptyStamina", () =>
        {
            var m = Match(); Check.True(m.Player.TryDodge(0, 1));
            while (!m.Player.CanAct) m.Player.Tick();
            foreach (var e in m.Enemies) { e.X = m.Player.X + 30; e.Y = m.Player.Y; e.SetGuard(false); }
            var move = m.Catalog.Move("light1"); m.Player.TryAttack(move);
            Until(m, () => !m.HitStopped && m.Player.AttackFrame == move.Startup + move.Active + 3);
            Check.True(m.Player.Stamina > 24); var before = m.Player.Stamina;
            m.Step(new CombatInput(DodgePressed: true));
            Check.Equal(CombatState.Attack, m.Player.State); Check.Equal(before, m.Player.Stamina);
            Check.Equal(0, m.Statistics.EmptyStamina);
        }));
        tests.Add(("CombatImpact", "EnemyHeavyBlockUsesCurrentOrLegacyStop", () =>
        {
            foreach (var legacy in new[] { false, true })
            foreach (var guard in new[] { false, true })
            {
                var data = JsonNode.Parse(File.ReadAllText("content/combat/combat.json"))!;
                if (legacy) data.AsObject().Remove("impactFeedback");
                var c = CombatCatalog.Parse(data.ToJsonString()); var p = CombatProgress.New(c);
                foreach (var w in c.Waves) p = p.Reward(w.Id, c);
                var m = new CombatMatch(c, p, "b1"); var e = m.Enemies[0];
                e.X = m.Player.X + 30; e.Y = m.Player.Y; e.SetGuard(false);
                for (int i = 0; i < 100 && !m.HitStopped; i++) m.Step(new CombatInput(GuardHeld: guard));
                Check.True(m.HitStopped); Check.Equal(guard ? 1 : 0, m.Statistics.Blocks);
                int held = 0; while (m.HitStopped && held < 20) { m.Step(default); held++; }
                Check.Equal(guard && !legacy ? 2 : 4, held);
            }
        }));
        tests.Add(("CombatImpact", "HitstopSeparatesLightAndHeavyAndFreezesModel", () =>
        {
            foreach (var (id, expected) in new[] { ("light1", 2), ("light2", 3), ("light3", 4), ("heavy", 5), ("charged", 7) })
            {
                var m = Match(); m.Player.TryAttack(m.Catalog.EquippedMove(id, m.Player.Stats));
                Until(m, () => m.HitStopped); int clock = m.TickCount, total = m.Statistics.TotalFrames, held = 0;
                while (m.HitStopped && held < 20) { m.Step(default); held++; Check.Equal(clock, m.TickCount); }
                Check.Equal(expected, held); Check.Equal(total + expected, m.Statistics.TotalFrames);
            }
        }));
        tests.Add(("CombatImpact", "ConfirmedLightRecoveryCancelsWithNormalCost", () =>
        {
            foreach (var id in new[] { "light1", "light2", "light3" })
            {
                var m = Match("b2"); var move = m.Catalog.EquippedMove(id, m.Player.Stats);
                m.Player.TryAttack(move);
                Until(m, () => !m.HitStopped && m.Player.AttackFrame == move.Startup + move.Active + 3);
                var stamina = m.Player.Stamina;
                m.Step(new CombatInput(MoveX: -1, DodgePressed: true));
                Check.Equal(CombatState.Dodge, m.Player.State); Check.Equal(stamina - 24, m.Player.Stamina);
                Check.Equal(1, m.Statistics.Dodges);
                m.Step(default); Check.Equal(stamina - 24, m.Player.Stamina);
            }
        }));
        tests.Add(("CombatImpact", "RejectEarlyMissBlockHeavyAndLowStaminaCancel", () =>
        {
            foreach (var condition in new[] { "early", "miss", "block", "heavy", "tired" })
            {
                var m = Match(block: condition == "block", distance: condition == "miss" ? 250 : 30);
                var move = m.Catalog.EquippedMove(condition == "heavy" ? "heavy" : "light1", m.Player.Stats);
                if (condition == "tired") move = move with { Cost = m.Player.Stats.Stamina - 23 };
                m.Player.TryAttack(move);
                int frame = move.Startup + move.Active + (condition == "early" ? 2 : 3);
                Until(m, () => !m.HitStopped && m.Player.AttackFrame == frame);
                var stamina = m.Player.Stamina; m.Step(new CombatInput(DodgePressed: true));
                Check.Equal(CombatState.Attack, m.Player.State); Check.Equal(stamina, m.Player.Stamina);
                Check.Equal(0, m.Statistics.Dodges);
            }
        }));
        tests.Add(("CombatImpact", "HitstopBufferedDodgeWaitsForRecoveryGate", () =>
        {
            var m = Match(); var move = m.Catalog.EquippedMove("light1", m.Player.Stats) with { Active = 1 };
            m.Player.TryAttack(move); Until(m, () => m.HitStopped);
            m.Step(new CombatInput(DodgePressed: true));
            Check.Equal(CombatState.Attack, m.Player.State);
            Until(m, () => m.Player.State == CombatState.Dodge);
            Check.Equal(1, m.Statistics.Dodges);
        }));
        tests.Add(("CombatImpact", "GuardBreakAllowsObservedReactionButExpires", () =>
        {
            foreach (var delay in new[] { 0, 1, 12, 24, 25, 35 })
            {
                var c = CombatChecks.Catalog(); var p = CombatProgress.New(c);
                foreach (var w in c.Waves) p = p.Reward(w.Id, c);
                p = p.Equip("stick", c); var m = new CombatMatch(c, p, "b1");
                m.Enemies[0].X = m.Player.X + 30; m.Enemies[0].Y = m.Player.Y;
                for (int frame = 0; frame < 110; frame++)
                    m.Step(new CombatInput(HeavyPressed: frame == delay, HeavyHeld: frame >= delay && frame < delay + 27, HeavyReleased: frame == delay + 27));
                Check.Equal(delay <= 24 ? 1 : 0, m.Statistics.BreakHits);
            }
        }));
        tests.Add(("CombatImpact", "LegacySnapshotKeepsOldHitstopAndGuardWindow", () =>
        {
            var data = JsonNode.Parse(File.ReadAllText("content/combat/combat.json"))!;
            foreach (var move in data["moves"]!.AsArray())
                foreach (var field in new[] { "hitstopFrames", "dodgeCancelRecoveryFrames" }) move!.AsObject().Remove(field);
            foreach (var e in data["enemies"]!.AsArray()) e!.AsObject().Remove("guardFrames");
            var c = CombatCatalog.Parse(data.ToJsonString()); var p = CombatProgress.New(c);
            foreach (var w in c.Waves) p = p.Reward(w.Id, c);
            var m = new CombatMatch(c, p, "b1"); m.Enemies[0].X = m.Player.X + 30; m.Enemies[0].Y = m.Player.Y;
            m.Enemies[0].SetGuard(false); var move1 = c.Move("light2"); m.Player.TryAttack(move1);
            Until(m, () => m.HitStopped); int stop = 0;
            while (m.HitStopped) { m.Step(default); stop++; }
            Check.Equal(2, stop);
            Until(m, () => m.Player.AttackFrame == move1.Startup + move1.Active + 3);
            m.Step(new CombatInput(DodgePressed: true)); Check.Equal(CombatState.Attack, m.Player.State);
            m = new CombatMatch(c, p.Equip("stick", c), "b1"); m.Enemies[0].X = m.Player.X + 30; m.Enemies[0].Y = m.Player.Y;
            for (int frame = 0; frame < 90; frame++) m.Step(new CombatInput(HeavyPressed: frame == 12, HeavyHeld: frame >= 12 && frame < 39, HeavyReleased: frame == 39));
            Check.Equal(0, m.Statistics.BreakHits);
            m = new CombatMatch(c, p, "b1"); m.Enemies[0].X = m.Player.X + 30; m.Enemies[0].Y = m.Player.Y;
            m.Player.TryAttack(c.Move("heavy")); Until(m, () => m.HitStopped); stop = 0;
            while (m.HitStopped) { m.Step(default); stop++; } Check.Equal(4, stop);
        }));
        tests.Add(("CombatImpact", "CancelConfirmationDoesNotLeakToNextAttack", () =>
        {
            var m = Match(); var move = m.Catalog.Move("light1"); m.Player.TryAttack(move);
            Until(m, () => m.Player.CanAct);
            m.Enemies[0].X = m.Player.X + 250;
            m.Player.TryAttack(move);
            Until(m, () => m.Player.AttackFrame == move.Startup + move.Active + 3);
            m.Step(new CombatInput(DodgePressed: true)); Check.Equal(CombatState.Attack, m.Player.State);
        }));
        tests.Add(("CombatImpact", "ImpactConfigRejectsUnboundedStopAndGuard", () =>
        {
            foreach (var (section, field, value) in new[] { ("moves", "hitstopFrames", 13), ("moves", "dodgeCancelRecoveryFrames", -1), ("enemies", "guardFrames", 0) })
            {
                var data = JsonNode.Parse(File.ReadAllText("content/combat/combat.json"))!;
                data[section]![0]![field] = value;
                Check.Throws<ArgumentException>(() => CombatCatalog.Parse(data.ToJsonString()));
            }
        }));
    }
}
