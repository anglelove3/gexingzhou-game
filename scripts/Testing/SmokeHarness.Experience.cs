using Godot;
using GeXingzhou.Domain;
using System.Text.Json;

public partial class SmokeHarness
{
    private async Task ExperienceGuidanceChecks()
    {
        var s=GetNode<GameSession>("/root/GameSession");s.SetOptions(new(){TextSpeed=0,ReducedMotion=true},false);s.NewGame();
        s.AdvanceClock(7.99);Require(!s.Snapshot.InvitationState.VoiceReceived,"Voice before eight seconds");
        s.Flow=FlowState.Phone;s.AdvanceClock(10);Require(!s.Snapshot.InvitationState.VoiceReceived,"Phone advanced invitation");
        s.Flow=FlowState.Dialogue;s.AdvanceClock(10);Require(!s.Snapshot.InvitationState.VoiceReceived,"Dialogue advanced invitation");
        s.Flow=FlowState.Field;s.AdvanceClock(.01);Require(s.Snapshot.InvitationState.VoiceReceived,"Voice missing at eight seconds");
        s.AdvanceClock(15);Require(s.Snapshot.InvitationState.PhoneRinging&&!s.Snapshot.InvitationState.CarArrived,"Call interval changed");
        s.AdvanceClock(30);Require(s.Snapshot.InvitationState.CarArrived,"Ignored phone stranded player");
        var main=await NewPolishMain();var hint=main.GetNodeOrNull<Label>("HUD/Guidance");Require(hint!=null,"Missing authored guidance label");
        main.World.Player.Position=new(160,400);await Frames(3);
        Require(hint!.Visible&&hint.Text.Contains("E 观察"),"Missing first observation hint");await Capture("guidance-first");
        var oldSign=main.World.GetTarget("old_sign")!;main.ShowObservation(oldSign);main.Dialogue.HandleKey(Key.Escape);await Frames(2);
        Require(!s.Snapshot.CompletedActions.Contains("observation.community.first"),"Cancelled observation consumed");
        main.World.Player.Position=new(320,460);await Frames(3);Require(!hint.Visible,"Hint retained far from object");
        s.AdvanceClock(53);main.World.Player.Position=new(400,440);await Frames(3);
        var cannon=main.World.GetTarget("cannon")!.GetNodeOrNull<Label>("NameLabel");Require(cannon is {Visible:true}&&cannon.Text=="张大炮","Missing near actor name");
        Require(hint.Visible&&hint.Text.Contains("Tab"),"Missing first message hint");await Capture("guidance-cannon");
        main.World.Player.Position=new(1480,460);await Frames(3);
        var exit=main.World.GetNodeOrNull<Label>("StreetExit/NameLabel");Require(exit is {Visible:true}&&exit.Text.Contains("便利店街"),"Missing exit destination");Require(!cannon!.Visible,"Distant actor name stayed visible");await Capture("guidance-exit");
        main.World.Player.Position=new(800,460);await Frames(3);Require(!exit!.Visible,"Distant exit name stayed visible");
        main.ChangeWorld("convenience_street",new(600,280));await Frames(3);Require(main.World.GetNode<Label>("Hey/NameLabel").Visible,"Hey name absent");
        main.ChangeWorld("soup_shop",new(400,430));await Frames(3);Require(main.World.GetNode<Label>("DepthLayers/Actors/Cannon/NameLabel").Visible,"Soup actor name absent");
        main.Free();await Frames(2);
    }
    private async Task ExperienceDialogueChecks()
    {
        var session=GetNode<GameSession>("/root/GameSession");
        foreach(var size in new[]{20,24,32})
        {
            session.SetOptions(new(){SubtitleSize=size,TextSpeed=0,ReducedMotion=true},false);
            var main=await NewPolishMain();
            var names=main.Dialogue.GetNode<Label>("Panel/Content/NameLabel");
            var callbacks=0;
            Require(main.Dialogue.Open("soup.tomorrow",()=>callbacks++),"Cannot open tomorrow");
            await GameTime(.17);Require(names.Text=="张大炮","Wrong first speaker");
            main.Dialogue.HandleKey(Key.Enter);await GameTime(.17);
            Require(names.Text=="葛行舟","Ge line attributed to Cannon");AssertDialogueFits(main);
            await Capture("dialogue-spoken-"+size);
            main.Dialogue.HandleKey(Key.Escape);await Frames(2);Require(callbacks==0,"Cancelled dialogue committed");
            var metadataJson="""{"Speaker":"葛行舟","Lines":["我还没有想清楚。","风从旧街口吹过来。"],"LineMetadata":[{"Kind":"Thought"},{"Kind":"Narration"}]}""";
            var sample=JsonSerializer.Deserialize<DialogueNode>(metadataJson)!;
            session.Catalog!.Dialogues=new Dictionary<string,DialogueNode>(session.Catalog.Dialogues){["experience.sample"]=sample};
            Require(main.Dialogue.Open("experience.sample"),"Cannot open thought sample");await GameTime(.17);
            Require(names.Text=="葛行舟·心声","Missing thought identity");AssertDialogueFits(main);await Capture("dialogue-thought-"+size);
            main.Dialogue.HandleKey(Key.Enter);await GameTime(.17);
            Require(names.Text=="旁白","Narration attributed to NPC");AssertDialogueFits(main);await Capture("dialogue-narration-"+size);
            main.Dialogue.HandleKey(Key.Escape);main.Free();await Frames(2);
        }
    }
}
