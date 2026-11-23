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
                for (var i = 0; i < 45; i++)
                {
                    m.Step(default);
                    hits.AddRange(m.Events.Where(e => e.MoveId == CombatMove.AreaSkillId && e.Kind == "hit"));
                }
                Check.Equal(targets, hits.Count);
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
        tests.Add(("CombatArea", "OtherWavesAndLegacyChallengeSnapshotUseOriginalRules", () =>
        {
            var c = CombatChecks.Catalog();
            Check.Equal("b2", c.Waves.Single(w => w.AreaSkill || w.CriticalChance > 0).Id);
            var first = new CombatMatch(c, CombatProgress.New(c), "a1", randomSeed: CriticalSeed());
            first.Step(new(AreaPressed: true));
            Check.True(first.Player.State == CombatState.Idle);

            var data = JsonNode.Parse(c.SourceJson)!;
            data["balanceVersion"] = "combat-footwork-0.2";
            var wave = data["waves"]![3]!.AsObject();
            foreach (var field in new[] { "areaSkill", "criticalChance", "criticalMultiplier" }) wave.Remove(field);
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
