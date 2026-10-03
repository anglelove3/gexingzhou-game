using Godot;
using GeXingzhou.Domain;
public partial class BootMenu : Control
{
    public override void _Ready()
    {
        var box=new VBoxContainer{Position=new Vector2(320,80),Size=new Vector2(640,540)}; AddChild(box);
        box.AddChild(new Label{Text="葛行舟",HorizontalAlignment=HorizontalAlignment.Center});
        box.AddChild(new Label{Text="首段试玩 · 原型美术",HorizontalAlignment=HorizontalAlignment.Center});
        var start=new Button{Text="回到故乡",CustomMinimumSize=new Vector2(480,54)}; box.AddChild(start);
        var session=GetNode<GameSession>("/root/GameSession");var loaded=session.Saves.Load();var manual=session.ManualSaves.Load();
        Theme=session.CreateUiTheme();var preferences=new SettingsController();AddChild(preferences);
        var choices=new ChoiceController();AddChild(choices);
        void BeginNew(){var a=session.Saves.PreserveForNewGame();var m=session.ManualSaves.PreserveForNewGame();if(!a.Success||!m.Success){box.AddChild(new Label{Text=a.Message+"\n"+m.Message});return;}session.NewGame();GetTree().ChangeSceneToFile("res://scenes/Main.tscn");}
        start.Pressed += ()=>{if(loaded.Status!=LoadStatus.NotFound||manual.Status!=LoadStatus.NotFound)choices.Open("开始新游戏？旧档会先保留副本，不会直接抹掉。",new (string,Action)[]{("保留旧档副本并开始",BeginNew),("返回",()=>{})});else BeginNew();};
        void Continue(WorldSnapshot snapshot){session.PendingRestore=snapshot;GetTree().ChangeSceneToFile("res://scenes/Main.tscn");}
        var resume=new Button{Text=loaded.Status==LoadStatus.Loaded?"继续自动存档":"继续（暂无有效自动档）",Disabled=loaded.Status!=LoadStatus.Loaded};box.AddChild(resume);resume.Pressed+=()=>Continue(loaded.Snapshot!);
        var manualResume=new Button{Text="继续手动存档",Disabled=manual.Status!=LoadStatus.Loaded};box.AddChild(manualResume);manualResume.Pressed+=()=>Continue(manual.Snapshot!);
        if(loaded.Status is LoadStatus.Corrupt or LoadStatus.UnsupportedVersion or LoadStatus.IoError)
        {
            box.AddChild(new Label{Text=loaded.Message,AutowrapMode=TextServer.AutowrapMode.WordSmart});var backup=session.Saves.LoadBackup();
            var recover=new Button{Text="保留原档副本，恢复上次有效备份",Disabled=backup.Status!=LoadStatus.Loaded};box.AddChild(recover);recover.Pressed+=()=>{if(session.Saves.PreserveForNewGame().Success)Continue(backup.Snapshot!);};
        }
        var settings=new Button{Text="设置"};box.AddChild(settings);settings.Pressed+=preferences.Open;
        var quit=new Button{Text="退出"}; box.AddChild(quit); quit.Pressed+=()=>GetTree().Quit(); start.GrabFocus();
        var error=GetNode<GameSession>("/root/GameSession").ContentError;
        if(error.Length>0||session.Catalog==null){foreach(var button in box.GetChildren().OfType<Button>())if(button!=settings&&button!=quit)button.Disabled=true;box.AddChild(new Label{Text="内容加载失败："+error,AutowrapMode=TextServer.AutowrapMode.WordSmart});}
        if(session.FontWarning.Length>0)box.AddChild(new Label{Text=session.FontWarning});
    }
    public override void _Input(InputEvent ev){if(ev is InputEventKey{Pressed:true,Echo:false,PhysicalKeycode:Key.Escape})foreach(var settings in GetChildren().OfType<SettingsController>())if(settings.IsOpen){settings.Close();Theme=GetNode<GameSession>("/root/GameSession").CreateUiTheme();GetViewport().SetInputAsHandled();}}
    public override void _Process(double delta){var s=GetNode<GameSession>("/root/GameSession");if(Theme?.DefaultFontSize!=s.Options.SubtitleSize)Theme=s.CreateUiTheme();}
}
