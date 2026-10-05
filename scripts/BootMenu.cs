using Godot;
using GeXingzhou.Domain;
public partial class BootMenu : Control
{
    public AudioDirector Audio {get;private set;}=null!;
    public override void _Ready()
    {
        try
        {
            Audio=SceneBindings.Require<AudioDirector>(this,"Audio");
            var start=SceneBindings.Require<Button>(this,"Menu/StartButton");
            var resume=SceneBindings.Require<Button>(this,"Menu/AutoResumeButton");
            var manualResume=SceneBindings.Require<Button>(this,"Menu/ManualResumeButton");
            var recover=SceneBindings.Require<Button>(this,"Menu/RecoveryButton");
            var settings=SceneBindings.Require<Button>(this,"Menu/SettingsButton");
            var quit=SceneBindings.Require<Button>(this,"Menu/QuitButton");
            var message=SceneBindings.Require<Label>(this,"Menu/Message");
            var preferences=SceneBindings.Require<SettingsController>(this,"Settings");
            var choices=SceneBindings.Require<ChoiceController>(this,"Choices");
            var s=GetNode<GameSession>("/root/GameSession");var loaded=s.Saves.Load();var manual=s.ManualSaves.Load();
            Theme=s.CreateUiTheme();
            void BeginNew()
            {
                var a=s.Saves.PreserveForNewGame();var m=s.ManualSaves.PreserveForNewGame();
                if(!a.Success||!m.Success){message.Text=a.Message+"\n"+m.Message;return;}
                s.NewGame();GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
            }
            start.Pressed+=()=>{if(loaded.Status!=LoadStatus.NotFound||manual.Status!=LoadStatus.NotFound)
                choices.Open("开始新游戏？旧档会先保留副本，不会直接抹掉。",new (string,Action)[]{("保留旧档副本并开始",BeginNew),("返回",()=>{})});else BeginNew();};
            void Continue(WorldSnapshot snapshot){s.PendingRestore=snapshot;GetTree().ChangeSceneToFile("res://scenes/Main.tscn");}
            resume.Disabled=loaded.Status!=LoadStatus.Loaded;
            resume.Text=resume.Disabled?"继续（暂无有效自动档）":"继续自动存档";resume.Pressed+=()=>Continue(loaded.Snapshot!);
            manualResume.Disabled=manual.Status!=LoadStatus.Loaded;manualResume.Pressed+=()=>Continue(manual.Snapshot!);
            recover.Visible=loaded.Status is LoadStatus.Corrupt or LoadStatus.UnsupportedVersion or LoadStatus.IoError;
            if(recover.Visible)
            {
                message.Text=loaded.Message;var backup=s.Saves.LoadBackup();recover.Disabled=backup.Status!=LoadStatus.Loaded;
                recover.Pressed+=()=>{if(s.Saves.PreserveForNewGame().Success)Continue(backup.Snapshot!);};
            }
            settings.Pressed+=preferences.Open;quit.Pressed+=()=>GetTree().Quit();
            if(s.ContentError.Length>0||s.Catalog==null)
            {
                foreach(var b in new[]{start,resume,manualResume,recover})b.Disabled=true;
                message.Text="内容加载失败："+s.ContentError;
            }
            if(s.FontWarning.Length>0)message.Text+="\n"+s.FontWarning;
            if(start.Disabled)settings.GrabFocus();else start.GrabFocus();
        }
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    public override void _Input(InputEvent ev)
    {
        if(ev is not InputEventKey{Pressed:true,Echo:false} key)return;
        var settings=GetNode<SettingsController>("Settings");var choices=GetNode<ChoiceController>("Choices");
        if(settings.IsOpen&&key.PhysicalKeycode==Key.Escape){settings.Close();GetViewport().SetInputAsHandled();}
        else if(choices.IsOpen&&key.PhysicalKeycode is Key.Escape or Key.E){choices.HandleKey(key.PhysicalKeycode);GetViewport().SetInputAsHandled();}
    }
    public override void _Process(double delta)
    {
        var s=GetNode<GameSession>("/root/GameSession");if(Theme?.DefaultFontSize!=s.Options.SubtitleSize)Theme=s.CreateUiTheme();
    }
}
