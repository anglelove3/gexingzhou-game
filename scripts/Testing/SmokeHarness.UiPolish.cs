using Godot;
using GeXingzhou.Domain;
public partial class SmokeHarness
{
    private async Task UiPolishChecks()
    {
        var s=GetNode<GameSession>("/root/GameSession");
        foreach(var font in new[]{20,24,32})
        {
            s.SetOptions(new(){SubtitleSize=font,TextSpeed=0},false);
            var main=await NewPolishMain();
            main.Dialogue.ShowText("葛行舟",string.Join('\n',Enumerable.Repeat("统一搬来的楼，一排一排。人也整齐了，记忆没那么听话。",8)),kind:DialogueLineKind.Thought);
            await Frames(6);
            var body=main.Dialogue.GetNode<ScrollContainer>("Panel/Content/Body");
            var label=main.Dialogue.GetNode<Label>("Panel/Content/Body/Text");
            var hint=main.Dialogue.GetNode<Label>("Panel/Content/ContinueHint");
            var line=label.GetThemeFont("font").GetHeight(font)+label.GetThemeConstant("line_spacing");
            GD.Print($"POLISH_BODY font={font} body={body.Size.Y} line={line} hint={hint.GetGlobalRect()}");
            Require(body.Size.Y>=2*line-1,"Dialogue cannot display two complete text lines at font "+font);
            Require(body.GetGlobalRect().End.Y<=hint.GetGlobalRect().Position.Y,"Dialogue body overlaps continue hint");
            Require(main.GetGlobalRect().Encloses(hint.GetGlobalRect()),"Continue hint outside screen");
            main.Dialogue.HandleKey(Key.End);await Frames(3);
            Require(body.ScrollVertical>0,"Long dialogue cannot reach end with End key");
            main.Dialogue.HandleKey(Key.Home);await Frames(3);Require(body.ScrollVertical==0,"Dialogue Home cannot return to start");
            main.Dialogue.HandleKey(Key.Escape);main.Free();await Frames(3);
        }
    }
}
