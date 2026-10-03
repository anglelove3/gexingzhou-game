using Godot;
public partial class SmokeHarness : Node
{
    public override async void _Ready()
    {
        try
        {
            var suite=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--suite="))?.Split('=')[1] ?? "Movement";
            if(suite is "Resume" or "ResumeSeed")
            {
                await MemoryPath(false,0,false,true);GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);GD.Print("GODOT_CHECKS_PASS "+suite);GetTree().Quit();return;
            }
            if(suite=="ResumeRead")
            {
                var s=GetNode<GameSession>("/root/GameSession");var loaded=s.Saves.Load();if(loaded.Status!=GeXingzhou.Domain.LoadStatus.Loaded)throw new Exception("Cross-process save missing: "+loaded.Message);
                s.PendingRestore=loaded.Snapshot;var readMain=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(readMain);await Frames(3);
                if(s.Snapshot.Stage!=GeXingzhou.Domain.SliceStage.SliceComplete||s.Flow!=GeXingzhou.Domain.FlowState.Field||readMain.World.SceneId!="soup_shop")throw new Exception("Cross-process resume did not restore real world");
                readMain.Free();await Frames(2);GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);GD.Print("GODOT_CHECKS_PASS ResumeRead");GetTree().Quit();return;
            }
            if(suite is "Memory" or "Slice")
            {
                if(suite=="Memory")for(int food=0;food<3;food++)await MemoryPath(false,food,true);
                else foreach(var answer in new[]{false,true})await MemoryPath(answer,0,false);
                GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);GD.Print("GODOT_CHECKS_PASS "+suite);GetTree().Quit();return;
            }
            if(suite=="Soup")
            {
                foreach(var (code,index) in new[]{("eat",0),("set_chopsticks",1),("check_phone",2)})await SoupPath(code,index);
                GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);GD.Print("GODOT_CHECKS_PASS Soup");GetTree().Quit();return;
            }
            if(suite is "Invitation" or "Hey")
            {
                foreach(var answer in new[]{false,true})await StoryPath(answer,suite);
                GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);
                GD.Print("GODOT_CHECKS_PASS "+suite);GetTree().Quit();return;
            }
            if(suite!="Movement") {GD.PrintErr("UNKNOWN_SUITE "+suite);GetTree().Quit(2);return;}
            var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate(); AddChild(main);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var players=main.FindChildren("Player","CharacterBody2D",true,false);
            if(players.Count!=1) throw new Exception("Real community player missing");
            var player=(CharacterBody2D)players[0];
            if(player.GetNodeOrNull<Camera2D>("Camera2D")==null) throw new Exception("Named camera missing");
            if(((MainView)main).World.Interactions==null) throw new Exception("World initialization incomplete");
            var x=player.Position.X;
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.D,Pressed=true});
            for(int i=0;i<25;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.D,Pressed=false});
            if(player.Position.X<=x) throw new Exception("Physical input did not move actual body");
            var controller=(PlayerController)player; controller.SetInputLocked(true); x=player.Position.X;
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.D,Pressed=true});
            for(int i=0;i<5;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            if(player.Position.X!=x)throw new Exception("Input lock leaked movement");
            controller.SetInputLocked(false);player.Position=new Vector2(1590,280);
            for(int i=0;i<30;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.D,Pressed=false});
            if(player.Position.X>1592.1f)throw new Exception("Player crossed boundary");
            player.Position=new Vector2(160,280);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.E,Pressed=true});
            Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.E,Pressed=false});
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            if(GetNode<GameSession>("/root/GameSession").Flow!=GeXingzhou.Domain.FlowState.Dialogue)throw new Exception("Observation input did not open notice");
            GD.Print("GODOT_CHECKS_PASS "+suite);GetTree().Quit();
        }
        catch(Exception ex){GD.PrintErr("GODOT_CHECKS_FAIL "+ex.Message);GetTree().Quit(1);}
    }
    private async Task Frames(int count=1){for(int i=0;i<count;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    private void KeyPress(Key key){using var down=new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=true};using var up=new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=false};Input.ParseInputEvent(down);Input.ParseInputEvent(up);}
    private async Task Finish(MainView main)
    {
        for(int i=0;i<40&&main.Dialogue.IsOpen;i++){await Frames(12);if(!main.Dialogue.IsOpen)break;KeyPress(Key.E);await Frames(2);}
        if(main.Dialogue.IsOpen)throw new Exception("Dialogue never finished with keyboard");
    }
    private async Task StoryPath(bool answer,string suite,bool resume=false)
    {
        var s=GetNode<GameSession>("/root/GameSession");s.NewGame();
        var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(main);await Frames(2);
        s.AdvanceClock(35);KeyPress(Key.Tab);await Frames(2);
        if(!main.Phone.IsOpen)throw new Exception("Keyboard did not open phone");
        var time=s.Snapshot.InvitationState.Elapsed;s.AdvanceClock(100);
        if(s.Snapshot.InvitationState.Elapsed!=time)throw new Exception("Phone did not freeze invitation");
        KeyPress(Key.F5);await Frames(2);
        if(s.ManualSaves.Load().Status!=GeXingzhou.Domain.LoadStatus.Loaded||s.Flow!=GeXingzhou.Domain.FlowState.Phone)throw new Exception("Phone manual save failed or changed flow");
        if(answer){KeyPress(Key.Enter);await Frames(2);if(s.Snapshot.InvitationState.Resolution!=GeXingzhou.Domain.InvitationResolution.Answered)throw new Exception("Phone answer button not keyboard accessible");await Finish(main);}
        else {KeyPress(Key.Escape);await Frames(2);}
        if(s.Snapshot.CandyCount!=0)throw new Exception("Phone gave candy early");
        s.AdvanceClock(30);await Frames(2);
        var cannon=main.World.GetNodeOrNull<Interactable>("Cannon");
        if(cannon==null)throw new Exception($"Cannon missing: elapsed={s.Snapshot.InvitationState.Elapsed} arrived={s.Snapshot.InvitationState.CarArrived} flow={s.Flow} stage={s.Snapshot.Stage} scene={main.World.SceneId}");
        if(!cannon.TryInteract(s))throw new Exception("Cannot meet cannon");await Finish(main);
        if(s.Snapshot.CandyCount!=1)throw new Exception("Meeting did not give one candy");
        if(resume){main=await Restart(main);s=GetNode<GameSession>("/root/GameSession");}
        if(suite=="Hey")
        {
            main.ChangeWorld("convenience_street",new(600,280));await Frames(2);
            main.ChangeWorld("community_gate",new(320,280));await Frames(2);
            main.ChangeWorld("convenience_street",new(600,280));await Frames(2);
            var hey=main.World.GetChildren().OfType<Interactable>().Single(t=>t.Id=="hey");hey.TryInteract(s);await Finish(main);
            if(s.Snapshot.CandyCount!=0||s.Snapshot.Stage!=GeXingzhou.Domain.SliceStage.CandyHeyDelivered)throw new Exception("Hey delivery failed");
            if(resume){main=await Restart(main);s=GetNode<GameSession>("/root/GameSession");hey=main.World.GetChildren().OfType<Interactable>().Single(t=>t.Id=="hey");hey.TryInteract(s);}
            await Choose(main,0);await Finish(main);
            hey.TryInteract(s);await Choose(main,1);await Finish(main);if(s.Snapshot.CandyCount!=0)throw new Exception("Repeated delivery changed candy");
        }
        GD.Print("STORY_PATH_PASS "+(answer?"answered":"ignored")+" "+suite);main.Free();await Frames();
    }
    private async Task Choose(MainView main,int index)
    {
        if(!main.Choices.IsOpen)throw new Exception("Expected keyboard choice");
        await Frames(2);for(int i=0;i<index;i++){KeyPress(Key.Down);await Frames(2);}KeyPress(Key.Enter);await Frames(2);
        if(main.Choices.IsOpen)throw new Exception("Keyboard choice did not close");
    }
    private async Task<MainView> SoupPath(string code,int index,bool keep=false,bool answer=false,bool resume=false)
    {
        await StoryPath(answer,"Hey",resume);var s=GetNode<GameSession>("/root/GameSession");
        var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(main);await Frames(2);
        main.ChangeWorld("soup_shop",new(440,280));await Frames(2);
        main.ChangeWorld("convenience_street",new(1120,280));main.ChangeWorld("soup_shop",new(440,280));await Frames(2);
        main.World.GetChildren().OfType<Interactable>().Single(t=>t.Id=="seat").TryInteract(s);
        if(resume){main=await Restart(main);s=GetNode<GameSession>("/root/GameSession");main.World.GetChildren().OfType<Interactable>().Single(t=>t.Id=="seat").TryInteract(s);}
        await Finish(main);await Choose(main,index);await Finish(main);
        if(s.Snapshot.ChoiceCodes["soup-response-1"]!=code||s.Snapshot.Stage!=GeXingzhou.Domain.SliceStage.SoupMeet)throw new Exception("Wrong soup route: want="+code+" got="+s.Snapshot.ChoiceCodes["soup-response-1"]);
        if(main.Choices.IsOpen){KeyPress(Key.Escape);await Frames(2);}
        var stage=s.Snapshot.Stage;main.ShowDialogue("missing_node");await Frames(2);KeyPress(Key.Escape);await Frames(2);
        if(s.Flow!=GeXingzhou.Domain.FlowState.Field||s.Snapshot.Stage!=stage)throw new Exception("Missing node locked or changed story");
        GD.Print("SOUP_PATH_PASS "+code);if(!keep){main.Free();await Frames();}return main;
    }
    private async Task MemoryPath(bool answer,int food,bool exercise,bool resume=false)
    {
        var main=await SoupPath("eat",0,true,answer,resume);var s=GetNode<GameSession>("/root/GameSession");
        var seat=main.World.GetChildren().OfType<Interactable>().Single(t=>t.Id=="seat");seat.TryInteract(s);await Choose(main,0);await Frames(50);
        if(main.Memory==null||s.Flow!=GeXingzhou.Domain.FlowState.Memory)throw new Exception("Memory scene failed to enter");
        KeyPress(Key.E);await Frames(2);KeyPress(Key.E);await Frames(2);var id=s.Snapshot.MemoryState!.InstanceId;
        if(resume){main=await Restart(main);s=GetNode<GameSession>("/root/GameSession");if(s.Snapshot.MemoryState!.PushedTotal!=2)throw new Exception("Saved coins missing after real scene restore");}
        if(exercise)
        {
            KeyPress(Key.Escape);await Frames(50);
            if(s.Snapshot.Stage!=GeXingzhou.Domain.SliceStage.MemoryActive||s.Snapshot.SceneId!="soup_shop")throw new Exception("Escape incorrectly completed memory");
            main.World.GetChildren().OfType<Interactable>().Single(t=>t.Id=="seat").TryInteract(s);await Choose(main,0);await Frames(50);
            if(s.Snapshot.MemoryState!.InstanceId!=id||s.Snapshot.MemoryState.PushedTotal!=2)throw new Exception("Memory resume lost coins or instance");
        }
        KeyPress(Key.E);await Frames(2);KeyPress(Key.E);await Frames(2);
        if(s.Snapshot.MemoryState!.PushedTotal!=5)throw new Exception("Keyboard did not push all coins");
        for(int i=0;i<food;i++){KeyPress(Key.Right);await Frames(2);}KeyPress(Key.E);await Frames(50);
        if(resume){main=await Restart(main);s=GetNode<GameSession>("/root/GameSession");main.World.GetChildren().OfType<Interactable>().Single(t=>t.Id=="seat").TryInteract(s);}
        await Finish(main);
        if(s.Snapshot.Stage!=GeXingzhou.Domain.SliceStage.SliceComplete||s.Snapshot.SceneId!="soup_shop")throw new Exception("Memory did not return and complete slice");
        if((int)s.Snapshot.MemoryState!.FoodChoice!.Value!=food)throw new Exception("Wrong memory food choice");
        if(resume){main=await Restart(main);s=GetNode<GameSession>("/root/GameSession");}
        if(exercise)
        {
            main.World.GetChildren().OfType<Interactable>().Single(t=>t.Id=="seat").TryInteract(s);await Choose(main,0);await Frames(50);
            if(s.Snapshot.MemoryState!.InstanceId!="soup-2"||s.Snapshot.Stage!=GeXingzhou.Domain.SliceStage.SliceComplete)throw new Exception("Replay changed story or reused instance");
            for(int i=0;i<4;i++){KeyPress(Key.E);await Frames(2);}KeyPress(Key.E);await Frames(50);await Finish(main);
            if(s.Snapshot.Stage!=GeXingzhou.Domain.SliceStage.SliceComplete)throw new Exception("Replay regressed story");
        }
        var before=s.Snapshot;var result=await main.SceneFlow.TryEnter("missing_scene",new(1,1));
        if(result.Success||s.Snapshot.Stage!=before.Stage||s.Snapshot.SceneId!=before.SceneId||s.Flow!=GeXingzhou.Domain.FlowState.Field)throw new Exception("Failed transition changed state or locked input");
        GD.Print("MEMORY_PATH_PASS "+(answer?"answered":"ignored")+" food="+food);main.Free();await Frames();
    }
    private async Task<MainView> Restart(MainView main)
    {
        var s=GetNode<GameSession>("/root/GameSession");var saved=s.Saves.Load();if(saved.Status!=GeXingzhou.Domain.LoadStatus.Loaded)throw new Exception("Checkpoint failed to save: "+saved.Message+" / "+s.SaveMessage);
        var stage=s.Snapshot.Stage;var candy=s.Snapshot.CandyCount;var expected=saved.Snapshot!.PlayerPosition;
        main.Free();s.Free();await Frames(2);s=new GameSession{Name="GameSession"};GetTree().Root.AddChild(s);s.PendingRestore=saved.Snapshot;
        main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(main);await Frames(3);
        if(s.Snapshot.Stage!=stage||s.Snapshot.CandyCount!=candy||s.PendingRestore!=null)throw new Exception("Checkpoint not restored");
        if(s.Snapshot.SceneId!="memory_soup_table"&&(main.World.Player.Position.X!=expected.X||s.Flow!=GeXingzhou.Domain.FlowState.Field))throw new Exception("Restore position or input lock wrong");
        GD.Print("CHECKPOINT_RESTART_PASS "+stage);return main;
    }
}
