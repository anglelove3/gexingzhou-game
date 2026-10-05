using Godot;
using GeXingzhou.Domain;
public partial class MainView : Control
{
    public WorldView World {get;private set;}=null!;
    public DialogueController Dialogue {get;private set;}=null!;
    public PhoneController Phone {get;private set;}=null!;
    public ChoiceController Choices {get;private set;}=null!;
    public SceneFlow SceneFlow {get;private set;}=null!;
    public SoupMemoryController? Memory {get;private set;}
    public SettingsController Settings {get;private set;}=null!;private int lastFont;
    public RestController Rest {get;private set;}=null!;
    public int PaymentFeedbackCount {get;private set;}
    private Label prompt=null!;private Label status=null!;private SubViewport viewport=null!;
    private WorldDisplayController display=null!;
    public override void _Ready()
    {
        try
        {
            viewport=SceneBindings.Require<SubViewport>(this,"WorldDisplay/WorldViewport");
            display=SceneBindings.Require<WorldDisplayController>(this,"WorldDisplay");
            Dialogue=SceneBindings.Require<DialogueController>(this,"Dialogue");
            Phone=SceneBindings.Require<PhoneController>(this,"Phone");
            Choices=SceneBindings.Require<ChoiceController>(this,"Choices");
            Settings=SceneBindings.Require<SettingsController>(this,"Settings");
            Rest=SceneBindings.Require<RestController>(this,"Rest");
            SceneFlow=SceneBindings.Require<SceneFlow>(this,"SceneFlow");SceneFlow.Main=this;
            status=SceneBindings.Require<Label>(this,"HUD/TaskCard/TaskText");
            prompt=SceneBindings.Require<Label>(this,"HUD/InteractionHint");
            if(FindChildren("*","",true,false).Any(n=>n.HasMeta("binding_error")))
                throw new InvalidOperationException("界面或世界引用不完整，请查看场景错误提示。");
            var s=GetNode<GameSession>("/root/GameSession");var candidate=s.PendingRestore??s.Snapshot;
            var worldScene=candidate.SceneId=="memory_soup_table"?"soup_shop":candidate.SceneId;
            World=viewport.GetChildren().OfType<WorldView>().Single();
            if(World.SceneId!=worldScene&&!ChangeWorld(worldScene,new(candidate.PlayerPosition.X,candidate.PlayerPosition.Y)))
                throw new InvalidOperationException("无法加载存档中的场景。");
            if(World.HasMeta("binding_error"))throw new InvalidOperationException(World.GetMeta("binding_error").AsString());
            if(s.PendingRestore!=null)
            {
                World.Player.Position=candidate.SceneId=="memory_soup_table"?new Vector2(candidate.ReturnContext!.Position.X,candidate.ReturnContext.Position.Y):new(candidate.PlayerPosition.X,candidate.PlayerPosition.Y);
                if(candidate.SceneId=="memory_soup_table"&&!EnterMemoryView()){ShowNotice("继续失败","回忆场景无法加载，原档保留。请返回菜单重试。");return;}
                if(!s.Restore(candidate).Success){ShowNotice("继续失败","存档内容无效，原档保留。");return;}
                if(candidate.SceneId=="memory_soup_table"&&candidate.MemoryState!.Completed)_=ReturnMemory(true);
            }
            Theme=s.CreateUiTheme();lastFont=s.Options.SubtitleSize;
            display.Configure(World);
        }
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    public override void _Process(double delta)
    {
        var s=GetNode<GameSession>("/root/GameSession");s.AdvanceClock(delta);
        prompt.Text=(Rest.IsActive?(GetNode<RestOptionsController>("RestOptions").IsOpen?"Esc 收起选项":"E 休息选项 · Esc 起身"):World.Interactions.Prompt)+" · Tab 手机";
        if(lastFont!=s.Options.SubtitleSize){lastFont=s.Options.SubtitleSize;Theme=s.CreateUiTheme();}
        if(s.Options.Assistance)prompt.Text+=" · ←→移动，靠近金色标记按E";
        prompt.Visible=s.Flow==FlowState.Field;
        var place=World.SceneId switch{"soup_shop"=>"鸭血粉丝汤店","convenience_street"=>"便利店街",_=>"安置小区"};
        status.Text="葛行舟 · "+place+"\n"+GameSession.TaskText(s.Snapshot)+(s.Snapshot.InvitationState.PhoneRinging?" · 【来电】":"")+(s.SaveMessage.Length>0?"\n"+s.SaveMessage:"");
        World.RefreshQuestActors(s.Snapshot);
    }
    public void ShowNotice(string title,string body)
    {
        Dialogue.ShowText(title,body);
    }
    public void ShowDialogue(string id,Action? done=null){if(!Dialogue.Open(id,done))ShowNotice("内容提示","这段内容暂时无法加载。按E或Esc回到自由走动。");}
    public void HandleInteraction(Interactable target)
    {
        var s=GetNode<GameSession>("/root/GameSession");
        if(target is BenchView bench&&target.ActionId=="rest.bench"){Rest.Begin(bench);return;}
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
            else if(s.Snapshot.Stage==SliceStage.MemoryActive)ShowMemoryEntry();
            else if(s.Snapshot.Stage==SliceStage.MemoryReturned)ShowSoupReturn("soup.return");
            else if(s.Snapshot.Stage==SliceStage.SliceComplete)ShowMemoryEntry(true);
            else ShowNotice("桌边","先把张大炮托你的喜糖交给Hey哥。");
        }
        else if(target.ActionId.StartsWith("scene:")){var id=target.ActionId[6..];if(id=="soup_shop"&&s.Snapshot.Stage<SliceStage.CandyHeyDelivered)ShowNotice("去汤店之前","先把喜糖送到Hey哥手里，别让他等着。");else _=SceneFlow.TryEnter(id,new(120,280));}
        else if(target.ActionId=="observe")ShowObservation(target);
        else ShowNotice(target.Caption,target.Description);
    }
    public void ShowObservation(Interactable target)
    {
        var s=GetNode<GameSession>("/root/GameSession");
        if(World.SceneId=="community_gate"&&!s.Snapshot.CompletedActions.Contains("observation.community.first"))
        {
            var id=s.Snapshot.InvitationState.Resolution==InvitationResolution.Answered?"observation.community.answered":s.Snapshot.InvitationState.VoiceReceived?"observation.community.unanswered":"observation.community.quiet";
            if(s.Catalog!.Dialogues.TryGetValue(id,out var node)&&node is not null)
                Dialogue.ShowText("葛行舟",target.Description+"\n"+string.Join("\n",node.Lines),s.MarkFirstCommunityObservation,DialogueLineKind.Thought);
            else ShowNotice(target.Caption,target.Description);
        }
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
    private void ShowMemoryEntry()=>ShowMemoryEntry(false);
    private void ShowMemoryEntry(bool replay)=>Choices.Open("汤气里的清晨 · 那年网吧包夜回来",new (string,Action)[]{(replay?"主动重看（不改写历史）":"想起那碗汤",async()=>await BeginMemory(replay)),("暂时离开",()=>{})});
    private async Task BeginMemory(bool replay)
    {
        var s=GetNode<GameSession>("/root/GameSession");var context=s.Snapshot.ReturnContext??new SceneReturnContext("soup_shop",new(World.Player.Position.X,World.Player.Position.Y),"soup.return",true);
        var result=await SceneFlow.TryEnter("memory_soup_table",new(320,280));if(result.Success)s.BeginMemory(context,replay);else ShowNotice("切场失败","回忆暂时无法打开，当前进度没有推进。");
    }
    public bool EnterMemoryView()
    {
        var packed=GD.Load<PackedScene>("res://scenes/world/MemorySoupTable.tscn");if(packed==null)return false;
        Memory=packed.Instantiate<SoupMemoryController>();Memory.Main=this;AddChild(Memory);World.Visible=false;return true;
    }
    public async Task ReturnMemory(bool complete)
    {
        var s=GetNode<GameSession>("/root/GameSession");if(s.Snapshot.ReturnContext is not {} context)return;
        var result=await SceneFlow.TryReturn(context);if(!result.Success){ShowNotice("切场失败","返回暂时失败，回忆进度保留。请重试。");return;}
        if(Memory!=null){Memory.Free();Memory=null;}World.Visible=true;
        s.RecordMemoryReturned();
        s.SaveCheckpoint();
        if(complete&&s.Snapshot.MemoryState is {} m){s.TryDispatch(new("memory.return","return",m.InstanceId));if(!m.Replay)ShowSoupReturn(context.DialogueNodeId);else ShowNotice("回忆结束","过去没有被改写。你仍坐在现实的汤店里。");}
    }
    private void ShowSoupReturn(string fallbackId)
    {
        var memory=GetNode<GameSession>("/root/GameSession").Snapshot.MemoryState;
        var id=fallbackId=="soup.return"&&memory is {Completed:true,Replay:false}?memory.FoodChoice switch
        {
            FoodChoice.Take=>"soup.return.take",FoodChoice.Wait=>"soup.return.wait",FoodChoice.Share=>"soup.return.share",_=>fallbackId
        }:fallbackId;
        ShowDialogue(id,FinishSlice);
    }
    private void FinishSlice()
    {
        var s=GetNode<GameSession>("/root/GameSession");
        if(s.TryDispatch(new("soup.payment","zhang_pays","soup-payment-1")).Applied)
        {
            PaymentFeedbackCount++;
            var feedback=new Label{Text="张大炮把钱压在碗边：这顿我来。",Position=new(560,220),MouseFilter=MouseFilterEnum.Ignore};AddChild(feedback);
            var coin=new ColorRect{Color=Colors.Gold,Position=new(580,300),Size=new(18,18),MouseFilter=MouseFilterEnum.Ignore};AddChild(coin);
            var tween=CreateTween();tween.TweenProperty(coin,"position",new Vector2(800,300),s.Options.FadeDuration);
            tween.TweenInterval(.6);tween.TweenCallback(Callable.From(()=>{coin.QueueFree();feedback.QueueFree();}));
        }
        ShowDialogue("soup.tomorrow",()=>s.TryDispatch(new("slice.complete","completed","slice-1")));
    }
    public bool ChangeWorld(string sceneId,Position2 position)
    {
        var path=ScenePath(sceneId);if(path==null)return false;
        var packed=GD.Load<PackedScene>("res://scenes/world/"+path+".tscn");if(packed==null)return false;
        Rest?.Cancel();
        var next=packed.Instantiate<WorldView>();World.Free();World=next;viewport.AddChild(World);
        if(World.HasMeta("binding_error")){SceneBindings.ReportFailure(this,World.GetMeta("binding_error").AsString());return false;}
        World.Player.Position=new(position.X,position.Y);display.Configure(World);GetNode<GameSession>("/root/GameSession").UpdateScene(sceneId,position);return true;
    }
    private static string? ScenePath(string id)=>id switch{"convenience_street"=>"ConvenienceStreet","community_gate"=>"CommunityGate","soup_shop"=>"SoupShop",_=>null};
    public override void _Input(InputEvent ev)
    {
        if(ev is not InputEventKey{Pressed:true,Echo:false} key)return;
        if(GetNode<GameSession>("/root/GameSession").Flow==FlowState.Transition){GetViewport().SetInputAsHandled();return;}
        if(Settings.IsOpen){if(key.PhysicalKeycode==Key.Escape)Settings.Close();else return;}
        else if(key.PhysicalKeycode==Key.F5){var s=GetNode<GameSession>("/root/GameSession");if(Rest.IsActive)s.UpdatePosition(Rest.SavePosition);s.SaveManual();}
        else if(key.PhysicalKeycode==Key.F9){var s=GetNode<GameSession>("/root/GameSession");if(Phone.IsOpen)Phone.Close();Rest.Cancel();var loaded=s.ManualSaves.Load();if(loaded.Status==LoadStatus.Loaded){s.PendingRestore=loaded.Snapshot;GetTree().ChangeSceneToFile("res://scenes/Main.tscn");}else ShowNotice("手动存档",loaded.Message);}
        else if(Phone.IsOpen){if(key.PhysicalKeycode==Key.Tab||key.PhysicalKeycode==Key.Escape)Phone.Close();else return;}
        else if(key.PhysicalKeycode==Key.Tab&&!Choices.IsOpen)Phone.Open("messages");
        else if(Choices.IsOpen){if(key.PhysicalKeycode is Key.Escape or Key.E)Choices.HandleKey(key.PhysicalKeycode);else return;}
        else if(Dialogue.IsOpen)Dialogue.HandleKey(key.PhysicalKeycode);
        else if(Memory!=null)Memory.HandleKey(key.PhysicalKeycode);
        else if(Rest.HandleKey(key.PhysicalKeycode)){}
        else if(key.PhysicalKeycode==Key.Escape)Settings.Open();
        else return;
        GetViewport().SetInputAsHandled();
    }
}
