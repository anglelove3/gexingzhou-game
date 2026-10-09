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
            var settings=SceneBindings.Require<Button>(this,"Menu/SettingsButton");
            var quit=SceneBindings.Require<Button>(this,"Menu/QuitButton");
            var message=SceneBindings.Require<Label>(this,"Menu/Message");
            var preferences=SceneBindings.Require<SettingsController>(this,"Settings");
            var choices=SceneBindings.Require<ChoiceController>(this,"Choices");
            var s=GetNode<GameSession>("/root/GameSession");Theme=s.CreateUiTheme();Audio.SetScene("boot");
            void Continue(WorldSnapshot snapshot){s.PendingRestore=snapshot;GetTree().ChangeSceneToFile("res://scenes/Main.tscn");}
            void Upgrade(bool manual,bool backup)
            {
                choices.Open("将旧版进度复制到二维探索版本？旧文件只读保留，汤店站位会适配新场景。",new (string,Action)[]{
                    ("复制并继续",()=>{var loaded=s.TryUpgradeLegacy(manual,backup);message.Text=loaded.Message;if(loaded.Status==LoadStatus.Loaded)Continue(loaded.Snapshot!);}),
                    ("返回",()=>{})});
            }
            void BindSlot(bool manual)
            {
                var resume=SceneBindings.Require<Button>(this,"Menu/"+(manual?"ManualResumeButton":"AutoResumeButton"));
                var upgrade=SceneBindings.Require<Button>(this,"Menu/"+(manual?"LegacyManualUpgradeButton":"LegacyAutoUpgradeButton"));
                var recovery=SceneBindings.Require<Button>(this,"Menu/"+(manual?"ManualRecoveryButton":"RecoveryButton"));
                var offer=s.ProbeResume(manual);var backup=s.ProbeResume(manual,true);var slot=manual?"手动":"自动";
                resume.Disabled=offer.Source!=ResumeSource.V2;resume.Visible=offer.Source!=ResumeSource.Legacy;
                resume.Text=resume.Disabled?"继续（暂无有效"+slot+"档）":"继续"+slot+"存档";
                resume.Pressed+=()=>{var fresh=s.ProbeResume(manual);if(fresh.Source==ResumeSource.V2)Continue(fresh.Result.Snapshot!);else message.Text=fresh.Result.Message;};
                upgrade.Visible=offer.Source==ResumeSource.Legacy;upgrade.Pressed+=()=>Upgrade(manual,false);
                recovery.Visible=(offer.Source is ResumeSource.Blocked or ResumeSource.Missing)&&backup.Result.Status==LoadStatus.Loaded;
                recovery.Disabled=backup.Result.Status!=LoadStatus.Loaded;
                recovery.Text=backup.Source==ResumeSource.Legacy?"复制旧"+slot+"档备份并继续":"保留原"+slot+"档副本，恢复备份";
                recovery.Pressed+=()=>{
                    var fresh=s.ProbeResume(manual,true);
                    if(fresh.Source==ResumeSource.Legacy){Upgrade(manual,true);return;}
                    if(fresh.Source!=ResumeSource.V2){message.Text=fresh.Result.Message;return;}
                    var repository=manual?s.ManualSaves:s.Saves;var preserved=repository.PreserveForNewGame();
                    if(!preserved.Success){message.Text=preserved.Message;return;}
                    var snapshot=fresh.Result.Snapshot! with{Settings=s.Options};var saved=repository.Save(snapshot);
                    message.Text=saved.Message;if(saved.Success)Continue(snapshot);
                };
                if(offer.Source==ResumeSource.Blocked)message.Text+=slot+"档："+offer.Result.Message+"\n";
            }
            BindSlot(false);BindSlot(true);
            void BeginNew()
            {
                var a=s.Saves.PreserveForNewGame();var m=s.ManualSaves.PreserveForNewGame();
                if(!a.Success||!m.Success){message.Text=a.Message+"\n"+m.Message;return;}
                s.NewGame();GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
            }
            start.Pressed+=()=>{
                var a=s.ProbeResume(false);var m=s.ProbeResume(true);
                if(a.Source is ResumeSource.V2 or ResumeSource.Blocked||m.Source is ResumeSource.V2 or ResumeSource.Blocked)
                    choices.Open("开始新游戏？当前版本两槽会先保留副本，旧版文件不会改动。",new (string,Action)[]{("保留副本并开始",BeginNew),("返回",()=>{})});else BeginNew();
            };
            settings.Pressed+=preferences.Open;quit.Pressed+=()=>GetTree().Quit();
            if(s.ContentError.Length>0||s.Catalog==null)
            {
                foreach(var b in GetNode("Menu").GetChildren().OfType<Button>())if(b!=settings&&b!=quit)b.Disabled=true;
                message.Text="内容加载失败："+s.ContentError;
            }
            if(s.FontWarning.Length>0)message.Text+="\n"+s.FontWarning;
            if(s.EventWarning.Length>0)message.Text+="\n"+s.EventWarning;
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
