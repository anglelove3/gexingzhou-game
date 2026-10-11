using Godot;
using GeXingzhou.Domain;
public partial class SmokeHarness
{
    private async Task FeedbackVisualChecks()
    {
        var session=GetNode<GameSession>("/root/GameSession");
        var boot=GD.Load<PackedScene>("res://scenes/Boot.tscn").Instantiate<BootMenu>();AddChild(boot);await Frames(4);await Capture("feedback-menu");boot.Free();await Frames(3);
        await CommunityNoticeWallDepth();await CommunityAmbientResidents();await CommunityStreetWayfinding();
        foreach(var font in new[]{20,24,32})
        {
            session.SetOptions(new(){SubtitleSize=font,TextSpeed=0,ReducedMotion=true},false);
            var main=await NewPolishMain();
            try
            {
                await Capture("feedback-entry-"+font);
                main.Pause.Open();await Frames(3);await Capture("feedback-pause-"+font);
                main.Settings.Open();await Frames(3);
                main.Settings.GetNode<ScrollContainer>("Panel/Scroll").EnsureControlVisible(main.Settings.GetNode<HSlider>("Panel/Scroll/Content/MasterVolume"));
                await Frames(3);await Capture("feedback-settings-"+font);main.Settings.Close();main.Pause.Close();
                var bench=(BenchView)main.World.GetTarget("bench")!;main.World.Player.Position=bench.StandAnchor.Position+bench.Position;
                Require(main.Rest.Begin(bench),"Feedback bench cannot sit");await WaitForPhase(main,RestPhase.Seated);await Capture("feedback-rest-"+font);
                main.Dialogue.ShowText("葛行舟",string.Join('\n',Enumerable.Repeat("统一搬来的楼，一排一排。人也整齐了，记忆没那么听话。",8)),kind:DialogueLineKind.Thought);
                await Frames(4);await Capture("feedback-thought-"+font);main.Dialogue.HandleKey(Key.End);await Frames(3);await Capture("feedback-thought-end-"+font);
                main.Dialogue.HandleKey(Key.Escape);
            }
            finally{main.Free();await Frames(2);}
        }
    }
}
