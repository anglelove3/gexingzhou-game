using Godot;
using GeXingzhou.Domain;

// Session-only presentation: never advances quests or writes hint state to saves.
public partial class GuidanceController : Node
{
    private MainView? main;private Label hint=null!;
    private bool observationShown,messageShown;private double remaining;private string current="";
    public void Configure(MainView owner){main=owner;hint=SceneBindings.Require<Label>(owner,"HUD/Guidance");}
    public override void _Process(double delta)
    {
        if(main==null)return;
        var s=GetNode<GameSession>("/root/GameSession");
        if(s.Flow==FlowState.Field)remaining=Math.Max(0,remaining-delta);
    }
    public void Refresh()
    {
        if(main==null||!GodotObject.IsInstanceValid(main.World))return;
        var s=GetNode<GameSession>("/root/GameSession");var field=s.Flow==FlowState.Field;
        var targets=main.World.GetChildren().OfType<Interactable>().ToArray();
        foreach(var target in targets)
            if(target.GetNodeOrNull<Label>("NameLabel") is {} name)
                name.Visible=field&&target.IsActive&&target.IsVisibleInTree()&&Math.Abs(target.GlobalPosition.X-main.World.Player.GlobalPosition.X)<=140;
        var nearbyObservation=field&&targets.Any(t=>t.ActionId=="observe"&&t.IsActive&&Math.Abs(t.GlobalPosition.X-main.World.Player.GlobalPosition.X)<=40);
        var invitation=s.Snapshot.Stage<=SliceStage.InvitationResolved;
        if(field&&invitation&&!messageShown&&s.Snapshot.InvitationState.VoiceReceived)
        {messageShown=true;current="Tab 打开旧手机 · 张大炮发来了消息";remaining=6;}
        else if(nearbyObservation&&!observationShown&&!s.Snapshot.CompletedActions.Contains("observation.community.first"))
        {observationShown=true;current="E 观察 · 可以看看，也可以继续走";remaining=4;}
        hint.Text=current;
        hint.Visible=field&&remaining>0&&(current.StartsWith("Tab")?invitation:nearbyObservation);
    }
}
