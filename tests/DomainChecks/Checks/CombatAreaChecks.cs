using GeXingzhou.Domain;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GeXingzhou.DomainChecks;

public static class CombatAreaChecks
{
    private static CombatMatch Bout(CombatCatalog catalog, int seed = 0)
    {
        var progress = CombatProgress.New(catalog).Reward("a1", catalog).Reward("a2", catalog).Reward("b1", catalog);
        var match = new CombatMatch(catalog, progress, "b2", randomSeed: seed);
        var player = match.Player;
        match.Enemies[0].X = player.X + 30;
        match.Enemies[0].Y = player.Y;
        match.Enemies[1].X = player.X + 300;
        foreach (var enemy in match.Enemies) enemy.Cooldown = 900;
        return match;
    }

    private static int CriticalSeed() => Enumerable.Range(0, 1000).First(seed => new Random(seed).NextDouble() < .1);

    private static CombatMatch LaneBout(CombatCatalog catalog, double depth = 500)
    {
        var match = Bout(catalog);
        match.Player.X = 500;
        match.Player.Y = depth;
        match.Enemies[0].X = 553;
        match.Enemies[0].Y = depth;
        match.Enemies[0].Cooldown = 0;
        match.Enemies[1].X = 532;
        match.Enemies[1].Y = depth;
        match.Enemies[1].Cooldown = 0;
        return match;
    }

    private static JsonObject Numbers(CombatMatch match) => JsonSerializer.SerializeToNode(match.Statistics)!.AsObject();

    private static CombatEvent LightHit(CombatMatch match)
    {
        match.Step(new(LightPressed: true));
        for (var frame = 0; frame < 20; frame++)
        {
            match.Step(default);
            var hit = match.Events.FirstOrDefault(e => e.SourceId == "player" && e.Kind is "hit" or "block");
            if (hit != null) return hit;
        }
        throw new InvalidOperationException("Expected a light hit");
    }

