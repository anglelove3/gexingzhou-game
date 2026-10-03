using Godot;
using GeXingzhou.Domain;
public partial class PhoneController : Control
{
    private FlowState source;private VBoxContainer box=null!;private Label body=null!;private Button answer=null!;
    public bool IsOpen {get;private set;}
    public override void _Ready()
    {
        AddChild(UiStyles.Dim());var panel=new PanelContainer{Position=new Vector2(230,70),Size=new Vector2(820,570)};panel.AddThemeStyleboxOverride("panel",UiStyles.Panel());AddChild(panel);
        box=new VBoxContainer{CustomMinimumSize=new Vector2(760,530)};panel.AddChild(box);
        body=new Label{AutowrapMode=TextServer.AutowrapMode.WordSmart,CustomMinimumSize=new Vector2(740,320)};box.AddChild(body);
        answer=new Button{Text="接听张大炮的电话"};box.AddChild(answer);answer.Pressed+=Answer;
        var close=new Button{Text="收起手机（Tab / Esc）"};box.AddChild(close);close.Pressed+=Close;Visible=false;
    }
    public void Open(string pageId)
    {
        if(IsOpen)return;var s=GetNode<GameSession>("/root/GameSession");source=s.Flow;if(source==FlowState.Transition)return;
        IsOpen=true;Visible=true;s.Flow=FlowState.Phone;var inv=s.Snapshot.InvitationState;
        body.Text="旧手机 · 消息与任务\n\n"+(inv.VoiceReceived?"张大炮：葛大爷，别装死，我要结婚了！等会儿来找你。":"暂无新消息。")+"\n\n"+(inv.PhoneRinging?"【张大炮来电】可以接，也可以收起手机。":"")+"\n\n当前任务："+GameSession.TaskText(s.Snapshot);
        answer.Visible=inv.PhoneRinging;if(answer.Visible)answer.GrabFocus();else box.GetChild<Button>(2).GrabFocus();
    }
    private void Answer(){var s=GetNode<GameSession>("/root/GameSession");if(!s.TryDispatch(new("invitation.answer","answered","invitation-1")).Applied)return;Close();FindMain()?.ShowDialogue("invitation.answer");}
    public void Close(){if(!IsOpen)return;IsOpen=false;Visible=false;GetNode<GameSession>("/root/GameSession").Flow=source;}
    private MainView? FindMain(){for(Node? p=GetParent();p!=null;p=p.GetParent())if(p is MainView m)return m;return null;}
}
