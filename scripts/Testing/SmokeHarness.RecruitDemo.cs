using Godot;
using GeXingzhou.Domain;
public partial class SmokeHarness
{
    private async Task RecruitDemoChecks()
    {
        var failures=new List<string>();
        async Task CheckCase(string name,Func<Task> run){try{await run();GD.Print("RECRUIT_CASE_PASS "+name);}catch(Exception ex){failures.Add(name+": "+ex.Message);GD.Print("RECRUIT_CASE_FAIL "+name+": "+ex.Message);}}
        // Break: the fourth physical coin adds two units, but must count as one visible coin.
        await CheckCase("CoinCountTracksPhysicalCoins",async()=>{
            foreach(var count in new[]{0,1,3,4}){
                var table=await NewCoinTable(count);
                try{Require(table.GetNode<Label>("Status").Text.Contains(count+"/4"),"Coin progress disagrees with visible four coins at "+count);}
                finally{table.Free();await Frames(2);}
            }
        });
        // Break: a help control that changes flow or cannot be reopened can strand a new player.
        await CheckCase("PauseHelpIsReusableAndReadOnly",async()=>{
            var main=await NewPolishMain();var s=GetNode<GameSession>("/root/GameSession");
            try{
                Require(main.Pause.Open(),"Pause did not open");await Frames(2);var before=StreetLifeSnapshot(s);
                var toggle=main.Pause.GetNodeOrNull<Button>("Panel/Scroll/Content/Controls");
                var help=main.Pause.GetNodeOrNull<Label>("Panel/Scroll/Content/ControlsHelp");
                Require(toggle!=null&&help!=null,"No reusable control help in pause");
                toggle!.EmitSignal(Button.SignalName.Pressed);await Frames(2);
                Require(help!.Visible&&help.Text.Contains("WASD")&&help.Text.Contains("E")&&help.Text.Contains("J"),"Help did not show essential controls");
                Require(s.Flow==FlowState.Paused&&before==StreetLifeSnapshot(s),"Reading help changed gameplay");
                main.Pause.Close();Require(main.Pause.Open(),"Pause failed to reopen");await Frames(2);
                Require(!help.Visible,"Help retained expanded state on reopen");
                toggle.EmitSignal(Button.SignalName.Pressed);await Frames(2);Require(help.Visible,"Help cannot be opened twice");
                var path=TestAutoPath();var original=System.IO.File.Exists(path)?System.IO.File.ReadAllBytes(path):null;
                try{
                    System.IO.File.WriteAllText(path,"{\"schema_version\":999,\"content_version\":\"future\"}");
                    var scroll=main.Pause.GetNode<ScrollContainer>("Panel/Scroll");scroll.ScrollVertical=int.MaxValue;await Frames(3);
                    Require(!main.Pause.TryReturnToMenu(),"Unsupported test save should block menu navigation");await Frames(5);
                    var feedback=main.Pause.GetNode<Label>("Panel/Scroll/Content/Message");
                    Require(scroll.GetGlobalRect().Grow(.1f).Encloses(feedback.GetGlobalRect()),$"Failure hidden above expanded control help: scroll={scroll.GetGlobalRect()} feedback={feedback.GetGlobalRect()} offset={scroll.ScrollVertical}");
                }finally{if(original!=null)System.IO.File.WriteAllBytes(path,original);else System.IO.File.Delete(path);}
            }finally{main.Free();await Frames(3);}
        });
        // Break: a destination-only goal fails to explain the direction from the current scene.
        await CheckCase("ExpandedGoalAndJournalExplainRoute",async()=>{
            var main=await NewPolishMain();var s=GetNode<GameSession>("/root/GameSession");
            try{
                var pending=s.Snapshot with{Stage=SliceStage.CandyHeyPending,CandyCount=1};Require(s.Restore(pending).Success,"Invalid route fixture");
                main.GetNode<Button>("HUD/TaskFold").EmitSignal(Button.SignalName.Pressed);await Frames(3);
                Require(main.GetNode<Label>("HUD/TaskCard/TaskText").Text.Contains("右"),"Expanded goal lacks community exit direction");
                Require(main.Journal.Open(),"Journal did not open");await Frames(2);
                Require(main.Journal.GetNode<Label>(JournalText).Text.Contains("右"),"Journal lacks route direction");main.Journal.Close();
                Require(main.ChangeWorld("convenience_street",new(800,520)),"Route street fixture failed");
                Require(s.Restore(s.Snapshot with{Stage=SliceStage.CandyHeyDelivered,CandyCount=0}).Success,"Invalid delivered fixture");await Frames(3);
                Require(main.GetNode<Label>("HUD/TaskCard/TaskText").Text.Contains("尽头"),"Street route still uses community directions");
                KeyPress(Key.Escape);await Frames(2);Require(!main.GetNode<Label>("HUD/Guidance").Visible,"Route leaks over pause");
            }finally{main.Free();await Frames(3);}
        });
        // Real Esc-return and scene transition: leaving an unfinished memory must first lead back to the shop.
        await CheckCase("MemoryExitThenStreetRoute",async()=>{
            var s=GetNode<GameSession>("/root/GameSession");var seed=await NewCoinTable(1);var pending=s.Snapshot;seed.Free();await Frames(2);
            s.PendingRestore=pending;var main=s.GetScene("res://scenes/Main.tscn")!.Instantiate<MainView>();AddChild(main);await Frames(4);
            try{
                Require(main.Memory!=null,"Restored unfinished memory missing");KeyPress(Key.Escape);
                await WaitUntil(()=>main.Memory==null&&s.Flow==FlowState.Field,"Actual unfinished memory exit");
                Require(s.Snapshot.Stage==SliceStage.MemoryActive&&s.Snapshot.MemoryState!.PushedTotal==1,"Exit lost memory progress");
                Require((await main.SceneFlow.TryEnter("convenience_street",new(1500,520))).Success,"Actual return to street failed");
                main.GetNode<Button>("HUD/TaskFold").EmitSignal(Button.SignalName.Pressed);await Frames(3);
                Require(main.GetNode<Label>("HUD/TaskCard/TaskText").Text.Contains("尽头"),"Exited memory still instructs absent coins");
                Require(main.Journal.Open(),"Street return journal failed");await Frames(2);
                Require(main.Journal.GetNode<Label>(JournalText).Text.Contains("尽头"),"Journal cannot lead back to unfinished memory");
            }finally{main.Free();await Frames(3);}
        });
        if(failures.Count>0)throw new Exception(string.Join("; ",failures));
    }
}
