using Godot;
using GeXingzhou.Domain;
public partial class PhoneController : Control
{
    private FlowState source;
    private Label contact=null!,messages=null!,task=null!,empty=null!,callNotice=null!;
    private Control incoming=null!,outgoing=null!;
    private PanelContainer frame=null!;private ScrollContainer messageScroll=null!;
    [Export(PropertyHint.Range,"360,520,1")] public float HandsetWidth {get;set;}=430;
    [Export(PropertyHint.Range,"600,900,1")] public float HandsetMaxHeight {get;set;}=760;
    private Button answer=null!,close=null!;
    private Control? previousFocus;
    public bool IsOpen {get;private set;}
    public override void _Ready()
    {
        try
        {
            contact=SceneBindings.Require<Label>(this,"Frame/Content/Contact");
            frame=SceneBindings.Require<PanelContainer>(this,"Frame");
            messageScroll=SceneBindings.Require<ScrollContainer>(this,"Frame/Content/MessageScroll");
            incoming=SceneBindings.Require<Control>(this,"Frame/Content/MessageScroll/Messages/Incoming");
            outgoing=SceneBindings.Require<Control>(this,"Frame/Content/MessageScroll/Messages/Outgoing");
            empty=SceneBindings.Require<Label>(this,"Frame/Content/MessageScroll/Messages/Empty");
            callNotice=SceneBindings.Require<Label>(this,"Frame/Content/MessageScroll/Messages/CallNotice");
            messages=SceneBindings.Require<Label>(this,"Frame/Content/MessageScroll/Messages/Incoming/Bubble/Text");
            task=SceneBindings.Require<Label>(this,"Frame/Content/Task");
            answer=SceneBindings.Require<Button>(this,"Frame/Content/AnswerButton");
            close=SceneBindings.Require<Button>(this,"Frame/Content/CloseButton");
            answer.Pressed+=Answer;close.Pressed+=Close;Resized+=LayoutHandset;LayoutHandset();Visible=false;
        }
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    public void Open(string pageId)
    {
        if(IsOpen||HasMeta("binding_error"))return;
        var s=GetNode<GameSession>("/root/GameSession");source=s.Flow;
        if(source==FlowState.Transition)return;
        previousFocus=GetViewport().GuiGetFocusOwner();
        var main=FindMain();if(main?.Dialogue.IsOpen==true)main.Dialogue.Visible=false;
        main?.Rest.Suspend();
        IsOpen=true;Visible=true;s.Flow=FlowState.Phone;
        var inv=s.Snapshot.InvitationState;
        contact.Text="张大炮";
        incoming.Visible=inv.VoiceReceived;empty.Visible=!inv.VoiceReceived;
        messages.Text="葛大爷，别装死，我要结婚了！等会儿来找你。";
        outgoing.Visible=inv.Resolution==InvitationResolution.Answered;
        callNotice.Visible=inv.PhoneRinging||inv.Resolution==InvitationResolution.Arrived;
        callNotice.Text=inv.PhoneRinging?"语音通话 · 正在呼叫你…":"未接听语音通话";
        task.Text="行程已记下，收起手机后按J查看";
        messageScroll.ScrollVertical=0;
        answer.Visible=inv.PhoneRinging;if(answer.Visible)answer.GrabFocus();else close.GrabFocus();
    }
    private void LayoutHandset()
    {
        if(frame==null)return;
        var height=Math.Min(HandsetMaxHeight,Size.Y-40);
        var width=Math.Min(HandsetWidth,Math.Min(Size.X-40,height/1.5f));
        frame.OffsetLeft=-width/2;frame.OffsetRight=width/2;
        frame.OffsetTop=-height/2;frame.OffsetBottom=height/2;
    }
    public override void _UnhandledKeyInput(InputEvent ev)
    {
        if(!IsOpen||ev is not InputEventKey{Pressed:true} key)return;
        var code=key.PhysicalKeycode;
        var page=Math.Max(32,(int)messageScroll.Size.Y-16);
        if(code==Key.Pagedown)messageScroll.ScrollVertical+=page;
        else if(code==Key.Pageup)messageScroll.ScrollVertical-=page;
        else if(code==Key.Home)messageScroll.ScrollVertical=0;
        else if(code==Key.End)messageScroll.ScrollVertical=int.MaxValue;
        else return;
        GetViewport().SetInputAsHandled();
    }
    private void Answer()
    {
        var s=GetNode<GameSession>("/root/GameSession");
        if(!s.TryDispatch(new("invitation.answer","answered","invitation-1")).Applied)return;
        Close();FindMain()?.ShowDialogue("invitation.answer");
    }
    public void Close()
    {
        if(!IsOpen)return;IsOpen=false;Visible=false;
        GetNode<GameSession>("/root/GameSession").Flow=source;
        var main=FindMain();if(main?.Dialogue.IsOpen==true)main.Dialogue.Visible=true;
        if(source==FlowState.Field)main?.Rest.Resume();
        if(GodotObject.IsInstanceValid(previousFocus)&&previousFocus!.IsVisibleInTree())previousFocus.GrabFocus();
        previousFocus=null;
    }
    private MainView? FindMain(){for(Node? p=GetParent();p!=null;p=p.GetParent())if(p is MainView m)return m;return null;}
}
