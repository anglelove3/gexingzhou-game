using Godot;
using GeXingzhou.Domain;
using System.Text.Json;

public partial class CombatDemo
{
    public bool TryEquip(string id)
    {
        if (Screen != "prepare" || blockedSave || !Progress.Owned.Contains(id)) return false;
        var candidate = Progress.Equip(id, Catalog); var saved = repository.Save(candidate);
        if (saved.Success) { Progress = candidate; UpdatePreparation(); }
        Message(saved.Message); return saved.Success;
    }

    private void UpdatePreparation()
    {
        var level = Catalog.Level(Progress.Experience); var stats = Catalog.Stats(Progress);
        GetNode<Label>("Prepare/Heading").Text = $"街头练习 · Lv.{level}    经验 {Progress.Experience}/{Catalog.ExperienceCap}    已完成 {Progress.Completed.Count}/{Catalog.Waves.Count} 波";
        var baseline = Catalog.BaseStats(Progress.Experience);
        GetNode<Label>(Prep + "Stats").Text = $"生命 {baseline.Health} → {stats.Health}    攻击 {baseline.Attack} → {stats.Attack}    防御 {baseline.Defense} → {stats.Defense}\n" +
            $"耐力 {baseline.Stamina} → {stats.Stamina}    恢复 {stats.Recovery:0}/秒    移动 {stats.MoveSpeed:0}/秒    闪避 {stats.DodgeSeconds:0.00}秒";
        var wave = GetNode<OptionButton>(Prep + "Wave");
        for (int i = 0; i < Catalog.Waves.Count; i++) wave.SetItemDisabled(i, !Progress.CanEnter(Catalog.Waves[i].Id, Catalog));
        wave.Select(Catalog.Waves.FindIndex(w => w.Id == selectedWave));
        GetNode<Label>(Prep + "WaveHint").Text = Catalog.Wave(selectedWave).Hint;
        foreach (var item in Catalog.Items)
        {
            var b = GetNode<Button>(Prep + "Equipment/" + item.Id); bool owned = Progress.Owned.Contains(item.Id);
            b.Disabled = !owned || blockedSave;
            b.Text = item.Name + "\n" + (!owned ? "完成波次后获得" : Progress.Equipment[item.Slot] == item.Id ? "已装备" : "选择装备");
            b.TooltipText = item.Description;
        }
        GetNode<Button>(PrepActions + "Start").Disabled = blockedSave;
        GetNode<Button>(PrepActions + "Recover").Visible = repository.LoadBackup().Status == LoadStatus.Loaded && blockedSave;
        GetNode<Label>(Prep + "Preview").Text = "选择装备查看属性变化，按Enter或点击穿戴。";
        var moves = new[] { "light1", "light2", "light3" }.Select(id => Catalog.EquippedMove(id, stats)).ToArray();
        var defense = Catalog.Enemies.First(e => e.Id == Catalog.Wave(selectedWave).Enemies[0]).Defense;
        var hits = moves.Select(m => CombatCatalog.Damage(stats.Attack, m.Power, defense)).ToArray();
        GetNode<Label>(Prep + "Theory").Text = $"配置 {Catalog.Version} · 目标防御 {defense}\n三连伤害 {string.Join(" + ", hits)} = {hits.Sum()}，动作时长 {moves.Sum(m => m.Total) / 60d:0.00}秒，耐力 {moves.Sum(m => m.Cost)}\n" +
            "理论伤害 = 攻击 × 招式倍率 × 100 ÷ (100 + 防御)，四舍五入。实际收益受到距离、格挡、打断与耐力影响。";
    }

    private void PreviewItem(string id)
    {
        var item = Catalog.Item(id);
        if (!Progress.Owned.Contains(id)) { GetNode<Label>(Prep + "Preview").Text = item.Name + " · " + item.Description; return; }
        var before = Catalog.Stats(Progress); var after = Catalog.Stats(Progress.Equip(id, Catalog));
        GetNode<Label>(Prep + "Preview").Text = item.Name + " · " + item.Description +
            $"\n生命 {before.Health} → {after.Health}    攻击 {before.Attack} → {after.Attack}    防御 {before.Defense} → {after.Defense}    耐力 {before.Stamina} → {after.Stamina}";
    }

    private void ExportStatistics()
    {
        if (Match == null || attemptProgress == null) return;
        try
        {
            var folder = System.IO.Path.Combine(saveDirectory, "exports"); System.IO.Directory.CreateDirectory(folder);
            var path = System.IO.Path.Combine(folder, "combat-" + Guid.NewGuid().ToString("N") + ".json");
            var data = new { mode = "combat", version = Catalog.Version, wave = Match.Wave.Id,
                level = Catalog.Level(attemptProgress.Experience), equipment = attemptProgress.Equipment,
                result = Match.Result.ToString(), statistics = Match.Statistics };
            System.IO.File.WriteAllText(path, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            GetNode<Label>(ResultPath + "Message").Text = "本次统计已导出到 " + path;
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
        { GetNode<Label>(ResultPath + "Message").Text = "统计导出失败，可重试。" + ex.GetType().Name; }
    }
}
