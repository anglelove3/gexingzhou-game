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
    public GuidanceController Guidance {get;private set;}=null!;
    public ObservationController Observations {get;private set;}=null!;
    private bool goalExpanded;
    public SoupSeatController SoupSeat {get;private set;}=null!;
    public AudioDirector Audio {get;private set;}=null!;
    public PauseController Pause {get;private set;}=null!;
    public SliceEndController SliceEnd {get;private set;}=null!;
    public JournalController Journal {get;private set;}=null!;
    public bool CanOpenJournal=>!HasMeta("binding_error")&&GodotObject.IsInstanceValid(World)&&World.IsInsideTree()&&GetNode<GameSession>("/root/GameSession").Flow==FlowState.Field&&Memory==null&&!Rest.IsActive&&!SoupSeat.IsActive&&!Phone.IsOpen&&!Dialogue.IsOpen&&!Choices.IsOpen&&!Pause.IsOpen&&!Settings.IsOpen&&!SliceEnd.IsOpen;
    public bool CanPause=>World!=null&&GetNode<GameSession>("/root/GameSession").Flow==FlowState.Field&&!Rest.IsActive&&!SoupSeat.IsActive&&!Phone.IsOpen&&!Dialogue.IsOpen&&!Choices.IsOpen&&Memory==null;
    public Position2 SafeSavePosition=>GetNode<GameSession>("/root/GameSession").Snapshot.SceneId=="memory_soup_table"?
        GetNode<GameSession>("/root/GameSession").Snapshot.PlayerPosition:
        SoupSeat?.IsActive==true?SoupSeat.SavePosition:Rest?.IsActive==true?Rest.SavePosition:new(World.Player.Position.X,World.Player.Position.Y);
    public string WorldLoadError {get;private set;}="";
    public int PaymentFeedbackCount {get;private set;}
    private Label prompt=null!;private Label status=null!;private SubViewport viewport=null!;
    private WorldDisplayController display=null!;private bool heardVoice,heardRing;private double noticeSeconds;
    public override void _Ready()
    {
        try
        {
            Audio=SceneBindings.Require<AudioDirector>(this,"Audio");
            viewport=SceneBindings.Require<SubViewport>(this,"WorldDisplay/WorldViewport");
            display=SceneBindings.Require<WorldDisplayController>(this,"WorldDisplay");
            Dialogue=SceneBindings.Require<DialogueController>(this,"Dialogue");
            Phone=SceneBindings.Require<PhoneController>(this,"Phone");
            Choices=SceneBindings.Require<ChoiceController>(this,"Choices");
            Settings=SceneBindings.Require<SettingsController>(this,"Settings");
            Rest=SceneBindings.Require<RestController>(this,"Rest");
            Guidance=SceneBindings.Require<GuidanceController>(this,"Guidance");
            Guidance.Configure(this);
            Observations=SceneBindings.Require<ObservationController>(this,"Observations");Observations.Configure(this);
            SoupSeat=SceneBindings.Require<SoupSeatController>(this,"SoupSeat");SoupSeat.Configure(this);
            Pause=SceneBindings.Require<PauseController>(this,"Pause");Pause.Configure(this);
            SliceEnd=SceneBindings.Require<SliceEndController>(this,"SliceEnd");SliceEnd.Configure(this);
            Journal=SceneBindings.Require<JournalController>(this,"Journal");Journal.Configure(this);
            SceneBindings.Require<Button>(this,"HUD/JournalButton").Pressed+=()=>Journal.Open();
            SceneBindings.Require<Button>(this,"HUD/PauseButton").Pressed+=()=>Pause.Open();
            SceneFlow=SceneBindings.Require<SceneFlow>(this,"SceneFlow");SceneFlow.Main=this;
            status=SceneBindings.Require<Label>(this,"HUD/TaskCard/TaskText");
            SceneBindings.Require<Button>(this,"HUD/TaskFold").Pressed+=()=>goalExpanded=!goalExpanded;
            prompt=SceneBindings.Require<Label>(this,"HUD/InteractionHint");
            SceneBindings.Require<Label>(this,"Notice");
            if(FindChildren("*","",true,false).Any(n=>n.HasMeta("binding_error")))
                throw new InvalidOperationException("界面或世界引用不完整，请查看场景错误提示。");
            var s=GetNode<GameSession>("/root/GameSession");var candidate=s.PendingRestore??s.Snapshot;
            if(s.Saves.ValidateSnapshot(candidate) is {} invalid)throw new InvalidOperationException("无法读取进度："+invalid);
            var worldScene=candidate.SceneId=="memory_soup_table"?"soup_shop":candidate.SceneId;
            var worldPosition=candidate.SceneId=="memory_soup_table"?candidate.ReturnContext!.Position:candidate.PlayerPosition;
            World=viewport.GetChildren().OfType<WorldView>().Single();
            if(World.SceneId!=worldScene&&!ChangeWorld(worldScene,worldPosition))
                throw new InvalidOperationException("无法加载存档中的场景。");
            if(World.HasMeta("binding_error"))throw new InvalidOperationException(World.GetMeta("binding_error").AsString());
            World.Player.Position=new(worldPosition.X,worldPosition.Y);
            if(s.PendingRestore!=null)
            {
                World.Player.Position=candidate.SceneId=="memory_soup_table"?new Vector2(candidate.ReturnContext!.Position.X,candidate.ReturnContext.Position.Y):new(candidate.PlayerPosition.X,candidate.PlayerPosition.Y);
                if(candidate.SceneId=="memory_soup_table"&&!EnterMemoryView()){ShowNotice("继续失败","回忆场景无法加载，原档保留。请返回菜单重试。");return;}
                if(!s.Restore(candidate).Success){ShowNotice("继续失败","存档内容无效，原档保留。");return;}
                if(candidate.SceneId=="memory_soup_table"&&candidate.MemoryState!.Completed)_=ReturnMemory(true);
            }
            Theme=s.CreateUiTheme();lastFont=s.Options.SubtitleSize;
            display.Configure(World);
            heardVoice=s.Snapshot.InvitationState.VoiceReceived;heardRing=s.Snapshot.InvitationState.PhoneRinging;Audio.SetScene(s.Snapshot.SceneId);
            if(s.Snapshot.Stage==SliceStage.SliceComplete)SliceEnd.ShowCompleted();
        }
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    public override void _Process(double delta)
    {
        var s=GetNode<GameSession>("/root/GameSession");s.AdvanceClock(delta);
        noticeSeconds=Math.Max(0,noticeSeconds-delta);GetNode<Label>("Notice").Visible=noticeSeconds>0;
        var invitation=s.Snapshot.InvitationState;
        if(invitation.VoiceReceived&&!heardVoice)Audio.PlayCue(AudioCue.PhoneMessage);
        if(invitation.PhoneRinging&&!heardRing)Audio.PlayCue(AudioCue.PhoneRing);
        heardVoice=invitation.VoiceReceived;heardRing=invitation.PhoneRinging;
        prompt.Text=(Rest.IsActive?(GetNode<RestOptionsController>("RestOptions").IsOpen?"Esc 收起选项":"E 休息选项 · Esc 起身"):World.Interactions.Prompt)+" · Tab 手机";
        if(SoupSeat.IsActive)prompt.Text=(SoupSeat.IsActing?"稍等一会儿":"E 桌边选项 · Esc 起身")+" · Tab 手机";
        prompt.Text+=Rest.IsActive||SoupSeat.IsActive?" · 起身后 J 查看记事":" · J 今日记事";
        if(lastFont!=s.Options.SubtitleSize){lastFont=s.Options.SubtitleSize;Theme=s.CreateUiTheme();}
        if(s.Options.Assistance)prompt.Text+=" · ←→移动，靠近金色标记按E";
        prompt.Visible=s.Flow==FlowState.Field;
        var place=World.SceneId switch{"soup_shop"=>"鸭血粉丝汤店","convenience_street"=>"便利店街",_=>"安置小区"};
        status.Text=(goalExpanded?"葛行舟 · "+place+"\n":"")+GameSession.TaskText(s.Snapshot)+(s.Snapshot.InvitationState.PhoneRinging?" · 【来电】":"")+(s.SaveMessage.Length>0?"\n"+s.SaveMessage:"");
        var card=GetNode<PanelContainer>("HUD/TaskCard");card.OffsetBottom=goalExpanded||s.SaveMessage.Length>0?116:78;
        GetNode<Button>("HUD/TaskFold").Text=goalExpanded?"－":"＋";
        World.RefreshQuestActors(s.Snapshot);
        Guidance.Refresh();
        var pauseButton=GetNode<Button>("HUD/PauseButton");pauseButton.Visible=CanPause;pauseButton.Disabled=!CanPause;
        GetNode<Button>("HUD/JournalButton").Disabled=!CanOpenJournal;
    }
    public void ShowNotice(string title,string body)
    {
        Dialogue.ShowText(title,body);
    }
    public void ShowNonBlockingNotice(string title,string body)
    {
        var label=GetNode<Label>("Notice");label.Text=title+"："+body;label.Visible=true;noticeSeconds=8;
    }
    public void ShowDialogue(string id,Action? done=null){if(!Dialogue.Open(id,done))ShowNotice("内容提示","这段内容暂时无法加载。按E或Esc回到自由走动。");}
    public void HandleInteraction(Interactable target)
    {
        var s=GetNode<GameSession>("/root/GameSession");
        if(target is BenchView bench&&target.ActionId=="rest.bench"){Rest.Begin(bench);return;}
        if(target.ActionId=="invitation.meeting_complete")ShowDialogue("invitation.meeting",()=>s.TryDispatch(new(target.ActionId,"meeting","invitation-1")));
        else if(target.ActionId=="candy.hey.delivered")
        {
            if(s.Snapshot.Stage==SliceStage.CandyHeyPending)ShowDialogue("hey.delivery",()=>{if(s.TryDispatch(new(target.ActionId,"delivered","hey-1")).Applied){World.ShowCandyHandover();Audio.PlayCue(AudioCue.Candy);}OfferHey();});
            else if(s.Snapshot.Stage>=SliceStage.CandyHeyDelivered)OfferHey();else ShowNotice("Hey哥","先去小区门口找张大炮吧。");
        }
        else if(target.ActionId=="soup.meet")
        {
            if(s.Snapshot.Stage<SliceStage.CandyHeyDelivered){ShowNotice("桌边","先把张大炮托你的喜糖交给Hey哥。");return;}
            if(SoupSeat.IsActing)return;
            if(SoupSeat.IsActive)OpenSoupMeeting();else SoupSeat.Begin(OpenSoupMeeting);
        }
        else if(target.ActionId.StartsWith("scene:")){var id=target.ActionId[6..];if(id=="soup_shop"&&s.Snapshot.Stage<SliceStage.CandyHeyDelivered)ShowNotice("去汤店之前","先把喜糖送到Hey哥手里，别让他等着。");else if(s.Navigation.Profiles.TryGetValue(id,out var nav))_=EnterWorld(id,nav.Anchors["entry"]);else ShowNonBlockingNotice("切场失败","目标场景导航不可用，当前进度保留。");}
        else if(target.ActionId=="observe")ShowObservation(target);
        else ShowNotice(target.Caption,target.Description);
    }
    public void ShowObservation(Interactable target)
    {
        if(target is ObservationHotspot hotspot&&!Observations.CanObserve(hotspot))return;
        var s=GetNode<GameSession>("/root/GameSession");
        if(World.SceneId=="community_gate"&&(target.Id=="old_sign"||target is BenchView)&&!s.Snapshot.CompletedActions.Contains("observation.community.first"))
        {
            var id=s.Snapshot.InvitationState.Resolution==InvitationResolution.Answered?"observation.community.answered":s.Snapshot.InvitationState.VoiceReceived?"observation.community.unanswered":"observation.community.quiet";
            if(s.Catalog!.Dialogues.TryGetValue(id,out var node)&&node is not null)
                Dialogue.ShowText("葛行舟",target.Description+"\n"+string.Join("\n",node.Lines),()=>{s.UpdatePosition(SafeSavePosition);if(target.Id=="old_sign")s.MarkCommunitySignObserved();else s.MarkFirstCommunityObservation();},DialogueLineKind.Thought);
            else ShowNotice(target.Caption,target.Description);
        }
        else if(target is ObservationHotspot observed)Observations.TryObserve(observed);
        else ShowNotice(target.Caption,target.Description);
    }
    public Vector2 WorldToUi(Vector2 point)
    {
        var canvas=viewport.GetCanvasTransform()*point;
        var global=display.GetGlobalTransformWithCanvas()*(canvas*display.Size/(Vector2)viewport.Size);
        return GetGlobalTransformWithCanvas().AffineInverse()*global;
    }
    public Vector2 UiToWorld(Vector2 global)
    {
        var local=display.GetGlobalTransformWithCanvas().AffineInverse()*global;
        return viewport.GetCanvasTransform().AffineInverse()*(local*(Vector2)viewport.Size/display.Size);
    }
    private void OfferHey()=>Choices.Open("糖已经收好。接下来你怎么做？",new (string,Action)[]{("收起手机，站一会儿",()=>ShowDialogue("hey.stay")),("看一眼手机",()=>ShowDialogue("hey.phone")),("转身去汤店",()=>ShowDialogue("hey.leave"))});
    private void OpenSoupMeeting()
    {
        var s=GetNode<GameSession>("/root/GameSession");
        if(s.Snapshot.Stage==SliceStage.CandyHeyDelivered)s.TryDispatch(new("soup.meet","sit","soup-seat-1"));
        if(s.Snapshot.Stage==SliceStage.SoupMeet){if(s.Snapshot.ChoiceCodes.ContainsKey("soup-response-1"))ShowMemoryEntry();else ShowDialogue("soup.start",OfferSoup);}
        else if(s.Snapshot.Stage==SliceStage.MemoryActive)ShowMemoryEntry();
        else if(s.Snapshot.Stage==SliceStage.MemoryReturned)ShowSoupReturn("soup.return");
        else if(s.Snapshot.Stage==SliceStage.SliceComplete)ShowMemoryEntry(true);
    }
    private void OfferSoup()
    {
        var options=new List<(string,Action)>();
        foreach(var (code,caption) in new[]{("eat","先吃一口汤"),("set_chopsticks","替他摆好筷子"),("check_phone","看一眼旧手机")})
            options.Add((caption,()=>{var s=GetNode<GameSession>("/root/GameSession");if(s.TryDispatch(new("soup.response",code,"soup-response-1")).Applied&&!SoupSeat.PlayAction(code,()=>ShowDialogue("soup."+code,ShowMemoryEntry)))ShowDialogue("soup."+code,ShowMemoryEntry);}));
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
        var packed=GetNode<GameSession>("/root/GameSession").GetScene("res://scenes/world/MemorySoupTable.tscn");if(packed==null)return false;
        var next=packed.Instantiate<SoupMemoryController>();
        try{next.ValidateBindings();}catch(InvalidOperationException){next.Free();return false;}
        next.Main=this;next.ProcessMode=ProcessModeEnum.Disabled;next.Visible=false;AddChild(next);
        if(next.HasMeta("binding_error")){next.Free();return false;}
        Rest.Cancel();SoupSeat.Cancel();Audio.StopTransient();Memory?.Free();Memory=next;
        next.ProcessMode=ProcessModeEnum.Inherit;next.Visible=true;World.Visible=false;Audio.SetScene("memory_soup_table");return true;
    }
    public async Task ReturnMemory(bool complete)
    {
        var s=GetNode<GameSession>("/root/GameSession");if(s.Snapshot.ReturnContext is not {} context)return;
        var result=await SceneFlow.TryReturn(context);if(!result.Success){ShowNotice("切场失败","返回暂时失败，回忆进度保留。请重试。");return;}
        if(Memory!=null){Memory.Free();Memory=null;}World.Visible=true;
        s.RecordMemoryReturned();
        s.SaveCheckpoint();
        if(complete&&s.Snapshot.MemoryState is {} m)
        {
            s.TryDispatch(new("memory.return","return",m.InstanceId));
            SoupSeat.Begin(()=>{if(!m.Replay)ShowSoupReturn(context.DialogueNodeId);else ShowNotice("回忆结束","过去没有被改写。你仍坐在现实的汤店里。");});
        }
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
            World.ShowPayment();Audio.PlayCue(AudioCue.Coin);
            var feedback=GetNode<Label>("HUD/PaymentCaption");feedback.Visible=true;
            var tween=CreateTween();tween.TweenInterval(s.Options.FadeDuration+.8);tween.TweenCallback(Callable.From(()=>feedback.Visible=false));
        }
        ShowDialogue("soup.tomorrow",()=>{if(s.TryDispatch(new("slice.complete","completed","slice-1")).Applied)SliceEnd.ShowCompleted();});
    }
    public WorldView? PrepareWorld(string sceneId,Position2 position)
    {
        WorldLoadError="";
        WorldView? next=null;
        try
        {
            var s=GetNode<GameSession>("/root/GameSession");var path=ScenePath(sceneId);
            if(path==null||s.Navigation==null||!s.Navigation.Profiles.TryGetValue(sceneId,out var nav)||!NavigationGeometry.CanStand(nav,position)){WorldLoadError="场景或导航站位无效，当前进度保留。";return null;}
            var packed=GetNode<GameSession>("/root/GameSession").GetScene("res://scenes/world/"+path+".tscn");if(packed==null){WorldLoadError="场景资源缺失，当前进度保留。";return null;}
            next=packed.Instantiate<WorldView>();
            if(!NavigationSceneReader.Matches(NavigationSceneReader.Read(next),nav)){WorldLoadError="导航数据与编辑场景不一致，请检查并重新导出；当前进度保留。";next.Free();return null;}
            next.ProcessMode=ProcessModeEnum.Disabled;next.Visible=false;return next;
        }
        catch(InvalidOperationException ex){WorldLoadError=ex.Message;next?.Free();return null;}
    }
    public bool CommitWorld(WorldView next,Position2 position)
    {
        viewport.AddChild(next);
        if(next.HasMeta("binding_error")){next.Free();return false;}
        Rest?.Cancel();SoupSeat?.Cancel();Audio.StopTransient();
        World.Free();World=next;next.Name=ScenePath(next.SceneId)!;
        World.Player.Position=new(position.X,position.Y);World.Player.SetInputLocked(false);
        next.ProcessMode=ProcessModeEnum.Inherit;next.Visible=Memory==null;display.Configure(World);
        GetNode<GameSession>("/root/GameSession").UpdateScene(next.SceneId,position);Audio.SetScene(next.SceneId);return true;
    }
    public bool ChangeWorld(string sceneId,Position2 position)=>PrepareWorld(sceneId,position) is {} next&&CommitWorld(next,position);
    private async Task EnterWorld(string id,Position2 position)
    {
        var result=await SceneFlow.TryEnter(id,position);
        if(!result.Success)ShowNonBlockingNotice("切场失败",WorldLoadError.Length>0?WorldLoadError:"场景暂不能加载，当前进度保留，请重试。");
    }
    private static string? ScenePath(string id)=>id switch{"convenience_street"=>"ConvenienceStreet","community_gate"=>"CommunityGate","soup_shop"=>"SoupShop",_=>null};
    private void LoadManual()
    {
        var s=GetNode<GameSession>("/root/GameSession");
        var loaded=s.ManualSaves.Load();
        if(loaded.Status==LoadStatus.Loaded)
        {
            var snapshot=loaded.Snapshot!;var scene=snapshot.SceneId=="memory_soup_table"?"soup_shop":snapshot.SceneId;
            var position=snapshot.SceneId=="memory_soup_table"?snapshot.ReturnContext!.Position:snapshot.PlayerPosition;
            var candidate=PrepareWorld(scene,position);if(candidate==null){ShowNonBlockingNotice("手动存档",WorldLoadError);return;}
            viewport.AddChild(candidate);var invalid=candidate.HasMeta("binding_error");candidate.Free();
            if(invalid){GetNode<Label>("StartupError").Visible=false;ShowNonBlockingNotice("手动存档","场景资源暂不能加载；原进度保留。");return;}
            if(snapshot.SceneId=="memory_soup_table")
            {
                SoupMemoryController? memoryCandidate=null;
                try
                {
                    var packed=s.GetScene("res://scenes/world/MemorySoupTable.tscn")??throw new InvalidOperationException("回忆场景资源缺失。");
                    memoryCandidate=packed.Instantiate<SoupMemoryController>();memoryCandidate.ValidateBindings();
                }
                catch(InvalidOperationException ex){ShowNonBlockingNotice("手动存档",ex.Message+" 原进度保留。");return;}
                finally{memoryCandidate?.Free();}
            }
            if(Pause.IsOpen)Pause.Close();
            if(Phone.IsOpen)Phone.Close();Rest.Cancel();SoupSeat.Cancel();Audio.StopTransient();
            s.PendingRestore=snapshot;GetTree().ChangeSceneToPacked(s.GetScene("res://scenes/Main.tscn")!);
        }
        else ShowNonBlockingNotice("手动存档",loaded.Message);
    }
    public override void _Input(InputEvent ev)
    {
        if(ev is not InputEventKey{Pressed:true,Echo:false} key)return;
        var code=key.PhysicalKeycode;var s=GetNode<GameSession>("/root/GameSession");
        if(s.Flow==FlowState.Transition){GetViewport().SetInputAsHandled();return;}
        if(Journal.IsOpen){if(Journal.HandleKey(code))GetViewport().SetInputAsHandled();return;}
        if(code==Key.J){Journal.Open();GetViewport().SetInputAsHandled();return;}
        if(Settings.IsOpen){if(code==Key.Escape)Settings.Close();else return;}
        else if(SliceEnd.IsOpen){if(code==Key.Escape)SliceEnd.Close();else return;}
        else if(Pause.IsOpen)
        {
            if(code==Key.Escape)Pause.Close();
            else if(code==Key.F5)Pause.Save();
            else if(code==Key.F9)LoadManual();
            else return;
        }
        else if(code==Key.F5){s.UpdatePosition(SafeSavePosition);s.SaveManual();}
        else if(code==Key.F9)LoadManual();
        else if(Phone.IsOpen){if(code is Key.Tab or Key.Escape)Phone.Close();else return;}
        else if(code==Key.Tab&&!Choices.IsOpen){Memory?.CancelDrag();Phone.Open("messages");}
        else if(Choices.IsOpen){if(code is Key.Escape or Key.E)Choices.HandleKey(code);else return;}
        else if(Dialogue.IsOpen)Dialogue.HandleKey(code);
        else if(Memory!=null)Memory.HandleKey(code);
        else if(Rest.HandleKey(code)){}
        else if(SoupSeat.HandleKey(code)){}
        else if(code==Key.Escape)Pause.Open();
        else return;
        GetViewport().SetInputAsHandled();
    }
}
