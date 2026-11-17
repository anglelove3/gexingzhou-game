using System.Text.Json;

namespace GeXingzhou.Domain;

public sealed record CombatChallengeHistory(List<CombatChallengeRecord> Records, List<string> Issues);

public sealed class CombatChallengeRepository
{
    private readonly string directory;
    public CombatChallengeRepository(string directory) => this.directory = Path.GetFullPath(directory);
    private string PathTo(string id, string extension) => Path.Combine(directory, "attempt-" + id + extension);
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    private static CombatChallengeRecord Read(string path)
    {
        if (new FileInfo(path).Length > 1048576) throw new ArgumentException("挑战记录过大。");
        var record = JsonSerializer.Deserialize<CombatChallengeRecord>(File.ReadAllText(path));
        if (record == null || record.Validate() is { }) throw new ArgumentException("挑战记录校验失败。");
        return record;
    }

    public SaveResult Save(CombatChallengeRecord record)
    {
        string? temporary = null;
        try
        {
            if (record.Validate() is { } error) return new(false, error);
            Directory.CreateDirectory(directory);
            using var lease = new FileStream(PathTo(record.AttemptId, ".lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var path = PathTo(record.AttemptId, ".json");
            var serialized = JsonSerializer.Serialize(record, Json);
            bool exists = File.Exists(path);
            if (exists)
            {
                var current = Read(path);
                if (current.AttemptId != record.AttemptId) return new(false, "挑战记录身份异常，请保留原记录。");
                if (JsonSerializer.Serialize(current, Json) == serialized) return new(true, "挑战成绩已保存。");
                var expected = current with { Revision = record.Revision, Outcome = record.Outcome, Waves = record.Waves };
                if (current.Outcome != ChallengeOutcome.Running || record.Revision != current.Revision + 1 ||
                    JsonSerializer.Serialize(expected, Json) != serialized || record.Waves.Count < current.Waves.Count ||
                    JsonSerializer.Serialize(record.Waves.Take(current.Waves.Count)) != JsonSerializer.Serialize(current.Waves))
                    return new(false, "挑战记录已更新，请保留本次结果后重试。");
            }
            else if (record.Revision != 0 || File.Exists(PathTo(record.AttemptId, ".bak")))
                return new(false, "挑战初始记录缺失，请保留副本并检查备份。");
            temporary = PathTo(record.AttemptId, "." + Guid.NewGuid().ToString("N") + ".tmp");
            File.WriteAllText(temporary, serialized);
            Read(temporary);
            if (exists) File.Replace(temporary, path, PathTo(record.AttemptId, ".bak"), true);
            else File.Move(temporary, path);
            temporary = null;
            return new(true, "挑战成绩已保存。");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or JsonException)
        { return new(false, "挑战记录保存失败，可重试。" + ex.GetType().Name); }
        finally
        {
            if (temporary != null)
                try { File.Delete(temporary); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    public CombatChallengeHistory LoadHistory()
    {
        var records = new List<CombatChallengeRecord>(); var issues = new List<string>();
        try
        {
            if (!Directory.Exists(directory))
            {
                if (File.Exists(directory)) issues.Add("挑战保存目录被文件占用。");
                return new(records, issues);
            }
            foreach (var path in Directory.EnumerateFiles(directory, "attempt-*.json"))
            {
                try
                {
                    var record = Read(path);
                    if (Path.GetFileName(path) != "attempt-" + record.AttemptId + ".json") throw new ArgumentException("身份异常。");
                    records.Add(record);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or JsonException)
                { issues.Add(Path.GetFileName(path) + " 无法读取，原文件及备份已保留。"); }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { issues.Add("挑战历史读取失败。" + ex.GetType().Name); }
        return new(records.OrderByDescending(r => r.StartedAt).ToList(), issues);
    }

    public SaveResult RecoverBackup(string attemptId)
    {
        if (!Guid.TryParseExact(attemptId, "N", out _)) return new(false, "挑战记录身份异常。");
        string? temporary = null;
        try
        {
            Directory.CreateDirectory(directory);
            using var lease = new FileStream(PathTo(attemptId, ".lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var backup = Read(PathTo(attemptId, ".bak"));
            if (backup.AttemptId != attemptId) return new(false, "挑战备份身份异常。");
            var path = PathTo(attemptId, ".json");
            var preserved = Path.Combine(directory, "preserved-" + attemptId + "-" + Guid.NewGuid().ToString("N") + ".json");
            if (File.Exists(path)) File.Copy(path, preserved);
            temporary = PathTo(attemptId, "." + Guid.NewGuid().ToString("N") + ".tmp");
            File.Copy(PathTo(attemptId, ".bak"), temporary);
            Read(temporary);
            File.Move(temporary, path, true); temporary = null;
            return new(true, "挑战备份已恢复，原文件副本已保留。");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or JsonException)
        { return new(false, "挑战备份恢复失败。" + ex.GetType().Name); }
        finally
        {
            if (temporary != null)
                try { File.Delete(temporary); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
}