    public static void Register(List<(string, string, Action)> tests)
    {
        tests.Add(("CombatArea", "FrontOpponentMakesRoamerPauseThenMoveLaterally", () =>
        {
            var catalog = CombatChecks.Catalog();
            Check.Equal("b2", catalog.Waves.Single(wave => wave.LaneBlocking).Id);
            foreach (var direction in new[] { -1, 1 })
            {
                var match = LaneBout(catalog);
                var roamer = match.Enemies[0];
                var quick = match.Enemies[1];
                var rearX = 500 + 53 * direction;
                roamer.X = rearX;
                quick.X = 500 + 32 * direction;
                match.Step(default);
                Check.Equal(CombatState.Idle, roamer.State);
                Check.Equal(CombatState.Attack, quick.State);
                for (var i = 0; i < 11; i++) match.Step(default);
                Check.Equal((double)rearX, roamer.X);
                Check.Equal(500d, roamer.Y);
                match.Step(default);
                Check.Equal((double)rearX, roamer.X);
                Check.True(match.IsSidestepping(roamer) && roamer.Y > 500);
            }
        }));
        tests.Add(("CombatArea", "WallDetourMovesInwardAndRoamerCanAttackAgain", () =>
        {
            foreach (var depth in new[] { 390d, 624d })
            {
                var match = LaneBout(CombatChecks.Catalog(), depth);
                match.Enemies[1].Cooldown = 900;
                var roamer = match.Enemies[0];
                roamer.RoamSide = depth == 390 ? -1 : 1;
                for (var i = 0; i < 26; i++) match.Step(default);
                Check.True(Math.Abs(roamer.Y - depth) >= 28 && roamer.Y is >= 390 and <= 624);
                for (var i = 0; i < 40 && match.IsSidestepping(roamer); i++) match.Step(default);
                Check.Equal(depth == 390 ? 434d : 580d, roamer.Y);
                for (var i = 0; i < 150 && roamer.AttackCount == 0; i++) match.Step(default);
                Check.True(roamer.AttackCount > 0);
            }
        }));
        tests.Add(("CombatArea", "LaneBlockingPreservesAnAttackAlreadyInProgress", () =>
        {
            var match = LaneBout(CombatChecks.Catalog());
            var roamer = match.Enemies[0];
            var quick = match.Enemies[1];
            quick.Y += 28;
            match.Step(default);
            Check.Equal(CombatState.Attack, roamer.State);
            var sequence = roamer.AttackSequence;
            var frame = roamer.AttackFrame;
            quick.Y = 500;
            match.Step(default);
            Check.Equal(CombatState.Attack, roamer.State);
            Check.Equal(sequence, roamer.AttackSequence);
            Check.Equal(frame + 1, roamer.AttackFrame);
            Check.True(!match.IsSidestepping(roamer));
        }));
        tests.Add(("CombatArea", "ClearLaneAndOldSnapshotKeepTheRoamersAttack", () =>
        {
            var catalog = CombatChecks.Catalog();
            var clear = LaneBout(catalog);
            clear.Enemies[1].Y += 28;
            clear.Step(default);
            Check.Equal(CombatState.Attack, clear.Enemies[0].State);

            var json = JsonNode.Parse(catalog.SourceJson)!;
            json["balanceVersion"] = "combat-area-crit-0.1";
            Check.True(json["waves"]![3]!.AsObject().Remove("laneBlocking"));
            var old = CombatCatalog.Parse(json.ToJsonString());
            Check.Equal<string?>(null, CombatChallenge.Start(old, CombatChallengeChecks.Complete(old)).Record.Validate());
            var replay = LaneBout(old);
            replay.Step(default);
            Check.Equal(CombatState.Attack, replay.Enemies[0].State);
        }));
        tests.Add(("CombatArea", "FourthWaveLightCanCriticallyHit", () =>
        {
            var catalog = CombatChecks.Catalog();
            var normal = LightHit(Bout(catalog));
            Check.Equal(12, normal.Damage); Check.True(!normal.Critical);
            var bout = Bout(catalog, CriticalSeed());
            var critical = LightHit(bout);
            Check.Equal(18, critical.Damage); Check.True(critical.Critical);
            Check.Equal(1, bout.Statistics.CriticalHits);
        }));
        tests.Add(("CombatArea", "CenteredEllipseAllowsRearButRejectsDiagonalCorner", () =>
        {
            var match = Bout(CombatChecks.Catalog());
            var player = match.Player;
            var enemy = match.Enemies[0];
            var move = new CombatMove { Id = "round_sweep", Reach = 72, Depth = 34 };
            enemy.X = player.X - 72; enemy.Y = player.Y;
            Check.True(player.InRange(enemy, move));
            enemy.X = player.X + 70; enemy.Y = player.Y + 30;
            Check.True(!player.InRange(enemy, move));
        }));
        tests.Add(("CombatArea", "AreaHitsZeroOneOrTwoOnceAndNeverCrits", () =>
        {
            var catalog = CombatChecks.Catalog();
            for (int targets = 0; targets <= 2; targets++)
            {
                var m = Bout(catalog, CriticalSeed());
                for (int i = 0; i < m.Enemies.Count; i++)
                {
                    m.Enemies[i].X = m.Player.X + (i >= targets ? 300 : i == 0 ? -30 : 32);
                    m.Enemies[i].Y = m.Player.Y;
                }
                m.Step(new(AreaPressed: true));
                Check.Equal(75d, m.Player.Stamina);
                Check.Equal(90, m.AreaCooldownRemaining);
                var hits = new List<CombatEvent>();
                var doubleHits = new List<CombatEvent>();
                for (var i = 0; i < 45; i++)
                {
                    m.Step(default);
                    hits.AddRange(m.Events.Where(e => e.MoveId == CombatMove.AreaSkillId && e.Kind == "hit"));
                    doubleHits.AddRange(m.Events.Where(e => e.Kind == "area-double-hit"));
                }
                Check.Equal(targets, hits.Count);
                Check.Equal(targets == 2 ? 1 : 0, doubleHits.Count);
                Check.Equal(targets, hits.Select(e => e.TargetId).Distinct().Count());
                Check.Equal(targets == 2 ? 27 : targets == 1 ? 13 : 0, hits.Sum(e => e.Damage));
                Check.True(hits.All(e => !e.Critical && e.Displacement == 0));
                Check.Equal(1, m.Statistics.AreaCasts);
                Check.Equal(targets, m.Statistics.AreaHits);
                Check.Equal(targets == 2 ? 1 : 0, (int?)Numbers(m)["AreaDoubleHits"]);
                Check.Equal(targets == 2 ? 27 : targets == 1 ? 13 : 0, (int?)Numbers(m)["AreaDamage"]);
            }
        }));
        tests.Add(("CombatArea", "DoubleHitsTrackIndividualCasts", () =>
        {
            var match = Bout(CombatChecks.Catalog());
            foreach (var targets in new[] { 1, 1, 2 })
            {
                for (var i = 0; i < 140 && (match.AreaCooldownRemaining > 0 || !match.Player.CanAct); i++) match.Step(default);
                Check.True(match.AreaCooldownRemaining == 0 && match.Player.CanAct);
                match.Enemies[0].X = match.Player.X - 36;
                match.Enemies[1].X = match.Player.X + (targets == 2 ? 38 : 300);
                foreach (var enemy in match.Enemies) enemy.Y = match.Player.Y;
                match.Step(new(AreaPressed: true));
                for (var i = 0; i < 30; i++) match.Step(default);
                Check.Equal(targets == 2 ? 1 : 0, (int?)Numbers(match)["AreaDoubleHits"]);
            }
            Check.Equal(3, match.Statistics.AreaCasts);
            Check.Equal(4, match.Statistics.AreaHits);
            Check.Equal(53, (int?)Numbers(match)["AreaDamage"]);
            Check.True(match.Statistics.IsValid());
        }));
        tests.Add(("CombatArea", "DoubleHitCueWaitsForTheSecondValidTarget", () =>
        {
            foreach (var guard in new[] { false, true })
            {
                var match = Bout(CombatChecks.Catalog());
                match.Step(new(AreaPressed: true));
                for (var i = 0; i < 30 && !match.HitStopped; i++) match.Step(default);
                Check.Equal(1, match.Statistics.AreaHits);
                Check.True(match.Events.All(e => e.Kind != "area-double-hit"));

                var second = match.Enemies[1];
                second.X = match.Player.X + 32;
                second.Y = match.Player.Y;
                second.Facing = -1;
                second.SetGuard(guard);
                for (var i = 0; i < 12 && match.Events.All(e => e.TargetId != second.Id); i++) match.Step(default);
                var contact = match.Events.Single(e => e.TargetId == second.Id);
                Check.Equal(guard ? "block" : "hit", contact.Kind);
                Check.Equal(guard ? 1 : 2, match.Statistics.AreaHits);
                var cue = match.Events.SingleOrDefault(e => e.Kind == "area-double-hit");
                if (guard) Check.True(cue == null);
                else
                {
                    Check.True(cue != null);
                    Check.Equal(CombatMove.AreaSkillId, cue!.MoveId);
                    Check.Equal(match.Player.Id, cue.SourceId);
                    Check.Equal(match.Player.X, cue.X);
                    Check.Equal(match.Player.Y, cue.Y);
                }
            }
        }));
        tests.Add(("CombatArea", "BlockedHitKeepsTheNextCriticalRollAndQCanBeBlocked", () =>
        {
            var m = Bout(CombatChecks.Catalog(), CriticalSeed());
            var enemy = m.Enemies[0];
            enemy.SetGuard(true);
            var blocked = LightHit(m);
            Check.True(blocked.Kind == "block" && !blocked.Critical);
            for (var steps = 0; steps < 80 && !m.Player.CanAct; steps++) m.Step(default);
            enemy.SetGuard(false);
            Check.True(LightHit(m).Critical);

            m = Bout(CombatChecks.Catalog(), CriticalSeed());
            m.Enemies[0].SetGuard(true);
            m.Step(new(AreaPressed: true));
            for (var steps = 0; steps < 30 && !m.HitStopped; steps++) m.Step(default);
            var areaBlock = m.Events.Single(e => e.MoveId == CombatMove.AreaSkillId);
            Check.Equal("block", areaBlock.Kind);
            Check.Equal(2, areaBlock.Damage);
            Check.Equal(0, m.Statistics.AreaHits);
            Check.Equal(0, (int?)Numbers(m)["AreaDoubleHits"]);
            Check.Equal(2, (int?)Numbers(m)["AreaDamage"]);
        }));
        tests.Add(("CombatArea", "AreaDamageUsesActualHealthAndExcludesLightAttacks", () =>
        {
            var catalog = CombatChecks.Catalog();
            var quick = catalog.Enemies.FindIndex(enemy => enemy.Id == "quick");
            catalog.Enemies[quick] = catalog.Enemies[quick] with { Health = 5 };
            var match = Bout(catalog);
            Check.Equal(12, LightHit(match).Damage);
            for (var i = 0; i < 80 && !match.Player.CanAct; i++) match.Step(default);
            Check.True(match.Player.CanAct);
            match.Enemies[0].X = match.Player.X - 36;
            match.Enemies[1].X = match.Player.X + 38;
            foreach (var enemy in match.Enemies) enemy.Y = match.Player.Y;
            match.Step(new(AreaPressed: true));
            for (var i = 0; i < 30; i++) match.Step(default);
            Check.Equal(1, (int?)Numbers(match)["AreaDoubleHits"]);
            Check.Equal(18, (int?)Numbers(match)["AreaDamage"]);
            Check.Equal(30, match.Statistics.EffectiveDamage);
            var roundTrip = JsonSerializer.Deserialize<CombatStatistics>(Numbers(match).ToJsonString())!;
            Check.True(roundTrip.IsValid());
            Check.Equal(18, (int?)JsonSerializer.SerializeToNode(roundTrip)!["AreaDamage"]);
        }));
        tests.Add(("CombatArea", "AreaCooldownAndUnavailableInputDoNotSpendAgain", () =>
        {
            var catalog = CombatChecks.Catalog();
            var m = Bout(catalog);
            m.Enemies[0].X = m.Player.X + 300;
            m.Step(new(AreaPressed: true));
            while (m.AreaCooldownRemaining > 2) m.Step(default);
            m.Step(new(AreaPressed: true));
            Check.Equal(1, m.Statistics.AreaCasts);
            m.Step(new(AreaPressed: true));
            Check.Equal(2, m.Statistics.AreaCasts);

            var tired = Bout(catalog);
            Check.True(tired.Player.TryAttack(catalog.Move("light1") with { Cost = 80, Startup = 1, Active = 1, Recovery = 1 }));
            for (int i = 0; i < 10; i++) tired.Step(default);
            tired.Step(new(AreaPressed: true));
            Check.Equal(0, tired.Statistics.AreaCasts);
            Check.Equal(0, tired.AreaCooldownRemaining);
            Check.Equal(1, tired.Statistics.EmptyStamina);
        }));
        tests.Add(("CombatArea", "FifthWaveSweepSeparatesGuardedAndMultipleHits", () =>
        {
            var catalog = CombatChecks.Catalog();
            var progress = catalog.Waves.Take(4).Aggregate(CombatProgress.New(catalog), (p, w) => p.Reward(w.Id, catalog));
            foreach (var (near, guardBlocks, valid, damage) in new[]
            {
                (0, true, 0, 0), (2, true, 1, 17), (3, true, 2, 32), (3, false, 3, 44)
            })
            {
                var match = new CombatMatch(catalog, progress, "c1", randomSeed: 1);
                for (int i = 0; i < match.Enemies.Count; i++)
                {
                    var enemy = match.Enemies[i];
                    enemy.X = match.Player.X + (i >= near ? 330 : 28 + i * 5);
                    enemy.Y = match.Player.Y;
                    enemy.Facing = -1;
                    enemy.Cooldown = 900;
                }
                match.Enemies[0].SetGuard(guardBlocks);
                match.Step(new(AreaPressed: true));
                Check.Equal(1, match.Statistics.AreaCasts);
                Check.Equal(80d, match.Player.Stamina);
                Check.Equal(90, match.AreaCooldownRemaining);
                var confirmations = 0;
                for (var frame = 0; frame < 44; frame++)
                {
                    match.Step(default);
                    confirmations += match.Events.Count(e => e.Kind == "area-double-hit");
                }
                Check.Equal(valid, match.Statistics.AreaHits);
                Check.Equal(damage, match.Statistics.AreaDamage);
                Check.Equal(valid >= 2 ? 1 : 0, match.Statistics.AreaDoubleHits);
                Check.Equal(valid >= 2 ? 1 : 0, confirmations);
                Check.Equal(0, match.Statistics.CriticalHits);
            }
        }));
        tests.Add(("CombatArea", "OtherWavesAndLegacyChallengeSnapshotUseOriginalRules", () =>
        {
            var c = CombatChecks.Catalog();
            Check.Equal("b2,c1", string.Join(",", c.Waves.Where(w => w.AreaSkill).Select(w => w.Id)));
            Check.Equal("b2", c.Waves.Single(w => w.CriticalChance > 0).Id);
            var first = new CombatMatch(c, CombatProgress.New(c), "a1", randomSeed: CriticalSeed());
            first.Step(new(AreaPressed: true));
            Check.True(first.Player.State == CombatState.Idle);

            var data = JsonNode.Parse(c.SourceJson)!;
            data["balanceVersion"] = "combat-borrow-position-0.1";
            data["waves"]![4]!.AsObject().Remove("areaSkill");
            var previous = CombatCatalog.Parse(data.ToJsonString());
            var previousProfile = previous.Waves.Take(4).Aggregate(CombatProgress.New(previous), (p, w) => p.Reward(w.Id, previous));
            var oldFifth = new CombatMatch(previous, previousProfile, "c1", randomSeed: 1);
            oldFifth.Step(new(AreaPressed: true));
            Check.Equal(0, oldFifth.Statistics.AreaCasts);
            Check.True(oldFifth.Statistics.AreaDoubleHits == null);
            Check.Equal<string?>(null, CombatChallenge.Start(previous, CombatChallengeChecks.Complete(previous)).Record.Validate());

            data["balanceVersion"] = "combat-footwork-0.2";
            var wave = data["waves"]![3]!.AsObject();
            foreach (var field in new[] { "areaSkill", "criticalChance", "criticalMultiplier", "laneBlocking" }) wave.Remove(field);
            var moves = data["moves"]!.AsArray();
            moves.Remove(moves.Single(m => (string?)m!["id"] == CombatMove.AreaSkillId));
            var old = CombatCatalog.Parse(data.ToJsonString());
            var oldBout = Bout(old, CriticalSeed());
            oldBout.Step(new(AreaPressed: true));
            Check.Equal(0, oldBout.Statistics.AreaCasts);
            Check.True(!LightHit(oldBout).Critical);
            var oldRecord = CombatChallenge.Start(old, CombatChallengeChecks.Complete(old)).Record;
            Check.Equal<string?>(null, oldRecord.Validate());
            var oldStatistics = JsonSerializer.Deserialize<CombatStatistics>("{}")!;
            Check.True(oldStatistics.IsValid());
            Check.Equal(0, oldStatistics.CriticalHits + oldStatistics.AreaCasts + oldStatistics.AreaHits);
            var oldAreaStatistics = JsonSerializer.Deserialize<CombatStatistics>("{\"AreaCasts\":3,\"AreaHits\":4}")!;
            Check.True(oldAreaStatistics.IsValid());
            Check.True(JsonSerializer.SerializeToNode(oldAreaStatistics)!["AreaDoubleHits"] == null);
            Check.True(JsonSerializer.SerializeToNode(oldAreaStatistics)!["AreaDamage"] == null);
        }));
    }
}
