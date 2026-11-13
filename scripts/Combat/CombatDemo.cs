using Godot;
using GeXingzhou.Domain;

public partial class CombatDemo : Control
{
    public CombatCatalog Catalog { get; private set; } = null!;
    public CombatProgress Progress { get; private set; } = null!;
    public CombatMatch? Match { get; private set; }
    public string Screen { get; private set; } = "prepare";
    private CombatRepository repository = null!;
    private CombatArena arena = null!;
    private AudioDirector audio = null!;
    private SettingsController settings = null!;
    private ChoiceController choices = null!;
    private CombatProgress? pendingReward, attemptProgress;
    private bool blockedSave, focused = true;
    private string selectedWave = "a1", saveDirectory = "";
    private double accumulator;
    private readonly HashSet<Key> held = new(), suppressed = new();
    private CombatInput pendingInput;
    private const string Prep = "Prepare/Panel/Scroll/Content/";
    private const string PrepActions = "Prepare/Footer/Content/Actions/";
    private const string Pause = "Pause/Panel/Scroll/Content/";
    private const string ResultPath = "Result/Panel/Scroll/Content/";
    private static readonly Key[] CombatKeys = { Key.W, Key.A, Key.S, Key.D, Key.Up, Key.Down, Key.Left, Key.Right, Key.J, Key.K, Key.L, Key.Space };

    public override void _Ready()
    {
        try
        {
            var session = GetNode<GameSession>("/root/GameSession"); Theme = session.CreateUiTheme();
            Catalog = CombatCatalog.Parse(Godot.FileAccess.GetFileAsString("res://content/combat/combat.json"));
            var args = OS.GetCmdlineUserArgs();
            var testing = args.Any(a => a.StartsWith("--suite=", StringComparison.Ordinal));
            var supplied = args.FirstOrDefault(a => a.StartsWith("--test-save-root=", StringComparison.Ordinal))?.Split('=', 2)[1];
            var root = testing ? "res://test-output/integration/combat-" + Guid.NewGuid().ToString("N") : "user://saves";
            if (supplied != null && supplied.StartsWith("res://test-output/", StringComparison.Ordinal) && !supplied.Contains("..")) root = supplied;
            saveDirectory = ProjectSettings.GlobalizePath(root + "/combat-v1");
            repository = new(saveDirectory, Catalog); arena = GetNode<CombatArena>("Arena");
            audio = GetNode<AudioDirector>("Audio"); settings = GetNode<SettingsController>("Settings"); choices = GetNode<ChoiceController>("Choices");
            BindUi();
            var loaded = repository.Load();
            blockedSave = loaded.Status != LoadStatus.Loaded && (loaded.Status != LoadStatus.NotFound || repository.LoadBackup().Status != LoadStatus.NotFound);
            Progress = loaded.Progress ?? CombatProgress.New(Catalog); selectedWave = Progress.LastWave;
            audio.SetScene("combat"); ShowPreparation(); Message(loaded.Message);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.IO.IOException)
        { SceneBindings.ReportFailure(this, ex.Message); }
    }

