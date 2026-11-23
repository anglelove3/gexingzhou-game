using GeXingzhou.Domain;

namespace GeXingzhou.DomainChecks;

public static class CombatBuildChecks
{
    private static CombatFighter Fighter(CombatCatalog c, string weapon = "wraps", int attack = 14) =>
        new("player", "葛行舟", new(100, attack, 8, 100, 24, 180, .34, c.Item(weapon)), 400, 480);

    public static void Register(List<(string, string, Action)> tests)
    {
        tests.Add(("CombatBuilds", "ConfiguredAndLegacyDefaults", () =>
        {
            var c = CombatChecks.Catalog();
            Check.Equal("combat-borrow-position-0.1", c.BalanceVersion);
            Check.Equal(4, c.Wave("a1").SweepEarlyBufferFrames);
            Check.True(c.Waves.Where(w => w.Id != "a1").All(w => w.SweepEarlyBufferFrames == 0));
            Check.Equal(6, c.Item("gloves").ComboStaminaRestore);
            Check.Equal(39, c.EquippedMove("charged", Fighter(c, "stick").Stats).Cost);
            var legacy = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText("content/combat/combat.json"))!;
            legacy.AsObject().Remove("balanceVersion");
            foreach (var item in legacy["items"]!.AsArray())
                foreach (var field in new[] { "comboStaminaRestore", "guardBreakPower", "chargedExtraCost", "perfectGuardFrames", "riposteFrames", "riposteCooldown", "ripostePower" }) item!.AsObject().Remove(field);
            var old = CombatCatalog.Parse(legacy.ToJsonString());
            Check.Equal(0, old.Item("gloves").ComboStaminaRestore);
            Check.Equal(35, old.EquippedMove("charged", Fighter(old, "stick").Stats).Cost);
            Check.Equal(64, c.ConfigHash.Length);
        }));
        tests.Add(("CombatBuilds", "ThirdHitReturnsOnceAndCaps", () =>
        {
            var c = CombatChecks.Catalog(); var p = CombatProgress.New(c);
            p = p.Reward("a1", c).Reward("a2", c).Equip("gloves", c);
            var m = new CombatMatch(c, p, "b1");
            var e = m.Enemies[0]; e.X = m.Player.X + 30; e.Y = m.Player.Y; e.SetGuard(false);
            Check.True(m.Player.TryAttack(c.EquippedMove("light3", m.Player.Stats)));
            for (int i = 0; i < 18; i++) m.Step(default);
            Check.Equal(1, m.Statistics.StaminaReturns);
            Check.Equal(6d, m.Statistics.StaminaRestored);
            Check.Equal((double)m.Player.Stats.Stamina - 14 + 6, m.Player.Stamina);
            Check.Equal(0d, m.Player.RestoreStamina(0));
            m.Player.RestoreStamina(1000);
            Check.Equal(0d, m.Player.RestoreStamina(6));
        }));
        tests.Add(("CombatBuilds", "ChargedFrontGuardBonusAndCost", () =>
        {
            var c = CombatChecks.Catalog(); var p = Fighter(c, "stick", 18);
            var e = CombatFighter.FromEnemy("e", c.Enemies.Single(x => x.Id == "guard"), 440, 480);
            e.Facing = -1; e.SetGuard(true);
            var move = c.EquippedMove("charged", p.Stats);
            Check.True(p.TryAttack(move)); Check.Equal(61d, p.Stamina);
            var hit = e.ReceiveHit(p, move); Check.Equal(48, hit.Damage); Check.True(hit.BreakBonus);
            e = CombatFighter.FromEnemy("e2", c.Enemies.Single(x => x.Id == "guard"), 440, 480);
            e.Facing = 1; e.SetGuard(true);
            Check.Equal(40, e.ReceiveHit(p, move).Damage);
            foreach (var remaining in new[] {35, 38, 39})
            {
                var low = Fighter(c, "stick", 18);
                low.TryAttack(new CombatMove { Cost = 100 - remaining, Startup = 1, Active = 1, Recovery = 1 });
                for (int i = 0; i < 3; i++) low.Tick();
                Check.Equal(remaining == 39, low.TryAttack(move));
            }
        }));
        tests.Add(("CombatBuilds", "PerfectGuardWindowAndConsumption", () =>
        {
            var c = CombatChecks.Catalog(); var source = Fighter(c); source.X = 440;
            var jab = c.Move("light1");
            foreach (var frame in new[] {0, 5, 6})
            {
                var p = Fighter(c); p.SetGuard(true);
                for (int i = 0; i < frame; i++) p.Tick();
                var hit = p.ReceiveHit(source, jab);
                Check.Equal(frame < 6, hit.PerfectGuard);
                Check.Equal(frame < 6 ? 90 : 0, p.RiposteRemaining);
                Check.True(p.TryAttack(c.EquippedMove("light1", p.Stats)));
                Check.Equal(frame < 6, p.AttackHasRiposte);
                Check.Equal(frame < 6 ? 1d : .8d, p.Attack!.Power);
                Check.Equal(0, p.RiposteRemaining);
            }
        }));
        tests.Add(("CombatBuilds", "RiposteExpiryCooldownAndHurt", () =>
        {
            var c = CombatChecks.Catalog(); var source = Fighter(c); source.X = 440;
            var p = Fighter(c); p.SetGuard(true); p.ReceiveHit(source, c.Move("light1"));
            p.SetGuard(false); p.SetGuard(true);
            Check.True(!p.ReceiveHit(source, c.Move("light1")).PerfectGuard);
            for (int i = 0; i < 90; i++) p.Tick();
            Check.Equal(0, p.RiposteRemaining);
            p.SetGuard(false); for (int i = 0; i < 30; i++) p.Tick(); p.SetGuard(true);
            Check.True(p.ReceiveHit(source, c.Move("light1")).PerfectGuard);
            p.SetGuard(false); p.ReceiveHit(source, c.Move("light1"));
            Check.Equal(0, p.RiposteRemaining);
        }));
        tests.Add(("CombatBuilds", "EffectiveDamageAndTotalClock", () =>
        {
            var c = CombatChecks.Catalog(); var m = new CombatMatch(c, CombatProgress.New(c), "a1");
            m.Enemies[0].X = m.Player.X + 30; m.Enemies[0].Y = m.Player.Y;
            m.Player.TryAttack(c.Move("light1") with { Power = 100 });
            for (int i = 0; i < 20; i++) m.Step(default);
            Check.Equal(72, m.Statistics.EffectiveDamage);
            Check.True(m.Statistics.DamageDealt > m.Statistics.EffectiveDamage);
            Check.True(m.Statistics.TotalFrames >= m.Statistics.ActiveFrames);
        }));
        tests.Add(("CombatBuilds", "ReturnRejectsBlockMissProtectionAndDuplicates", () =>
        {
            var c = CombatChecks.Catalog(); var progress = CombatProgress.New(c);
            foreach (var wave in c.Waves.Take(3)) progress = progress.Reward(wave.Id, c);
            progress = progress.Equip("gloves", c);
            foreach (var condition in new[] { "multiple", "block", "miss", "protected" })
            {
                var m = new CombatMatch(c, progress, "b2");
                foreach (var e in m.Enemies)
                {
                    e.X = m.Player.X + (condition == "miss" ? 250 : 30); e.Y = m.Player.Y; e.Facing = -1;
                    if (condition == "block") e.SetGuard(true);
                    if (condition == "protected") { e.TryDodge(0, 1); for (int i = 0; i < 4; i++) e.Tick(); e.Y = m.Player.Y; }
                }
                m.Player.TryAttack(c.EquippedMove("light3", m.Player.Stats) with { Startup = 1, Active = 1 });
                m.Step(default);
                for (int i = 0; i < 4; i++) m.Step(default);
                Check.Equal(condition == "multiple" ? 1 : 0, m.Statistics.StaminaReturns);
            }
        }));
        tests.Add(("CombatBuilds", "InvalidMechanicsRejected", () =>
        {
            var data = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText("content/combat/combat.json"))!;
            data["items"]![1]!["comboStaminaRestore"] = -1;
            Check.Throws<ArgumentException>(() => CombatCatalog.Parse(data.ToJsonString()));
            data["items"]![1]!["comboStaminaRestore"] = 6;
            data["items"]![0]!["riposteCooldown"] = 0;
            Check.Throws<ArgumentException>(() => CombatCatalog.Parse(data.ToJsonString()));
        }));
        tests.Add(("CombatBuilds", "HitstopCountsOnlyInTotalClock", () =>
        {
            var c = CombatChecks.Catalog(); var m = new CombatMatch(c, CombatProgress.New(c), "a1");
            m.Enemies[0].X = m.Player.X + 30; m.Enemies[0].Y = m.Player.Y;
            m.Step(new CombatInput(LightPressed: true));
            for (int i = 0; i < 15; i++) m.Step(default);
            Check.Equal(16, m.Statistics.TotalFrames);
            Check.True(m.Statistics.ActiveFrames < m.Statistics.TotalFrames);
        }));
    }
}
