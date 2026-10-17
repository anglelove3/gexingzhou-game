using Godot;
using GeXingzhou.Domain;
public partial class SmokeHarness
{
    private async Task NarrativeReflectionChecks()
    {
        var s=GetNode<GameSession>("/root/GameSession");s.SetOptions(new(){TextSpeed=0,ReducedMotion=true},false);
        foreach(var scenario in new[]{"empty","partial","full"})
        {
            var empty=scenario=="empty";
            var complete=SaveV4Codec.CreateNew(s.Options,s.Navigation) with{Stage=SliceStage.SliceComplete,SceneId="soup_shop",PlayerPosition=new(400,430),
                MemoryOrdinal=1,MemoryState=new(){PushedCoinIds=new(){"c1","c2","c3","c4"},PushedTotal=5,FoodChoice=FoodChoice.Take,Completed=true},
                ReturnContext=new("soup_shop",new(400,430),"soup.return",true),
                CompletedActions=empty?new():new(){"candy.hey.delivered:hey-1","soup.meet:soup-seat-1","slice.complete:slice-1"},
                DiscoveredIds=empty?new():new(){"street.doorplate","street.stool"}};
            if(scenario=="full")complete=complete with{CompletedActions=new(){"invitation.meeting_complete:invitation-1","candy.hey.delivered:hey-1","soup.meet:soup-seat-1","memory.return:soup-1","memory.food.resolve:soup-1","soup.payment:soup-payment-1","slice.complete:slice-1"}};
            s.SetOptions(s.Options with{SubtitleSize=32},false);
            Require(s.Restore(complete).Success,"Narrative reflection fixture invalid");
            var before=StreetLifeSnapshot(s);
            var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(main);await Frames(3);
            try{
                var reflection=main.SliceEnd.GetNodeOrNull<Label>("Panel/Scroll/Content/Reflection");
                Require(reflection!=null&&!string.IsNullOrWhiteSpace(reflection.Text),"Ending lacks fact-derived reflection");
                Require(reflection!.Text.Contains("喜糖")==!empty&&reflection.Text.Contains("旧门牌")==!empty,"Ending does not follow completed/read facts");
                Require(!reflection.Text.Contains("排水沟")&&reflection.Text.Contains("张大炮结")== (scenario=="full"),"Ending invents unobserved/uncompleted events");
                var actions=main.SliceEnd.GetNode<Control>("Panel/Actions");var scroll=main.SliceEnd.GetNode<Control>("Panel/Scroll");
                foreach(var button in main.SliceEnd.FindChildren("*","Button",true,false).OfType<Button>())Require(actions.GetGlobalRect().Encloses(button.GetGlobalRect()),"Long reflection pushes ending actions out of fixed actions "+button.Name);
                Require(!actions.GetGlobalRect().Intersects(scroll.GetGlobalRect()),"Reflection overlaps ending actions");
                if(scenario=="full"){
                    KeyPress(Key.End);await Frames(3);Require(main.SliceEnd.GetNode<ScrollContainer>("Panel/Scroll").ScrollVertical>0,"Keyboard cannot reach long ending reflection");
                    KeyPress(Key.Home);await Frames(3);Require(main.SliceEnd.GetNode<ScrollContainer>("Panel/Scroll").ScrollVertical==0,"Ending Home failed to restore start");
                    var path=TestAutoPath();var original=System.IO.File.Exists(path)?System.IO.File.ReadAllBytes(path):null;
                    try{
                        System.IO.File.WriteAllText(path,"{\"schema_version\":999,\"content_version\":\"future\"}");
                        Require(!main.SliceEnd.TryReturnToMenu(),"Unknown save should block ending menu");await Frames(5);
                        var feedback=main.SliceEnd.GetNode<Label>("Panel/Scroll/Content/Message");
                        Require(feedback.Text.Length>0&&scroll.GetGlobalRect().Encloses(feedback.GetGlobalRect()),"Ending failure feedback hidden below long reflection");
                    }finally{if(original!=null)System.IO.File.WriteAllBytes(path,original);else System.IO.File.Delete(path);}
                }
                Require(before==StreetLifeSnapshot(s),"Rendering reflection wrote gameplay state");
                main.SliceEnd.Close();await Frames(2);Require(s.Flow==FlowState.Field,"Reflection prevents continuing");
            }finally{main.Free();await Frames(3);}
        }
        GD.Print("NARRATIVE_REFLECTION_PASS FactsOnly OptionalEcho NoInventedEvents NoMutation Continue");
    }
}
