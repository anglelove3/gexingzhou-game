using Godot;
using GeXingzhou.Domain;
public partial class SmokeHarness
{
    private async Task StreetCaptureChecks()
    {
        var session=GetNode<GameSession>("/root/GameSession");
        foreach(var font in new[]{20,24,32})
        {
            session.SetOptions(new(){SubtitleSize=font,TextSpeed=0,ReducedMotion=false},false);
            var main=await NewPolishMain();
            try
            {
                Require(main.ChangeWorld("convenience_street",new(100,520)),"Street capture entry failed");await Frames(5);
                foreach(var (name,position) in new[]{("entry",new Vector2(100,520)),("main",new Vector2(800,520)),("north",new Vector2(800,220)),("south",new Vector2(810,650)),("east",new Vector2(1380,420))}){
                    main.World.Player.Position=position;await Frames(5);AssertRefinementHud(main);await Capture("street-"+name+"-"+font);
                }
                foreach(var id in StreetTargets){
                    var target=(ObservationHotspot)main.World.GetTarget(id)!;main.World.Player.Position=target.Position;await Frames(5);
                    Require(main.Observations.TryObserve(target),"Street capture observe rejected "+id);await GameTime(.2);AssertDialogueFits(main);
                    await Capture("street-observe-"+id+"-"+font);await Finish(main);
                    Require(session.Snapshot.DiscoveredIds.Contains("street."+id),"Street capture lost completed read");
                }
                foreach(var id in new[]{"community.sign","community.notice","community.planter","soup.sign","soup.menu","soup.note"})session.MarkDiscovery(id);
                main.World.Player.Position=new(800,520);await Frames(4);
                Require(main.Journal.Open(),"Thirteen journal capture rejected");main.Journal.ShowPage(JournalPage.Discoveries);await Frames(4);
                var list=main.Journal.GetNode<Label>(JournalText);
                Require(JournalProjection.Build(session.Snapshot).Discoveries.Count==13,"Capture does not contain thirteen discoveries");
                await Capture("street-journal-top-"+font);KeyPress(Key.End);await Frames(4);await Capture("street-journal-bottom-"+font);main.Journal.Close();
                foreach(var (name,position) in new[]{("front",new Vector2(800,270)),("back",new Vector2(800,210))}){
                    main.World.Player.Position=position;await Frames(4);await Capture("street-residents-"+name+"-"+font);
                }
                // Real quest guard and existing handover/dialogue; no new story branch.
                var pending=session.Snapshot with{Stage=SliceStage.CandyHeyPending,CandyCount=1,SceneId="convenience_street",PlayerPosition=new(800,520)};
                Require(session.Restore(pending).Success,"Street handover fixture invalid");
                main.World.Player.Position=new(1500,520);await Frames(3);main.World.GetTarget("soup")!.TryInteract(session);await Frames(3);
                Require(main.World.SceneId=="convenience_street","Locked soup exit transitioned");await Capture("street-soup-locked-"+font);
                if(main.Dialogue.IsOpen){KeyPress(Key.Escape);await Frames(3);}
                main.World.Player.Position=new(800,520);await Frames(4);await Capture("street-hey-before-"+font);
                main.World.GetTarget("hey")!.TryInteract(session);await Frames(3);await Finish(main);
                Require(session.Snapshot.CompletedActions.Contains("candy.hey.delivered:hey-1"),"Capture handover did not deliver candy");
                if(main.Choices.IsOpen){KeyPress(Key.Escape);await Frames(3);}
                await Capture("street-hey-after-"+font);
                main.World.Player.Position=new(1500,520);await Frames(4);await Capture("street-soup-ready-"+font);
            }finally{main.Free();await Frames(3);}
        }
        await EndVisualChecks();
        GD.Print("STREET_CAPTURE_PASS ThreeFonts SevenThoughts ThirteenJournal Regions Residents Handover ExitGuard Ending");
    }
}
