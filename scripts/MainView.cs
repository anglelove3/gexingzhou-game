using Godot;
using GeXingzhou.Domain;
public partial class MainView : Control
{
    public WorldView World {get;private set;}=null!;
    public DialogueController Dialogue {get;private set;}=null!;
    public PhoneController Phone {get;private set;}=null!;
    private Label prompt=null!;private Label status=null!;private SubViewport viewport=null!;
    public override void _Ready()
    {
        var container=new SubViewportContainer{Size=new Vector2(1280,720),Stretch=true,StretchShrink=2}; AddChild(container);
        viewport=new SubViewport{Size=new Vector2I(640,360),RenderTargetUpdateMode=SubViewport.UpdateMode.Always}; container.AddChild(viewport);
        World=GD.Load<PackedScene>("res://scenes/world/CommunityGate.tscn").Instantiate<WorldView>();viewport.AddChild(World);
        status=new Label{Position=new Vector2(30,24),CustomMinimumSize=new Vector2(1220,80),AutowrapMode=TextServer.AutowrapMode.WordSmart};AddChild(status);
        prompt=new Label{Position=new Vector2(30,658)};AddChild(prompt);
        Dialogue=new DialogueController();AddChild(Dialogue);Phone=new PhoneController();AddChild(Phone);
    }
    public override void _Process(double delta)
    {
        var s=GetNode<GameSession>("/root/GameSession");s.AdvanceClock(delta);prompt.Text=World.Interactions.Prompt+" · Tab 手机";
        status.Text="原型美术 / 葛行舟首段试玩\n"+GameSession.TaskText(s.Snapshot)+(s.Snapshot.InvitationState.PhoneRinging?" · 【来电】":"");
        if(s.Snapshot.InvitationState.CarArrived&&s.Snapshot.Stage<=SliceStage.InvitationResolved&&World.SceneId=="community_gate"&&World.GetNodeOrNull("Cannon")==null)
        {var t=World.AddTarget("cannon",400,"张大炮 · 见面","","invitation.meeting_complete");t.Name="Cannon";}
    }
    public void ShowNotice(string title,string body)
    {
        Dialogue.ShowText(title,body);
    }
    public void ShowDialogue(string id,Action? done=null){if(!Dialogue.Open(id,done))ShowNotice("内容提示","这段内容暂时无法加载。按E或Esc回到自由走动。");}
    public void HandleInteraction(Interactable target)
    {
        var s=GetNode<GameSession>("/root/GameSession");
        if(target.ActionId=="invitation.meeting_complete")ShowDialogue("invitation.meeting",()=>s.TryDispatch(new(target.ActionId,"meeting","invitation-1")));
        else if(target.ActionId=="candy.hey.delivered")
        {
            if(s.Snapshot.Stage==SliceStage.CandyHeyPending)ShowDialogue("hey.delivery",()=>{s.TryDispatch(new(target.ActionId,"delivered","hey-1"));ShowNotice("Hey哥","糖收好了。再按E可以聊两句，或者先去汤店。");});
            else ShowDialogue("hey.stay");
        }
        else if(target.ActionId.StartsWith("scene:")){ChangeWorld(target.ActionId[6..],new(120,280));}
        else ShowNotice(target.Caption,target.Description);
    }
    public bool ChangeWorld(string sceneId,Position2 position)
    {
        var path=sceneId=="convenience_street"?"ConvenienceStreet":"CommunityGate";
        var packed=GD.Load<PackedScene>("res://scenes/world/"+path+".tscn");if(packed==null)return false;
        var next=packed.Instantiate<WorldView>();World.Free();World=next;viewport.AddChild(World);World.Player.Position=new(position.X,position.Y);return true;
    }
    public override void _Input(InputEvent ev)
    {
        if(ev is not InputEventKey{Pressed:true,Echo:false} key)return;
        if(Phone.IsOpen){if(key.PhysicalKeycode==Key.Tab||key.PhysicalKeycode==Key.Escape)Phone.Close();else return;}
        else if(key.PhysicalKeycode==Key.Tab)Phone.Open("messages");
        else if(Dialogue.IsOpen)Dialogue.HandleKey(key.PhysicalKeycode);
        else return;
        GetViewport().SetInputAsHandled();
    }
}
