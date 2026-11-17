using GeXingzhou.Domain;

namespace GeXingzhou.DomainChecks;

public static class CombatBalanceChecks
{
    public static void Register(List<(string, string, Action)> tests)
    {
        tests.Add(("CombatBalance", "ControlledSampleIsDeterministic", () =>
        {
            var c = CombatChecks.Catalog(); var p = CombatChallengeChecks.Complete(c);
            var a = CombatBalance.Sample(c, p, 1, 0, true);
            var b = CombatBalance.Sample(c, p, 1, 0, true);
            Check.Equal("Won", a.Result); Check.Equal(6, a.CompletedWaves);
            Check.Equal(a.TotalSeconds, b.TotalSeconds); Check.Equal(a.Health, b.Health);
            Check.Equal(c.ConfigHash, a.ConfigHash);
            Check.Equal(a.Waves[0].HealthEnd, a.Waves[1].HealthStart);
        }));
        tests.Add(("CombatBalance", "BaselineAndEquipmentConditions", () =>
        {
            var c = CombatChecks.Catalog(); var baseline = CombatBalance.Baseline(c);
            Check.True(c.ConfigHash != baseline.ConfigHash);
            Check.Equal(0, baseline.Item("gloves").ComboStaminaRestore);
            Check.Equal(0, baseline.Item("wraps").PerfectGuardFrames);
            Check.Equal(0, baseline.Item("stick").ChargedExtraCost);
            Check.Equal(1d, baseline.Item("stick").GuardBreakPower);
            Check.Equal(27, CombatBalance.Loadouts(c).Count);
        }));
        tests.Add(("CombatBalance", "PolicyClosesToActualWeaponReach", () =>
        {
            var c = CombatChecks.Catalog(); var p = CombatChallengeChecks.Complete(c).Equip("gloves", c);
            var match = new CombatMatch(c, p, "a1");
            match.Enemies[0].X = match.Player.X + c.EquippedMove("light1", match.Player.Stats).Reach + 2;
            match.Enemies[0].Y = match.Player.Y;
            var input = CombatAutomation.Decide(match);
            Check.True(!input.LightPressed && input.MoveX > 0);
        }));
    }
}
