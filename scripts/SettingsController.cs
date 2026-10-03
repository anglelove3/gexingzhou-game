using Godot;
using GeXingzhou.Domain;
public partial class SettingsController : Control
{
    public bool IsOpen {get;private set;}private FlowState source;private PanelContainer? panel;
    public void Open()
    {
        if(IsOpen)return;var s=GetNode<GameSession>("/root/GameSession");source=s.Flow;s.Flow=FlowState.Phone;IsOpen=true;
        panel=new PanelContainer{Position=new Vector2(100,70),Size=new Vector2(1080,580)};panel.AddThemeStyleboxOverride("panel",UiStyles.Panel());AddChild(panel);var box=new VBoxContainer{CustomMinimumSize=new Vector2(1040,540)};panel.AddChild(box);
        box.AddChild(new Label{Text="设置 · Esc 返回 · ↑↓ / Tab切换 · Enter确认"});
        var font=new SpinBox{MinValue=20,MaxValue=32,Step=1,Value=s.Options.SubtitleSize};box.AddChild(new Label{Text="字体大小（20—32）"});box.AddChild(font);font.ValueChanged+=value=>s.SetOptions(s.Options with {SubtitleSize=(int)value},false);
        var speed=new OptionButton();foreach(var n in new[]{0,15,30,45,60})speed.AddItem(n==0?"字幕：立即显示":$"字幕：每秒{n}字",n);speed.Select(s.Options.TextSpeed==0?0:s.Options.TextSpeed<=15?1:s.Options.TextSpeed<=30?2:s.Options.TextSpeed<=45?3:4);box.AddChild(speed);speed.ItemSelected+=index=>s.SetOptions(s.Options with {TextSpeed=speed.GetItemId((int)index)},false);
        void Toggle(string title,bool initial,Action<bool> change){var check=new CheckButton{Text=title,ButtonPressed=initial};box.AddChild(check);check.Toggled+=value=>change(value);}
        Toggle("操作辅助：增加文字提示（不改变选择）",s.Options.Assistance,v=>s.SetOptions(s.Options with {Assistance=v},false));
        Toggle("减少动效（转场0.15秒）",s.Options.ReducedMotion,v=>s.SetOptions(s.Options with {ReducedMotion=v},false));
        Toggle("本地匿名行为记录（默认关闭，不上传）",s.Options.RecordEventsEnabled,v=>s.SetOptions(s.Options with {RecordEventsEnabled=v},false));
        var message=new Label{AutowrapMode=TextServer.AutowrapMode.WordSmart};box.AddChild(message);
        var clear=new Button{Text="清空本地行为记录（不删除存档）"};box.AddChild(clear);clear.Pressed+=()=>message.Text=s.Recorder.Clear().Message;
        var export=new Button{Text="导出允许的事件字段到本机"};box.AddChild(export);export.Pressed+=()=>{var path=System.IO.Path.Combine(s.SaveDirectory,"exports","events-"+Guid.NewGuid()+".jsonl");var r=s.Recorder.Export(path);message.Text=r.Success?"已导出到："+path:r.Message;};
        var close=new Button{Text="保存设置并返回"};box.AddChild(close);close.Pressed+=Close;font.GetLineEdit().GrabFocus();
    }
    public void Close(){if(!IsOpen)return;var s=GetNode<GameSession>("/root/GameSession");s.SetOptions(s.Options);s.Flow=source;panel?.QueueFree();panel=null;IsOpen=false;}
}
