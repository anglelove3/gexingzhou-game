using Godot;
using GeXingzhou.Domain;

public partial class SmokeHarness
{
    private async Task<SoupMemoryController> NewCoinTable(int count, int font = 24)
    {
        var s=GetNode<GameSession>("/root/GameSession");s.NewGame();
        s.SetOptions(new(){SubtitleSize=font,TextSpeed=0,ReducedMotion=true},false);
        var context=new SceneReturnContext("soup_shop",new(400,430),"soup.return",true);
        var state=MemorySession.Begin(s.Snapshot,context,false);
        for(int i=0;i<count;i++)state=MemorySession.PushCoin(state,"c"+(i+1));
        Require(s.Restore(s.Snapshot with {Stage=SliceStage.MemoryActive,SceneId="memory_soup_table",PlayerPosition=new(320,280),MemoryOrdinal=1,MemoryState=state,ReturnContext=context}).Success,"Memory fixture restore rejected");s.Flow=FlowState.Memory;
        var table=GD.Load<PackedScene>("res://scenes/world/MemorySoupTable.tscn").Instantiate<SoupMemoryController>();table.Theme=s.CreateUiTheme();AddChild(table);await Frames(3);return table;
    }
    private async Task CoinVisualChecks()
    {
        foreach(var font in new[]{20,24,32})
        {
            var table=await NewCoinTable(0,font);AssertTabletopArtAlignment(table);await Capture("memory-table-"+font);
            table.HandleKey(Key.E);table.HandleKey(Key.E);await Frames(18);await Capture("memory-partial-"+font);
            table.HandleKey(Key.E);table.HandleKey(Key.E);await Frames(18);await Capture("memory-complete-"+font);
            table.GetNode<Button>("HelpToggle").EmitSignal(Button.SignalName.Pressed);await Frames(3);await Capture("memory-help-"+font);
            Require(table.GetGlobalRect().Encloses(table.GetNode<Control>("Table/Foods").GetGlobalRect()),"Table foods clipped");table.Free();await Frames(2);
        }
        GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);
    }
    private void AssertTabletopArtAlignment(SoupMemoryController table)
    {
        var rect=table.GetGlobalRect();var surface=table.GetNode<Control>("Table");
        foreach(var i in Enumerable.Range(1,4))
        {
            var far=surface.GetNode<Marker2D>("Far"+i).GlobalPosition;
            Require((far.Y-rect.Position.Y)/rect.Size.Y>=.50f,"Paid coin floats above authored tabletop");
        }
        var region=surface.GetNode<Control>("DeliveryArea").GetGlobalRect();
        Require((region.Position.Y-rect.Position.Y)/rect.Size.Y>=.46f&&rect.Encloses(region),"Delivery region detached from tabletop");
        Require(rect.Encloses(surface.GetNode<TextureRect>("Bowl").GetGlobalRect()),"Bowl cropped outside screen");
        Require(rect.Encloses(surface.GetNode<Control>("Foods").GetGlobalRect()),"Table foods cropped outside screen");
    }
    private async Task ExperienceCoinsChecks()
    {
        await CoinCrossProgressReloadChecks();
        var s=GetNode<GameSession>("/root/GameSession");
        for(int n=0;n<4;n++)
        {
            var table=await NewCoinTable(n);var surface=table.GetNodeOrNull<Control>("Table");Require(surface!=null,"Missing authored tabletop");
            for(int i=1;i<=4;i++)
            {
                var coin=surface!.GetNode<Button>("Coin"+i);var anchor=surface.GetNode<Marker2D>((i<=n?"Far":"Near")+i);
                Require(coin.Position.DistanceTo(anchor.Position)<.1f,"Restored coin at wrong endpoint");
            }
            Require(surface!.GetNode<Button>("Foods/Take").Disabled,"Food enabled before full delivery");table.Free();await Frames(2);
        }
        var active=await NewCoinTable(0);var first=active.GetNode<Button>("Table/Coin1");
        void Mouse(InputEvent ev)=>active.GetViewport().PushInput(ev,true);
        var from=first.GetGlobalRect().GetCenter();var side=from+new Vector2(250,0);
        Mouse(new InputEventMouseButton{Position=from,GlobalPosition=from,ButtonIndex=MouseButton.Left,Pressed=true});
        Mouse(new InputEventMouseMotion{Position=side,GlobalPosition=side});
        Mouse(new InputEventMouseButton{Position=side,GlobalPosition=side,ButtonIndex=MouseButton.Left,Pressed=false});await Frames(2);
        Require(s.Snapshot.MemoryState!.PushedTotal==0,"Side drag delivered a coin");
        Require(first.Position==active.GetNode<Marker2D>("Table/Near1").Position,"Outside release stranded drag");
        var destination=active.GetNode<Control>("Table/DeliveryArea").GetGlobalRect().GetCenter();
        Mouse(new InputEventMouseButton{Position=from,GlobalPosition=from,ButtonIndex=MouseButton.Left,Pressed=true});
        Mouse(new InputEventMouseMotion{Position=destination,GlobalPosition=destination});
        Mouse(new InputEventMouseButton{Position=destination,GlobalPosition=destination,ButtonIndex=MouseButton.Left,Pressed=false});await Frames(16);
        Require(s.Snapshot.MemoryState.PushedTotal==1,"Forward drag did not deliver");
        for(int i=0;i<3;i++){active.HandleKey(Key.E);await Frames(16);}
        Require(s.Snapshot.MemoryState.PushedTotal==5,"Keyboard did not deliver four coins");
        active.HandleKey(Key.Left);await Frames(2);
        Require(active.GetNode<Button>("Table/Foods/Take").Disabled==false,"Bowl arrival did not enable food");
        Require(active.GetNode<TextureRect>("Table/Bowl").Position.DistanceTo(active.GetNode<Marker2D>("Table/BowlNear").Position)<.1,"Bowl did not approach");
        var before=s.Snapshot.MemoryState;active.GetNode<Button>("Table/Coin4").EmitSignal(Button.SignalName.Pressed);await Frames(2);
        Require(s.Snapshot.MemoryState==before,"Duplicate two-unit coin changed progress");active.Free();await Frames(2);
        GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);
    }
    private async Task CoinCrossProgressReloadChecks()
    {
        var s=GetNode<GameSession>("/root/GameSession");
        foreach(var (current,loaded) in new[]{(3,4),(4,1)})
        {
            var savedTable=await NewCoinTable(loaded);Require(s.SaveManual().Success,"Cross-progress fixture save");savedTable.Free();await Frames(2);
            var seed=await NewCoinTable(current);var candidate=s.Snapshot;seed.Free();await Frames(2);
            s.PendingRestore=candidate;var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(main);await Frames(4);
            GetTree().CurrentScene=null;KeyPress(Key.F9);await Frames(8);
            var reloaded=GetTree().CurrentScene as MainView;
            Require(reloaded!=null&&reloaded!=main&&reloaded.Memory!=null,"Actual F9 did not recreate memory");main.Free();await Frames(3);
            var table=reloaded!.Memory!;var focus=loaded==4?table.GetNode<Button>("Table/Foods/Take"):table.GetNode<Button>("Table/Coin2");
            Require(GetViewport().GuiGetFocusOwner()==focus,"Cross-progress reload retained stale keyboard selection "+current+"->"+loaded);
            KeyPress(Key.E);await Frames(2);
            if(loaded==4)
            {
                Require(s.Snapshot.MemoryState is {Completed:true,FoodChoice:FoodChoice.Take},"Loaded completed coins did not choose first food");
                await WaitUntil(()=>s.Flow!=FlowState.Transition&&reloaded.Memory==null,"Loaded food return");
            }
            else Require(s.Snapshot.MemoryState!.PushedTotal==2&&s.Snapshot.MemoryState.PushedCoinIds.Contains("c2"),"Loaded partial coins chose paid coin");
            reloaded.Free();await Frames(2);
        }
        GD.Print("COIN_CROSS_PROGRESS_F9_PASS 2");
    }
}
