using Godot;
using GeXingzhou.Domain;
public partial class PhoneController : Control
{
    private FlowState source;
    private Label contact=null!,messages=null!,task=null!;
    private Button answer=null!,close=null!;
    private Control? previousFocus;
    public bool IsOpen {get;private set;}
    public override void _Ready()
    {
        try
        {
            contact=SceneBindings.Require<Label>(this,"Frame/Content/Contact");
            messages=SceneBindings.Require<Label>(this,"Frame/Content/MessageScroll/Messages");
            task=SceneBindings.Require<Label>(this,"Frame/Content/Task");
            answer=SceneBindings.Require<Button>(this,"Frame/Content/AnswerButton");
            close=SceneBindings.Require<Button>(this,"Frame/Content/CloseButton");
            answer.Pressed+=Answer;close.Pressed+=Close;Visible=false;
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
        IsOpen=true;Visible=true;s.Flow=FlowState.Phone;
        var inv=s.Snapshot.InvitationState;
        contact.Text=inv.PhoneRinging?"张大炮 · 来电":"旧手机 · 消息与任务";
        messages.Text=inv.VoiceReceived?"张大炮：葛大爷，别装死，我要结婚了！等会儿来找你。":"暂无新消息。";
        task.Text="当前任务："+GameSession.TaskText(s.Snapshot);
        answer.Visible=inv.PhoneRinging;if(answer.Visible)answer.GrabFocus();else close.GrabFocus();
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
        if(GodotObject.IsInstanceValid(previousFocus)&&previousFocus!.IsVisibleInTree())previousFocus.GrabFocus();
        previousFocus=null;
    }
    private MainView? FindMain(){for(Node? p=GetParent();p!=null;p=p.GetParent())if(p is MainView m)return m;return null;}
}
