using Godot;
using GeXingzhou.Domain;
using System.Text.Json;

public partial class SmokeHarness
{
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
            await Frames(12);Require(names.Text=="张大炮","Wrong first speaker");
            main.Dialogue.HandleKey(Key.Enter);await Frames(12);
            Require(names.Text=="葛行舟","Ge line attributed to Cannon");AssertDialogueFits(main);
            await Capture("dialogue-spoken-"+size);
            main.Dialogue.HandleKey(Key.Escape);await Frames(2);Require(callbacks==0,"Cancelled dialogue committed");
            var metadataJson="""{"Speaker":"葛行舟","Lines":["我还没有想清楚。","风从旧街口吹过来。"],"LineMetadata":[{"Kind":"Thought"},{"Kind":"Narration"}]}""";
            var sample=JsonSerializer.Deserialize<DialogueNode>(metadataJson)!;
            session.Catalog!.Dialogues=new Dictionary<string,DialogueNode>(session.Catalog.Dialogues){["experience.sample"]=sample};
            Require(main.Dialogue.Open("experience.sample"),"Cannot open thought sample");await Frames(12);
            Require(names.Text=="葛行舟·心声","Missing thought identity");AssertDialogueFits(main);await Capture("dialogue-thought-"+size);
            main.Dialogue.HandleKey(Key.Enter);await Frames(12);
            Require(names.Text=="旁白","Narration attributed to NPC");AssertDialogueFits(main);await Capture("dialogue-narration-"+size);
            main.Dialogue.HandleKey(Key.Escape);main.Free();await Frames(2);
        }
    }
}
