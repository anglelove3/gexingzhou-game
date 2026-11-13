using GeXingzhou.Domain;
using System.Text.Json;

namespace GeXingzhou.DomainChecks;

public static class CombatChecks
{
    public static CombatCatalog Catalog() => CombatCatalog.Parse(File.ReadAllText("content/combat/combat.json"));

    public static void Register(List<(string, string, Action)> tests)
    {
        tests.Add(("Combat", "CatalogAndDamage", () =>
        {
            var c = Catalog();
            Check.Equal(9, c.Items.Count);
            Check.Equal(6, c.Waves.Count);
            Check.Equal(4, c.Enemies.Count);
            Check.Equal(11, CombatCatalog.Damage(14, .8, 6));
            Check.Equal(12, CombatCatalog.Damage(14, .9, 6));
            Check.Equal(15, CombatCatalog.Damage(14, 1.1, 6));
            Check.Equal(1, CombatCatalog.Damage(1, .1, 999));
            Check.Throws<ArgumentException>(() => CombatCatalog.Parse("{}"));
        }));
        tests.Add(("Combat", "RewardsAndGrowth", () =>
        {
            var c = Catalog(); var p = CombatProgress.New(c);
            Check.Equal(1, c.Level(p.Experience));
            Check.Equal(16, c.Stats(p).Defense);
            Check.Throws<InvalidOperationException>(() => p.Reward("c2", c));
            foreach (var wave in c.Waves)
            {
                p = p.Reward(wave.Id, c);
                Check.Equal(p, p.Reward(wave.Id, c));
            }
            Check.Equal(480, p.Experience);
            Check.Equal(5, c.Level(p.Experience));
            Check.Equal(9, p.Owned.Count);
            Check.True(p.CompletedRun);
            Check.Equal<string?>(null, p.Validate(c));
            var changed = p.Equip("stick", c).Equip("padded", c);
            Check.Equal(26, c.Stats(changed).Attack);
            Check.Equal(32, c.Stats(changed).Defense);
            Check.True(c.Stats(changed).MoveSpeed < c.Stats(p).MoveSpeed);
            Check.Equal("wraps", p.Equipment["weapon"]);
            Check.True((p with { Experience = 999 }).Validate(c) != null);
            Check.True((p with { Owned = new List<string>{"missing"} }).Validate(c) != null);
        }));
        tests.Add(("Combat", "EquipmentBudget", () =>
        {
            var c = Catalog(); var p = CombatProgress.New(c);
            foreach (var wave in c.Waves) p = p.Reward(wave.Id, c);
            foreach (var weapon in c.Items.Where(i => i.Slot == "weapon"))
            foreach (var coat in c.Items.Where(i => i.Slot == "coat"))
            foreach (var shoes in c.Items.Where(i => i.Slot == "shoes"))
            {
                var s = c.Stats(p.Equip(weapon.Id, c).Equip(coat.Id, c).Equip(shoes.Id, c));
                Check.True(s.Health > 0 && s.Attack > 0 && s.Defense >= 0 && s.Stamina > 0);
                Check.True(s.MoveSpeed > 0 && s.Recovery > 0 && s.DodgeSeconds >= .18);
            }
        }));
        tests.Add(("Combat", "AtomicSaveAndRecovery", () =>
        {
            var c = Catalog(); var dir = Path.Combine("test-output", "combat-save-" + Guid.NewGuid().ToString("N"));
            var repo = new CombatRepository(dir, c); var p = CombatProgress.New(c);
            Check.Equal(LoadStatus.NotFound, repo.Load().Status);
            Check.True(repo.Save(p).Success);
            Check.True(repo.Save(p.Reward("a1", c)).Success);
            Check.Equal(30, repo.Load().Progress!.Experience);
            File.WriteAllText(Path.Combine(dir, "save.json"), "{bad");
            Check.Equal(LoadStatus.Corrupt, repo.Load().Status);
            Check.True(!repo.Save(p).Success);
            Check.True(repo.RecoverBackup().Success);
            Check.Equal(0, repo.Load().Progress!.Experience);
            Check.True(Directory.GetFiles(dir, "preserved-*").Length >= 2);
            File.WriteAllText(Path.Combine(dir, "save.json"), JsonSerializer.Serialize(p with { SchemaVersion = 99 }));
            Check.Equal(LoadStatus.UnsupportedVersion, repo.Load().Status);
            Check.True(!repo.Save(p).Success);
        }));
        tests.Add(("Combat", "SaveFailureKeepsCandidate", () =>
        {
            var c = Catalog(); var p = CombatProgress.New(c).Reward("a1", c);
            var path = Path.Combine("test-output", "combat-file-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory("test-output"); File.WriteAllText(path, "occupied");
            var repo = new CombatRepository(path, c);
            Check.Equal(LoadStatus.IoError, repo.Load().Status);
            Check.True(!repo.Save(p).Success);
            Check.Equal(30, p.Experience);
            File.Delete(path); Check.True(repo.Save(p).Success);
            Check.Equal(30, repo.Load().Progress!.Experience);
        }));
        tests.Add(("Combat", "AttackWindowAndOneHit", () =>
        {
            var c = Catalog(); var match = new CombatMatch(c, CombatProgress.New(c), "a1");
            match.Enemies[0].X = match.Player.X + 45; match.Enemies[0].Y = match.Player.Y;
            match.Step(new CombatInput(LightPressed: true));
            for (int i = 0; i < 5; i++) match.Step(default);
            Check.Equal(72, match.Enemies[0].Health);
            for (int i = 0; i < 7; i++) match.Step(default);
            Check.Equal(61, match.Enemies[0].Health);
            Check.Equal(88d, match.Player.Stamina);
        }));
        tests.Add(("Combat", "DepthAndFacing", () =>
        {
            var c = Catalog(); var match = new CombatMatch(c, CombatProgress.New(c), "a1");
            var p = match.Player; var e = match.Enemies[0];
            e.X = p.X + 45; e.Y = p.Y + 23;
            Check.True(!p.InRange(e, c.Move("light1")));
            e.Y = p.Y + 22; Check.True(p.InRange(e, c.Move("light1")));
            e.X = p.X - 10; Check.True(!p.InRange(e, c.Move("light1")));
        }));
        tests.Add(("Combat", "GuardAndBreak", () =>
        {
            var c = Catalog(); var stats = c.Stats(CombatProgress.New(c));
            var p = new CombatFighter("player", "葛行舟", stats, 400, 480);
            var e = new CombatFighter("enemy", "对手", stats, 440, 480);
            p.SetGuard(true);
            var hit = p.ReceiveHit(e, c.Move("light1"));
            Check.True(hit.Blocked); Check.Equal(98, p.Health); Check.True(p.Stamina < 100);
            for (int i = 0; i < 20 && p.State == CombatState.Guard; i++) p.ReceiveHit(e, c.Move("heavy"));
            Check.Equal(CombatState.Hurt, p.State);
            Check.Equal(0d, p.Stamina);
            var rear = new CombatFighter("rear", "对手", stats, 360, 480);
            var fresh = new CombatFighter("p", "葛行舟", stats, 400, 480); fresh.SetGuard(true);
            Check.True(!fresh.ReceiveHit(rear, c.Move("light1")).Blocked);
        }));
        tests.Add(("Combat", "DodgeAndRecovery", () =>
        {
            var c = Catalog(); var stats = c.Stats(CombatProgress.New(c));
            var p = new CombatFighter("p", "葛行舟", stats, 400, 480);
            Check.True(p.TryDodge(1, 0)); Check.Equal(76d, p.Stamina);
            for (int i = 0; i < 6; i++) p.Tick();
            Check.True(p.Protected); Check.True(!p.TryDodge(1, 0));
            for (int i = 0; i < 95; i++) p.Tick();
            Check.True(p.Stamina > 76 && p.Stamina <= 100); Check.True(p.X > 400);
        }));
        tests.Add(("Combat", "ChargedBreaksGuardAndLeaderCount", () =>
        {
            var c = Catalog(); var stats = c.Stats(CombatProgress.New(c));
            var p = new CombatFighter("p", "葛行舟", stats, 400, 480);
            var g = CombatFighter.FromEnemy("g", c.Enemies.Single(e => e.Id == "guard"), 440, 480); g.Facing = -1; g.SetGuard(true);
            Check.True(g.ReceiveHit(p, c.Move("heavy")).Blocked); Check.Equal(CombatState.Guard, g.State);
            Check.True(!g.ReceiveHit(p, c.Move("charged")).Blocked); Check.Equal(CombatState.Down, g.State);
            var leader = CombatFighter.FromEnemy("b", c.Enemies.Single(e => e.Id == "leader"), 440, 480);
            leader.ReceiveHit(p, c.Move("charged")); Check.True(leader.State != CombatState.Down);
            for (int i = 0; i < 12; i++) leader.Tick();
            leader.ReceiveHit(p, c.Move("heavy")); Check.Equal(CombatState.Down, leader.State);
        }));
        tests.Add(("Combat", "ChargeAndComboInput", () =>
        {
            var c = Catalog(); var m = new CombatMatch(c, CombatProgress.New(c), "a1");
            m.Step(new CombatInput(HeavyPressed: true, HeavyHeld: true));
            for (int i = 0; i < 28; i++) m.Step(new CombatInput(HeavyHeld: true));
            m.Step(new CombatInput(HeavyReleased: true));
            Check.Equal("charged", m.Player.Attack!.Id); Check.Equal(72d, m.Player.Stamina);
            var combo = new CombatMatch(c, CombatProgress.New(c), "a1");
            combo.Step(new CombatInput(LightPressed: true));
            for (int i = 0; i < 16; i++) combo.Step(default);
            combo.Step(new CombatInput(LightPressed: true));
            for (int i = 0; i < 8; i++) combo.Step(default);
            Check.Equal("light2", combo.Player.Attack!.Id);
        }));
        tests.Add(("Combat", "AttackPermitAndDeterminism", () =>
        {
            var c = Catalog(); var p = CombatProgress.New(c);
            foreach (var w in c.Waves.Take(4)) p = p.Reward(w.Id, c);
            var a = new CombatMatch(c, p, "c1"); var b = new CombatMatch(c, p, "c1");
            for (int i = 0; i < 900; i++)
            {
                var input = new CombatInput(MoveY: i % 240 < 120 ? .4 : -.4, GuardHeld: true);
                a.Step(input); b.Step(input);
                Check.True(a.Enemies.Count(e => e.State == CombatState.Attack && e.AttackFrame < e.Attack!.Startup + e.LastActiveOffset) <= 1);
                Check.Equal(a.Player.Health, b.Player.Health);
                Check.Equal(a.Player.Y, b.Player.Y);
            }
        }));
        tests.Add(("Combat", "SixWavesPlayableGrowth", () =>
        {
            var c = Catalog(); var p = CombatProgress.New(c);
            foreach (var wave in c.Waves)
            {
                foreach (var id in new[] { "stick", "padded", "steady" }) if (p.Owned.Contains(id)) p = p.Equip(id, c);
                var m = new CombatMatch(c, p, wave.Id);
                for (int tick = 0; tick < 12000 && m.Result == CombatResult.Running; tick++)
                {
                    m.Step(CombatAutomation.Decide(m));
                    if (Environment.GetEnvironmentVariable("COMBAT_TRACE") == wave.Id && m.Events.Count > 0)
                        Console.WriteLine($"TRACE {tick} p={m.Player.State} x={m.Player.X:0} hp={m.Player.Health} st={m.Player.Stamina:0} e={m.Enemies[0].State} ex={m.Enemies[0].X:0} ehp={m.Enemies[0].Health} events={string.Join(',',m.Events.Select(e=>e.Kind+e.Damage))}");
                }
                Console.WriteLine($"COMBAT_AUTO {wave.Id} {m.Result} hp={m.Player.Health} seconds={m.Statistics.ActiveSeconds:0.0} damage={m.Statistics.DamageDealt} hits={m.Statistics.HitsTaken}");
                Check.Equal(CombatResult.Won, m.Result);
                p = p.Reward(wave.Id, c);
            }
            Check.Equal(5, c.Level(p.Experience)); Check.Equal(9, p.Owned.Count);
        }));
        tests.Add(("Combat", "MoveAndAttackTurnsFacing", () =>
        {
            var c = Catalog(); var m = new CombatMatch(c, CombatProgress.New(c), "a1");
            m.Step(new CombatInput(MoveX: -1, LightPressed: true));
            Check.Equal(-1, m.Player.Facing);
        }));
        tests.Add(("Combat", "GrowthIsDataDriven", () =>
        {
            var data = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText("content/combat/combat.json"))!;
            data["growth"] = System.Text.Json.Nodes.JsonNode.Parse("{\"health\":100,\"attack\":18,\"defense\":8,\"stamina\":100,\"recovery\":24,\"moveSpeed\":180,\"dodgeSeconds\":0.34,\"levelHealth\":12,\"levelAttack\":2,\"levelDefense\":2,\"levelStamina\":5}");
            data["levels"]![1] = 50;
            var changed = CombatCatalog.Parse(data.ToJsonString());
            Check.Equal(18, changed.Stats(CombatProgress.New(changed)).Attack);
            Check.Equal(2, changed.Level(50));
        }));
        tests.Add(("Combat", "StaleSaveCannotRegressGrowth", () =>
        {
            var c = Catalog(); var p = CombatProgress.New(c);
            var dir = Path.Combine("test-output", "combat-concurrent-" + Guid.NewGuid().ToString("N"));
            var a = new CombatRepository(dir, c); var b = new CombatRepository(dir, c);
            Check.True(a.Save(p).Success); Check.True(a.Save(p.Reward("a1", c)).Success);
            Check.True(!b.Save(p).Success); Check.Equal(30, a.Load().Progress!.Experience);
            using (var lease = new FileStream(Path.Combine(dir, "write.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                Check.True(!b.Save(p.Reward("a1", c)).Success);
            Check.True(a.PreserveForNewGame().Success); Check.True(a.Save(p).Success);
        }));
        tests.Add(("Combat", "WaitingEnemySpacing", () =>
        {
            var c = Catalog(); var p = CombatProgress.New(c);
            foreach (var wave in c.Waves.Take(4)) p = p.Reward(wave.Id, c);
            var m = new CombatMatch(c, p, "c1");
            foreach (var e in m.Enemies) { e.X = m.Player.X + 45; e.Y = m.Player.Y; }
            for (int i = 0; i < 30; i++) m.Step(new CombatInput(GuardHeld: true));
            Check.True(Math.Abs(m.Enemies[2].Y - m.Player.Y) >= 20);
        }));
        tests.Add(("Combat", "GuardRepeatsDefensiveCycle", () =>
        {
            var c = Catalog(); var p = CombatProgress.New(c).Reward("a1", c).Reward("a2", c);
            var m = new CombatMatch(c, p, "b1"); var e = m.Enemies[0];
            e.X = m.Player.X + 45; e.Y = m.Player.Y;
            int entries = 1, guardFrames = 0; var previous = e.State;
            for (int i = 0; i < 650; i++)
            {
                m.Step(new CombatInput(GuardHeld: true));
                if (e.State == CombatState.Guard) { guardFrames++; if (previous != CombatState.Guard) entries++; }
                previous = e.State;
            }
            Check.True(entries >= 3); Check.True(guardFrames >= 100);
        }));
        tests.Add(("Combat", "SuspendDropsQueuedCombo", () =>
        {
            var c = Catalog(); var m = new CombatMatch(c, CombatProgress.New(c), "a1");
            m.Step(new CombatInput(LightPressed: true));
            for (int i = 0; i < 16; i++) m.Step(default);
            m.Step(new CombatInput(LightPressed: true)); m.SuspendInput();
            for (int i = 0; i < 30; i++) m.Step(default);
            Check.Equal(1, m.Statistics.LightAttacks);
        }));
        tests.Add(("Combat", "RecoveryTimerDoesNotExtendHitStun", () =>
        {
            var c = Catalog(); var stats = c.Stats(CombatProgress.New(c));
            var attacker = new CombatFighter("p", "葛行舟", stats, 400, 480);
            foreach (var id in new[] { "quick", "leader" })
            {
                var target = CombatFighter.FromEnemy("e", c.Enemies.Single(e => e.Id == id), 440, 480);
                target.Recover(30); target.ReceiveHit(attacker, c.Move("light1"));
                for (int i = 0; i < 12; i++) target.Tick();
                Check.Equal(CombatState.Idle, target.State);
            }
        }));
    }
}
