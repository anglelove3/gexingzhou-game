using Godot;
using GeXingzhou.Domain;
public partial class MainView : Control
{
    public WorldView World {get;private set;}=null!;
    public DialogueController Dialogue {get;private set;}=null!;
    public PhoneController Phone {get;private set;}=null!;
    public ChoiceController Choices {get;private set;}=null!;
    private Label prompt=null!;private Label status=null!;private SubViewport viewport=null!;
    public override void _Ready()
    {
        var container=new SubViewportContainer{Size=new Vector2(1280,720),Stretch=true,StretchShrink=2}; AddChild(container);
        viewport=new SubViewport{Size=new Vector2I(640,360),RenderTargetUpdateMode=SubViewport.UpdateMode.Always}; container.AddChild(viewport);
        World=GD.Load<PackedScene>("res://scenes/world/CommunityGate.tscn").Instantiate<WorldView>();viewport.AddChild(World);
        status=new Label{Position=new Vector2(30,24),CustomMinimumSize=new Vector2(1220,80),AutowrapMode=TextServer.AutowrapMode.WordSmart};AddChild(status);
        prompt=new Label{Position=new Vector2(30,658)};AddChild(prompt);
        Dialogue=new DialogueController();AddChild(Dialogue);Choices=new ChoiceController();AddChild(Choices);Phone=new PhoneController();AddChild(Phone);
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
            if(s.Snapshot.Stage==SliceStage.CandyHeyPending)ShowDialogue("hey.delivery",()=>{s.TryDispatch(new(target.ActionId,"delivered","hey-1"));OfferHey();});
            else if(s.Snapshot.Stage>=SliceStage.CandyHeyDelivered)OfferHey();else ShowNotice("Hey哥","先去小区门口找张大炮吧。");
        }
        else if(target.ActionId=="soup.meet")
        {
            if(s.Snapshot.Stage==SliceStage.CandyHeyDelivered)s.TryDispatch(new("soup.meet","sit","soup-seat-1"));
            if(s.Snapshot.Stage==SliceStage.SoupMeet)
            {if(s.Snapshot.ChoiceCodes.ContainsKey("soup-response-1"))ShowMemoryEntry();else ShowDialogue("soup.start",OfferSoup);}
            else ShowNotice("桌边","先把张大炮托你的喜糖交给Hey哥。");
        }
        else if(target.ActionId.StartsWith("scene:")){var id=target.ActionId[6..];if(id=="soup_shop"&&s.Snapshot.Stage<SliceStage.CandyHeyDelivered)ShowNotice("去汤店之前","先把喜糖送到Hey哥手里，别让他等着。");else ChangeWorld(id,new(120,280));}
        else ShowNotice(target.Caption,target.Description);
    }
    private void OfferHey()=>Choices.Open("糖已经收好。接下来你怎么做？",new (string,Action)[]{("收起手机，站一会儿",()=>ShowDialogue("hey.stay")),("看一眼手机",()=>ShowDialogue("hey.phone")),("转身去汤店",()=>ShowDialogue("hey.leave"))});
    private void OfferSoup()
    {
        var options=new List<(string,Action)>();
        foreach(var (code,caption) in new[]{("eat","先吃一口汤"),("set_chopsticks","替他摆好筷子"),("check_phone","看一眼旧手机")})
            options.Add((caption,()=>{var s=GetNode<GameSession>("/root/GameSession");if(s.TryDispatch(new("soup.response",code,"soup-response-1")).Applied)ShowDialogue("soup."+code,ShowMemoryEntry);}));
        Choices.Open("张大炮：最近怎么样？你可以用行动回答。",options);
    }
    private void ShowMemoryEntry()=>ShowNotice("汤气里的清晨","想起那年网吧包夜回来，我们在这儿吃鸭血粉丝汤。回忆互动下一步接入。");
    public bool ChangeWorld(string sceneId,Position2 position)
    {
        var path=sceneId switch{"convenience_street"=>"ConvenienceStreet","community_gate"=>"CommunityGate","soup_shop"=>"SoupShop",_=>null};if(path==null)return false;
        var packed=GD.Load<PackedScene>("res://scenes/world/"+path+".tscn");if(packed==null)return false;
        var next=packed.Instantiate<WorldView>();World.Free();World=next;viewport.AddChild(World);World.Player.Position=new(position.X,position.Y);return true;
    }
    public override void _Input(InputEvent ev)
    {
        if(ev is not InputEventKey{Pressed:true,Echo:false} key)return;
        if(Phone.IsOpen){if(key.PhysicalKeycode==Key.Tab||key.PhysicalKeycode==Key.Escape)Phone.Close();else return;}
        else if(key.PhysicalKeycode==Key.Tab)Phone.Open("messages");
        else if(Choices.IsOpen){if(key.PhysicalKeycode is Key.Escape or Key.E)Choices.HandleKey(key.PhysicalKeycode);else return;}
        else if(Dialogue.IsOpen)Dialogue.HandleKey(key.PhysicalKeycode);
        else return;
        GetViewport().SetInputAsHandled();
    }
}
