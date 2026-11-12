using Godot;
using GeXingzhou.Domain;

public partial class SmokeHarness
{
    private async Task<MainView> InterruptedMemoryMain(bool reduced)
    {
        var seed=await NewCoinTable(3);var session=GetNode<GameSession>("/root/GameSession");
        var snapshot=session.Snapshot;seed.Free();await Frames(2);
        session.SetOptions(session.Options with {ReducedMotion=reduced},false);session.PendingRestore=snapshot;
        var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(main);await Frames(4);
        Require(main.Memory!=null&&session.Flow==FlowState.Memory,"Interrupted memory fixture failed");return main;
    }
    private async Task MemoryInterruptionChecks()
    {
        var session=GetNode<GameSession>("/root/GameSession");var failures=new List<string>();
        foreach(var reduced in new[]{false,true})foreach(var phone in new[]{true,false})
        {
            var main=await InterruptedMemoryMain(reduced);var memory=main.Memory!;
            Require(memory.TryPush(3),"Last coin rejected");await Frames(3);
            if(phone){KeyPress(Key.Tab);await Frames(1);Require(main.Phone.IsOpen,"Memory phone did not open");}
            else GetTree().Root.PropagateNotification((int)Node.NotificationApplicationFocusOut);
            var bowl=memory.GetNode<TextureRect>("Table/Bowl");var coin=memory.GetNode<Button>("Table/Coin4");
            var food=memory.GetNode<Button>("Table/Foods/Take");
            var bowlPosition=bowl.Position;var coinPosition=coin.Position;var focus=GetViewport().GuiGetFocusOwner();
            var snapshot=System.Text.Json.JsonSerializer.Serialize(session.Snapshot.MemoryState);await GameTime(1);
            var mode=(phone?"Phone":"FocusLoss")+(reduced?"Reduced":"Normal");
            if(bowl.Position!=bowlPosition||coin.Position!=coinPosition||!food.Disabled||phone&&GetViewport().GuiGetFocusOwner()!=focus)
            {failures.Add(mode+": interrupted memory animation/focus continued");GD.Print("MEMORY_INTERRUPT_FAIL "+mode);}
            Require(System.Text.Json.JsonSerializer.Serialize(session.Snapshot.MemoryState)==snapshot,"Interruption changed logical coin progress");
            if(phone)KeyPress(Key.Tab);else GetTree().Root.PropagateNotification((int)Node.NotificationApplicationFocusIn);
            await Frames(1);
            var bowlTarget=memory.GetNode<Marker2D>("Table/BowlNear").Position;
            Require(bowl.Position.DistanceTo(bowlTarget)<=bowlPosition.DistanceTo(bowlTarget)+.1f,"Resuming rewound bowl progress");
            await WaitUntil(()=>!food.Disabled,"Memory animation resume");await Frames(2);
            Require(coin.Position.DistanceTo(memory.GetNode<Marker2D>("Table/Far4").Position)<.1f&&bowl.Position.DistanceTo(bowlTarget)<.1f,"Resumed coin/bowl missed final endpoints");
            Require(GetViewport().GuiGetFocusOwner()==food,"Resumed bowl did not restore food keyboard focus");
            Require(session.Snapshot.MemoryState!.PushedCoinIds.Count==4&&session.Snapshot.MemoryState.PushedTotal==5,"Resume repeated coin delivery");
            KeyPress(Key.E);await Frames(3);Require(session.Snapshot.MemoryState.Completed,"Resumed food keyboard action failed");
            await WaitUntil(()=>main.Memory==null&&session.Flow!=FlowState.Transition,"Interrupted memory return");
            main.Free();await Frames(3);GD.Print("MEMORY_INTERRUPT_CASE_DONE "+mode);
        }
        if(failures.Count>0)throw new Exception(string.Join("; ",failures));
        GD.Print("MEMORY_INTERRUPTION_PASS Phone FocusLoss Normal Reduced SnapshotStable KeyboardResume");
    }
}
