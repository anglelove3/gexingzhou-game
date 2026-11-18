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
            data["waves"]![0]!.AsObject().Remove("footwork");
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
