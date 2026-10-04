using Godot;
using GeXingzhou.Domain;
public partial class SettingsController : Control
{
    public bool IsOpen {get;private set;}
    private FlowState source;private Control? previousFocus;
    private SpinBox font=null!;private OptionButton speed=null!;
    private CheckButton assistance=null!,motion=null!,records=null!;
    private Label message=null!;
    private const string Prefix="Panel/Scroll/Content/";
    public override void _Ready()
    {
        try
        {
            font=SceneBindings.Require<SpinBox>(this,Prefix+"FontSize");speed=SceneBindings.Require<OptionButton>(this,Prefix+"TextSpeed");
            assistance=SceneBindings.Require<CheckButton>(this,Prefix+"Assistance");motion=SceneBindings.Require<CheckButton>(this,Prefix+"ReducedMotion");
            records=SceneBindings.Require<CheckButton>(this,Prefix+"RecordEvents");message=SceneBindings.Require<Label>(this,Prefix+"Message");
            var s=GetNode<GameSession>("/root/GameSession");
            font.ValueChanged+=v=>{if(IsOpen)s.SetOptions(s.Options with {SubtitleSize=(int)v},false);};
            speed.ItemSelected+=i=>{if(IsOpen)s.SetOptions(s.Options with {TextSpeed=speed.GetItemId((int)i)},false);};
            assistance.Toggled+=v=>{if(IsOpen)s.SetOptions(s.Options with {Assistance=v},false);};
            motion.Toggled+=v=>{if(IsOpen)s.SetOptions(s.Options with {ReducedMotion=v},false);};
            records.Toggled+=v=>{if(IsOpen)s.SetOptions(s.Options with {RecordEventsEnabled=v},false);};
            SceneBindings.Require<Button>(this,Prefix+"ClearButton").Pressed+=()=>message.Text=s.Recorder.Clear().Message;
            SceneBindings.Require<Button>(this,Prefix+"ExportButton").Pressed+=()=>{
                var path=System.IO.Path.Combine(s.SaveDirectory,"exports","events-"+Guid.NewGuid()+".jsonl");
                var r=s.Recorder.Export(path);message.Text=r.Success?"已导出到："+path:r.Message;};
            SceneBindings.Require<Button>(this,Prefix+"CloseButton").Pressed+=Close;Visible=false;
        }
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    public void Open()
    {
        if(IsOpen||HasMeta("binding_error"))return;var s=GetNode<GameSession>("/root/GameSession");
        source=s.Flow;previousFocus=GetViewport().GuiGetFocusOwner();s.Flow=FlowState.Phone;
        font.Value=s.Options.SubtitleSize;
        speed.Select(s.Options.TextSpeed==0?0:s.Options.TextSpeed<=15?1:s.Options.TextSpeed<=30?2:s.Options.TextSpeed<=45?3:4);
        assistance.SetPressedNoSignal(s.Options.Assistance);motion.SetPressedNoSignal(s.Options.ReducedMotion);records.SetPressedNoSignal(s.Options.RecordEventsEnabled);
        IsOpen=true;Visible=true;GetNode<ScrollContainer>("Panel/Scroll").ScrollVertical=0;font.GetLineEdit().GrabFocus();
    }
    public void Close()
    {
        if(!IsOpen)return;var s=GetNode<GameSession>("/root/GameSession");s.SetOptions(s.Options);s.Flow=source;IsOpen=false;Visible=false;
        if(GodotObject.IsInstanceValid(previousFocus)&&previousFocus!.IsVisibleInTree())previousFocus.GrabFocus();previousFocus=null;
    }
}
