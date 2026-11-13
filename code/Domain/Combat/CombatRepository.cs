using System.Text.Json;
using System.Security.Cryptography;

namespace GeXingzhou.Domain;

public sealed record CombatLoad(LoadStatus Status, CombatProgress? Progress, string Message);

public sealed class CombatRepository
{
    private readonly string directory;
    private readonly CombatCatalog catalog;
    private (string Save, string Backup)? preservedState;
    private string PathTo(string name) => Path.Combine(directory, name);
    public CombatRepository(string directory, CombatCatalog catalog)
    {
        this.directory = Path.GetFullPath(directory); this.catalog = catalog;
    }

    public CombatLoad Load() => Read(PathTo("save.json"));
    public CombatLoad LoadBackup() => Read(PathTo("save.bak.json"));

    private CombatLoad Read(string path)
    {
        try
        {
            var p = JsonSerializer.Deserialize<CombatProgress>(File.ReadAllText(path));
            if (p == null) return new(LoadStatus.Corrupt, null, "战斗存档为空。");
            if (p.SchemaVersion != 1 || p.ContentVersion != catalog.Version)
                return new(LoadStatus.UnsupportedVersion, null, "战斗存档版本不兼容，请保留副本或恢复备份。");
            var error = p.Validate(catalog);
            return error == null ? new(LoadStatus.Loaded, p, "已读取战斗进度。") : new(LoadStatus.Corrupt, null, error);
        }
        catch (FileNotFoundException) { return new(LoadStatus.NotFound, null, "暂无战斗进度。"); }
        catch (DirectoryNotFoundException)
        {
            var parent = Path.GetDirectoryName(path);
            while (parent != null && !Directory.Exists(parent))
            {
                if (File.Exists(parent)) return new(LoadStatus.IoError, null, "战斗保存目录被文件占用。");
                parent = Path.GetDirectoryName(parent);
            }
            return new(LoadStatus.NotFound, null, "暂无战斗进度。");
        }
        catch (JsonException) { return new(LoadStatus.Corrupt, null, "战斗存档损坏，请恢复备份或保留副本重开。"); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        { return new(LoadStatus.IoError, null, "战斗存档读取失败。" + ex.GetType().Name); }
    }

    public SaveResult Save(CombatProgress p)
    {
        try
        {
            Directory.CreateDirectory(directory);
            using var lease = new FileStream(PathTo("write.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            return SaveLocked(p);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        { return new(false, "战斗保存失败，可重试。" + ex.GetType().Name); }
    }

    private SaveResult SaveLocked(CombatProgress p)
    {
        string? temp = null;
        try
        {
            if (p.Validate(catalog) is { } error) return new(false, error);
            var current = Load(); var backup = LoadBackup();
            if (current.Status == LoadStatus.IoError) return new(false, current.Message);
            var preserved = preservedState.HasValue;
            if (preserved && preservedState!.Value != FileState())
            {
                preservedState = null;
                return new(false, "保留副本后进度已在另一窗口更新，请重新保留副本后再操作。");
            }
            if (!preserved && current.Progress != null && current.Progress.RunId != p.RunId)
                return new(false, "另一窗口已重开或恢复练习，请返回主菜单重新读取当前周目。");
            if (!preserved && current.Progress != null && current.Progress.Experience > p.Experience)
                return new(false, "战斗进度已在另一窗口更新，请返回主菜单重新读取。");
            if (!preserved && (current.Status is not (LoadStatus.Loaded or LoadStatus.NotFound) ||
                current.Status == LoadStatus.NotFound && backup.Status != LoadStatus.NotFound))
                return new(false, "请先恢复备份或保留原档副本。");
            temp = PathTo("write-" + Guid.NewGuid().ToString("N") + ".tmp");
            File.WriteAllText(temp, JsonSerializer.Serialize(p, new JsonSerializerOptions { WriteIndented = true }));
            if (Read(temp).Status != LoadStatus.Loaded) return new(false, "临时战斗存档校验失败。");
            if (current.Status == LoadStatus.Loaded) File.Replace(temp, PathTo("save.json"), PathTo("save.bak.json"), true);
            else File.Move(temp, PathTo("save.json"), preserved);
            temp = null; preservedState = null; return new(true, "战斗进度已保存。");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        { return new(false, "战斗保存失败，可重试。" + ex.GetType().Name); }
        finally
        {
            if (temp != null && File.Exists(temp))
                try { File.Delete(temp); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    public SaveResult PreserveForNewGame()
    {
        try
        {
            Directory.CreateDirectory(directory);
            using var lease = new FileStream(PathTo("write.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            return PreserveLocked();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { return new(false, "保留战斗存档失败。" + ex.GetType().Name); }
    }

    private SaveResult PreserveLocked()
    {
        foreach (var name in new[] { "save.json", "save.bak.json" })
            if (File.Exists(PathTo(name))) File.Copy(PathTo(name), PathTo("preserved-" + Guid.NewGuid().ToString("N") + ".json"));
        preservedState = FileState(); return new(true, "战斗进度副本已保留。");
    }

    private (string Save, string Backup) FileState() => (FileHash(PathTo("save.json")), FileHash(PathTo("save.bak.json")));
    private static string FileHash(string path) => File.Exists(path) ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) : "";

    public SaveResult PreserveAttempt(CombatProgress p)
    {
        string? temporary = null;
        try
        {
            if (p.Validate(catalog) is { } error) return new(false, error);
            Directory.CreateDirectory(directory);
            using var lease = new FileStream(PathTo("write.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var target = PathTo("preserved-attempt-" + Guid.NewGuid().ToString("N") + ".json");
            temporary = target + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(p, new JsonSerializerOptions { WriteIndented = true }));
            if (Read(temporary).Status != LoadStatus.Loaded) return new(false, "本次战斗副本校验失败，可重试。");
            File.Move(temporary, target); temporary = null;
            return new(true, "本次旧周目进度副本已保留。");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        { return new(false, "本次战斗副本保存失败，可重试。" + ex.GetType().Name); }
        finally
        {
            if (temporary != null && File.Exists(temporary))
                try { File.Delete(temporary); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    public SaveResult RecoverBackup()
    {
        try
        {
            Directory.CreateDirectory(directory);
            using var lease = new FileStream(PathTo("write.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var backup = LoadBackup();
            if (backup.Status != LoadStatus.Loaded) return new(false, backup.Message);
            PreserveLocked();
            return SaveLocked(backup.Progress! with { RunId = Guid.NewGuid().ToString("N") });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        { return new(false, "恢复战斗存档失败，可重试。" + ex.GetType().Name); }
    }
}