    private void BindUi()
    {
        var panel = GetNode<PanelContainer>("Prepare/Panel");
        var style = (StyleBoxFlat)panel.GetThemeStylebox("panel").Duplicate();
        style.ContentMarginTop = 70; style.ContentMarginBottom = 108; panel.AddThemeStyleboxOverride("panel", style);
        GetNode<ProgressBar>("Hud/Top/Content/Health").AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = new Color(.57f, .72f, .39f) });
        GetNode<ProgressBar>("Hud/Top/Content/Stamina").AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = new Color(.86f, .65f, .29f) });
        GetNode<Label>("Hud/Wave").AddThemeConstantOverride("outline_size", 5);
        GetNode<Label>("Hud/Wave").AddThemeColorOverride("font_outline_color", new Color(.04f, .05f, .03f, .9f));
        GetNode<Button>(PrepActions + "Start").Pressed += () => StartWave();
        GetNode<Button>(PrepActions + "Menu").Pressed += ReturnToMenu;
        GetNode<Button>(PrepActions + "Recover").Pressed += () =>
        {
            var saved = repository.RecoverBackup();
            if (saved.Success) { Progress = repository.Load().Progress!; blockedSave = false; selectedWave = Progress.LastWave; ShowPreparation(); }
            Message(saved.Message);
        };
        GetNode<Button>(PrepActions + "Reset").Pressed += () => choices.Open("保留当前战斗进度副本，并重新开始练习？", new (string, Action)[]
        { ("保留副本并开始", ResetProgress), ("返回", () => { }) });
        var wave = GetNode<OptionButton>(Prep + "Wave");
        for (int i = 0; i < Catalog.Waves.Count; i++) wave.AddItem(Catalog.Waves[i].Name, i);
        wave.ItemSelected += index => { selectedWave = Catalog.Waves[(int)index].Id; UpdatePreparation(); };
        foreach (var item in Catalog.Items)
        {
            var button = GetNode<Button>(Prep + "Equipment/" + item.Id);
            button.Pressed += () => TryEquip(item.Id);
            button.FocusEntered += () => PreviewItem(item.Id);
            button.MouseEntered += () => PreviewItem(item.Id);
            button.Icon = arena.ItemIcon(item.Id, Catalog);
        }
        GetNode<CheckButton>(Prep + "TheoryToggle").Toggled += value => GetNode<Label>(Prep + "Theory").Visible = value;
        GetNode<Button>(Pause + "Resume").Pressed += ResumeCombat;
        GetNode<Button>(Pause + "Help").Pressed += () => GetNode<Label>(Pause + "HelpText").Visible = !GetNode<Label>(Pause + "HelpText").Visible;
        GetNode<Button>(Pause + "Settings").Pressed += settings.Open;
        GetNode<Button>(Pause + "Prepare").Pressed += () => ConfirmLeave(ReturnToPreparation);
        GetNode<Button>(Pause + "Menu").Pressed += () => ConfirmLeave(ReturnToMenu);
        GetNode<Button>(ResultPath + "Continue").Pressed += ContinueResult;
        GetNode<Button>(ResultPath + "Retry").Pressed += () => StartWave();
        GetNode<Button>(ResultPath + "Menu").Pressed += ReturnToMenu;
        GetNode<Button>(ResultPath + "Export").Pressed += ExportStatistics;
    }

    public bool StartWave()
    {
        if (blockedSave || pendingReward != null || !Progress.CanEnter(selectedWave, Catalog)) return false;
        var saved = repository.Save(Progress with { LastWave = selectedWave });
        if (!saved.Success) { Message(saved.Message); return false; }
        Progress = Progress with { LastWave = selectedWave }; attemptProgress = Progress;
        Match = new(Catalog, Progress, selectedWave); arena.Match = Match;
        ChangeScreen("fight"); return true;
    }

    public void PauseCombat()
    {
        if (Screen != "fight") return;
        ChangeScreen("pause"); BindFocus(Pause, new[] { "Resume", "Help", "Settings", "Prepare", "Menu" });
    }

    public void ResumeCombat() { if (Screen == "pause") ChangeScreen("fight"); }

    public void ReturnToPreparation()
    {
        if (pendingReward != null) return;
        Match = null; arena.Match = null; ShowPreparation();
    }

    private void ShowPreparation()
    {
        ChangeScreen("prepare"); UpdatePreparation();
        var controls = new List<Control> { GetNode<OptionButton>(Prep + "Wave") };
        controls.AddRange(Catalog.Items.Where(i => Progress.Owned.Contains(i.Id)).Select(i => GetNode<Control>(Prep + "Equipment/" + i.Id)));
        controls.Add(GetNode<CheckButton>(Prep + "TheoryToggle"));
        controls.AddRange(new[] { "Start", "Recover", "Reset", "Menu" }.Select(id => GetNode<Control>(PrepActions + id)).Where(c => c.Visible && !((Button)c).Disabled));
        SceneBindings.BindFocusLoop(controls); controls[0].GrabFocus();
    }

    private void ChangeScreen(string screen)
    {
        Screen = screen; GetNode<Control>("Prepare").Visible = screen == "prepare";
        GetNode<Control>("Pause").Visible = screen == "pause"; GetNode<Control>("Result").Visible = screen == "result";
        GetNode<Control>("Hud").Visible = screen is "fight" or "pause";
        audio.SetPaused(screen != "fight"); ClearCombatInput(); accumulator = 0;
    }

    private void ConfirmLeave(Action leave) => choices.Open("离开当前波？已保存的成长保留，下次从本波开场重试。", new (string, Action)[]
        { ("离开当前波", leave), ("继续练习", () => { }) });

    private void ResetProgress()
    {
        var saved = repository.PreserveForNewGame();
        if (saved.Success)
        {
            var fresh = CombatProgress.New(Catalog); saved = repository.Save(fresh);
            if (saved.Success) { Progress = fresh; blockedSave = false; selectedWave = fresh.LastWave; ReturnToPreparation(); }
        }
        Message(saved.Message);
    }

    private void ReturnToMenu()
    {
        if (pendingReward != null && !CommitReward()) return;
        if (settings.IsOpen) settings.Close();
        choices.Close(); GetTree().ChangeSceneToFile("res://scenes/Boot.tscn");
    }

    private bool CommitReward()
    {
        if (pendingReward == null) return true;
        var saved = repository.Save(pendingReward);
        if (saved.Success) { Progress = pendingReward; pendingReward = null; }
        GetNode<Label>(ResultPath + "Message").Text = saved.Message;
        GetNode<Button>(ResultPath + "Continue").Text = saved.Success ? "继续准备" : "重试保存并继续";
        GetNode<Button>(ResultPath + "Retry").Disabled = !saved.Success;
        return saved.Success;
    }

    private void ShowResult()
    {
        ChangeScreen("result"); bool won = Match!.Result == CombatResult.Won;
        GetNode<Label>(ResultPath + "Title").Text = won ? "本波完成" : "调整一下，再试一次";
        if (won)
        {
            bool first = !Progress.Completed.Contains(selectedWave); pendingReward = Progress.Reward(selectedWave, Catalog);
            var rewards = Catalog.Wave(selectedWave);
            GetNode<Label>(ResultPath + "Reward").Text = first ? $"经验 +{rewards.Experience} · Lv.{Catalog.Level(pendingReward.Experience)}\n" +
                string.Join("、", rewards.Items.Select(id => Catalog.Item(id).Name)) : "回放挑战完成。当前成长与装备已保留。";
            CommitReward();
        }
        else
        {
            GetNode<Label>(ResultPath + "Reward").Text = "可以回准备界面换装，观察对手预警后再出手。";
            GetNode<Button>(ResultPath + "Continue").Text = "回准备界面";
            GetNode<Button>(ResultPath + "Retry").Disabled = false;
            GetNode<Label>(ResultPath + "Message").Text = "";
        }
        var s = Match.Statistics;
        GetNode<Label>(ResultPath + "Statistics").Text = $"本次可操作战斗时间 {s.ActiveSeconds:0.0} 秒\n轻击 {s.LightAttacks} · 重击 {s.HeavyAttacks} · 闪避 {s.Dodges} · 格挡 {s.Blocks}\n受击 {s.HitsTaken} 次 · 造成伤害 {s.DamageDealt} · 承受伤害 {s.DamageTaken}\n耐力不足 {s.EmptyStamina} 次";
        BindFocus(ResultPath, new[] { "Continue", "Retry", "Export", "Menu" });
    }

    private void ContinueResult()
    {
        if (!CommitReward()) return;
        if (Match?.Result == CombatResult.Won) selectedWave = Progress.LastWave;
        ReturnToPreparation();
    }

    private void BindFocus(string prefix, string[] ids)
    {
        var controls = ids.Select(id => GetNode<Button>(prefix + id)).Where(b => !b.Disabled).Cast<Control>().ToArray();
        SceneBindings.BindFocusLoop(controls); controls[0].GrabFocus();
    }

    private void Message(string value) => GetNode<Label>("Prepare/Footer/Content/Message").Text = value;

    private bool Down(params Key[] keys) => keys.Any(held.Contains);
    private void ClearCombatInput()
    {
        foreach (var key in CombatKeys) if (Input.IsPhysicalKeyPressed(key)) suppressed.Add(key);
        held.Clear(); pendingInput = default;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut) { focused = false; if (IsNodeReady()) PauseCombat(); ClearCombatInput(); }
        if (what == NotificationApplicationFocusIn) { focused = true; ClearCombatInput(); }
    }

    public override void _Input(InputEvent ev)
    {
        if (ev is not InputEventKey { Echo: false } key) return;
        var code = key.PhysicalKeycode == Key.None ? key.Keycode : key.PhysicalKeycode;
        if (!key.Pressed)
        {
            held.Remove(code); suppressed.Remove(code);
            if (code == Key.K && Screen == "fight") pendingInput = pendingInput with { HeavyReleased = true };
            return;
        }
        if (choices.IsOpen)
        {
            if (code is Key.Escape or Key.E) { choices.HandleKey(code); GetViewport().SetInputAsHandled(); }
            return;
        }
        if (settings.IsOpen)
        {
            if (code == Key.Escape) { settings.Close(); GetViewport().SetInputAsHandled(); }
            return;
        }
        if (code == Key.Escape)
        {
            if (Screen == "fight") PauseCombat(); else if (Screen == "pause") ResumeCombat();
            GetViewport().SetInputAsHandled(); return;
        }
        if (Screen == "prepare" && code == Key.I) { GetNode<Button>(Prep + "Equipment/" + Progress.Equipment["weapon"]).GrabFocus(); return; }
        if (Screen != "fight" || !focused || suppressed.Contains(code) || !CombatKeys.Contains(code)) return;
        held.Add(code);
        pendingInput = pendingInput with
        {
            LightPressed = pendingInput.LightPressed || code == Key.J,
            HeavyPressed = pendingInput.HeavyPressed || code == Key.K,
            DodgePressed = pendingInput.DodgePressed || code == Key.Space
        };
        GetViewport().SetInputAsHandled();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Screen != "fight" || !focused || settings.IsOpen || choices.IsOpen || Match == null) return;
        accumulator += Math.Min(delta, .25);
        while (accumulator >= 1d / 60 && Screen == "fight")
        {
            var input = pendingInput with
            {
                MoveX = (Down(Key.D, Key.Right) ? 1 : 0) - (Down(Key.A, Key.Left) ? 1 : 0),
                MoveY = (Down(Key.S, Key.Down) ? 1 : 0) - (Down(Key.W, Key.Up) ? 1 : 0),
                HeavyHeld = Down(Key.K), GuardHeld = Down(Key.L)
            };
            pendingInput = default; accumulator -= 1d / 60;
            Match.Step(input); arena.ShowEvents(Match.Events); PlayEvents(Match.Events);
            if (Match.Result != CombatResult.Running) ShowResult();
        }
    }

    private void PlayEvents(IEnumerable<CombatEvent> events)
    {
        foreach (var ev in events)
        {
            AudioCue? cue = ev.Kind switch { "attack" => AudioCue.CombatSwing, "hit" => AudioCue.CombatHit, "block" => AudioCue.CombatBlock,
                "down" or "break" => AudioCue.CombatDown, _ => null };
            if (cue is { } value) audio.PlayCue(value);
        }
    }

    public override void _Process(double delta)
    {
        if (Catalog == null || HasMeta("binding_error")) return;
        var session = GetNode<GameSession>("/root/GameSession");
        if (Theme.DefaultFontSize != session.Options.SubtitleSize) Theme = session.CreateUiTheme();
        arena.Scale = Vector2.One * Math.Min(Size.X / 1280, Size.Y / 720);
        arena.Position = (Size - new Vector2(1280, 720) * arena.Scale) / 2;
        if (Screen == "prepare")
        {
            var footer = GetNode<Control>("Prepare/Footer");
            var height = GetNode<VBoxContainer>("Prepare/Footer/Content").GetCombinedMinimumSize().Y;
            var bottom = Size.Y * .945f - 18;
            footer.AnchorTop = 0; footer.AnchorBottom = 0;
            footer.OffsetTop = bottom - height; footer.OffsetBottom = bottom;
            var style = (StyleBoxFlat)GetNode<PanelContainer>("Prepare/Panel").GetThemeStylebox("panel");
            if (Math.Abs(style.ContentMarginBottom - height - 36) > .1) style.ContentMarginBottom = height + 36;
        }
        if (Match == null) return;
        var p = Match.Player;
        GetNode<Label>("Hud/Top/Content/Player").Text = $"葛行舟 · Lv.{Catalog.Level(attemptProgress!.Experience)}    生命 {p.Health}/{p.Stats.Health}    耐力 {p.Stamina:0}/{p.Stats.Stamina}";
        GetNode<ProgressBar>("Hud/Top/Content/Health").Value = p.Health * 100d / p.Stats.Health;
        GetNode<ProgressBar>("Hud/Top/Content/Stamina").Value = p.Stamina * 100 / p.Stats.Stamina;
        GetNode<Label>("Hud/Top/Content/Action").Text = p.State == CombatState.Charging ? $"蓄力 {Math.Min(100, p.StateFrame * 100 / 27)}%" :
            p.State == CombatState.Attack ? p.Attack!.Name : p.State switch { CombatState.Guard => "正面防御", CombatState.Dodge => "闪避", CombatState.Hurt => "受击", CombatState.Down => "倒地", _ => "观察距离，留出耐力" };
        GetNode<Label>("Hud/Wave").Text = Match.Wave.Name + "\n" + Match.Wave.Hint;
    }

    public Godot.Collections.Dictionary ReadCombatState()
    {
        var result = new Godot.Collections.Dictionary { ["screen"] = Screen, ["experience"] = Progress.Experience, ["wave"] = selectedWave };
        if (Match == null) return result;
        var p = Match.Player;
        result["x"] = p.X; result["y"] = p.Y; result["health"] = p.Health; result["stamina"] = p.Stamina;
        result["facing"] = p.Facing; result["state"] = p.State.ToString(); result["frame"] = p.StateFrame;
        result["attack"] = p.Attack?.Id ?? ""; result["total"] = p.Attack?.Total ?? 0;
        var enemies = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var e in Match.Enemies)
            enemies.Add(new() { ["id"] = e.Enemy!.Id, ["x"] = e.X, ["y"] = e.Y, ["health"] = e.Health, ["state"] = e.State.ToString(), ["frame"] = e.StateFrame });
        result["enemies"] = enemies; return result;
    }
}
