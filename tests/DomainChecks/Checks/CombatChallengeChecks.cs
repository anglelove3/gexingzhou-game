using GeXingzhou.Domain;
using System.Text.Json;

namespace GeXingzhou.DomainChecks;

public static class CombatChallengeChecks
{
    public static CombatProgress Complete(CombatCatalog c)
    {
        var p = CombatProgress.New(c);
        foreach (var wave in c.Waves) p = p.Reward(wave.Id, c);
        return p.Equip("stick", c).Equip("padded", c).Equip("steady", c);
    }

    public static void Win(CombatMatch match)
    {
        for (int tick = 0; tick < 7200 && match.Result == CombatResult.Running; tick++) match.Step(CombatAutomation.Decide(match));
        Check.Equal(CombatResult.Won, match.Result);
    }

    public static void Register(List<(string, string, Action)> tests)
    {
        tests.Add(("CombatChallenge", "UnlockSnapshotAndCarryHealth", () =>
        {
            var c = CombatChecks.Catalog();
            Check.Throws<InvalidOperationException>(() => CombatChallenge.Start(c, CombatProgress.New(c)));
            var p = Complete(c); var challenge = CombatChallenge.Start(c, p);
            var original = challenge.Record.Progress.Equipment["weapon"];
            p.Equipment["weapon"] = "wraps";
            challenge.Record.Progress.Equipment["weapon"] = "wraps";
            Check.Equal(original, challenge.Record.Progress.Equipment["weapon"]);
            var match = challenge.CreateMatch();
            var source = CombatFighter.FromEnemy("s", c.Enemies[0], match.Player.X + 30, match.Player.Y);
            match.Player.ReceiveHit(source, c.Move("light1"));
            Win(match); int health = match.Player.Health;
            challenge.FinishWave(match);
            Check.Equal(1, challenge.Record.CompletedWaves);
            Check.Throws<InvalidOperationException>(() => challenge.CreateMatch());
            challenge.Continue(); var next = challenge.CreateMatch();
            Check.Equal(health, next.Player.Health);
            Check.Equal((double)next.Player.Stats.Stamina, next.Player.Stamina);
            Check.Equal(0, next.Player.RiposteRemaining);
            Check.Equal(c.ConfigHash, challenge.Record.ConfigHash);
        }));
        tests.Add(("CombatChallenge", "SixWavesAndAbandon", () =>
        {
            var c = CombatChecks.Catalog(); var challenge = CombatChallenge.Start(c, Complete(c));
            for (int i = 0; i < 6; i++)
            {
                var match = challenge.CreateMatch(); Win(match); challenge.FinishWave(match);
                if (i < 5) challenge.Continue();
            }
            Check.Equal(ChallengeOutcome.Won, challenge.Record.Outcome);
            Check.Equal(6, challenge.Record.CompletedWaves);
            Check.Equal<string?>(null, challenge.Record.Validate());
            Check.Throws<InvalidOperationException>(() => challenge.Continue());
            var exited = CombatChallenge.Start(c, Complete(c)); exited.CreateMatch(); exited.End(); exited.End();
            Check.Equal(ChallengeOutcome.Abandoned, exited.Record.Outcome);
            Check.Equal(0, exited.Record.CompletedWaves);
            Check.Equal(1, exited.Record.Waves.Count);
        }));
        tests.Add(("CombatChallenge", "LossAndInterruptedHistory", () =>
        {
            var c = CombatChecks.Catalog(); var challenge = CombatChallenge.Start(c, Complete(c));
            var match = challenge.CreateMatch();
            var source = CombatFighter.FromEnemy("s", c.Enemies[0], match.Player.X + 30, match.Player.Y);
            match.Player.ReceiveHit(source, c.Move("heavy") with { Power = 1000 }); match.Step(default);
            challenge.FinishWave(match);
            Check.Equal(ChallengeOutcome.Lost, challenge.Record.Outcome);
            Check.Equal(0, challenge.Record.Waves[0].HealthEnd);
            Check.Equal<string?>(null, challenge.Record.Validate());
            var dir = Path.Combine("test-output", "challenge-" + Guid.NewGuid().ToString("N"));
            var repo = new CombatChallengeRepository(dir);
            var active = CombatChallenge.Start(c, Complete(c));
            Check.True(repo.Save(active.Record).Success);
            Check.Equal(ChallengeOutcome.Running, repo.LoadHistory().Records.Single().Outcome);
        }));
        tests.Add(("CombatChallenge", "AtomicSaveRetryAndStaleRejection", () =>
        {
            var c = CombatChecks.Catalog(); var challenge = CombatChallenge.Start(c, Complete(c));
            var dir = Path.Combine("test-output", "challenge-save-" + Guid.NewGuid().ToString("N"));
            var repo = new CombatChallengeRepository(dir); var initial = challenge.Record;
            Check.True(repo.Save(initial).Success); Check.True(repo.Save(initial).Success);
            var match = challenge.CreateMatch(); Win(match); challenge.FinishWave(match);
            using (var lease = new FileStream(Path.Combine(dir, "attempt-" + initial.AttemptId + ".lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                Check.True(!repo.Save(challenge.Record).Success);
            Check.Equal(0, repo.LoadHistory().Records.Single().CompletedWaves);
            Check.True(repo.Save(challenge.Record).Success); Check.True(repo.Save(challenge.Record).Success);
            Check.True(!repo.Save(initial).Success);
            var loaded = repo.LoadHistory().Records.Single();
            Check.Equal(match.Statistics.EffectiveDamage, loaded.Waves[0].Statistics.EffectiveDamage);
            Check.True(loaded.Waves[0].Statistics.TotalFrames > 0);
            Check.Equal(1, Directory.GetFiles(dir, "attempt-*.json").Length);
            File.WriteAllText(Path.Combine(dir, "attempt-" + initial.AttemptId + ".json"), "{bad");
            Check.Equal(1, repo.LoadHistory().Issues.Count);
            Check.True(repo.RecoverBackup(initial.AttemptId).Success);
            Check.Equal(0, repo.LoadHistory().Records.Single().CompletedWaves);
        }));
        tests.Add(("CombatChallenge", "InvalidRecordAndIndependentRuns", () =>
        {
            var c = CombatChecks.Catalog(); var a = CombatChallenge.Start(c, Complete(c));
            var b = CombatChallenge.Start(c, Complete(c));
            var dir = Path.Combine("test-output", "challenge-runs-" + Guid.NewGuid().ToString("N"));
            var repo = new CombatChallengeRepository(dir);
            Check.True(repo.Save(a.Record).Success); Check.True(repo.Save(b.Record).Success);
            Check.Equal(2, repo.LoadHistory().Records.Count);
            Check.True(!repo.Save(a.Record with { ConfigHash = new string('0', 64) }).Success);
            Check.True(!repo.Save(a.Record with { AttemptId = "../bad" }).Success);
            var data = JsonSerializer.Serialize(a.Record);
            Check.Equal<string?>(null, JsonSerializer.Deserialize<CombatChallengeRecord>(data)!.Validate());
        }));
        tests.Add(("CombatChallenge", "MalformedHistoryKeepsOtherRecordsAvailable", () =>
        {
            var c = CombatChecks.Catalog(); var a = CombatChallenge.Start(c, Complete(c)); var b = CombatChallenge.Start(c, Complete(c));
            var dir = Path.Combine("test-output", "challenge-corrupt-" + Guid.NewGuid().ToString("N"));
            var repo = new CombatChallengeRepository(dir);
            Check.True(repo.Save(a.Record).Success); Check.True(repo.Save(b.Record).Success);
            foreach (var corruption in new[] { "null-item", "overflow" })
            {
                var config = System.Text.Json.Nodes.JsonNode.Parse(a.Record.ConfigJson)!;
                if (corruption == "null-item") config["items"]![0] = null;
                else config["waves"]![0]!["experience"] = int.MaxValue;
                var broken = a.Record with { ConfigJson = config.ToJsonString() };
                File.WriteAllText(Path.Combine(dir, "attempt-" + a.Record.AttemptId + ".json"), JsonSerializer.Serialize(broken));
                var history = repo.LoadHistory();
                Check.Equal(1, history.Records.Count); Check.Equal(1, history.Issues.Count);
                Check.Equal(b.Record.AttemptId, history.Records[0].AttemptId);
            }
        }));
    }
}
