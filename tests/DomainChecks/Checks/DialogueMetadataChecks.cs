using GeXingzhou.Domain;
using System.Text.Json.Nodes;
namespace GeXingzhou.DomainChecks;

public static class DialogueMetadataChecks
{
    private static ContentLoadResult Load(string node)
    {
        var content=JsonNode.Parse(File.ReadAllText("content/vs01/dialogues.json"))!.AsObject();
        content["soup.tomorrow"]=JsonNode.Parse(node);
        return ContentCatalog.LoadText(name=>name=="dialogues.json"?content.ToJsonString():File.ReadAllText("content/vs01/"+name));
    }
    private static string Kind(DialogueSession session)=>session.Kind.ToString();
    public static void Register(List<(string,string,Action)> tests)
    {
        tests.Add(("Dialogue","LegacyMetadataDefaults",()=>{
            var c=Load("""{"Speaker":"朋友","Lines":["第一句","第二句"]}""");
            Check.True(c.Success);var s=new DialogueSession();Check.True(s.Start("soup.tomorrow",c.Catalog!).Success);
            Check.Equal("朋友",s.Speaker);Check.Equal("Spoken",Kind(s));
            s.Tick(1,0);s.Advance(.2);s.Tick(1,0);Check.Equal("朋友",s.Speaker);
        }));
        tests.Add(("Dialogue","SpeakerAndKindChangePerLine",()=>{
            var c=Load("""{"Speaker":"张大炮","Lines":["早点来。","我尽量。","他没再追问。","我还不知道。"],"LineMetadata":[{"Kind":"Spoken"},{"Speaker":"葛行舟","Kind":"Spoken"},{"Kind":"Narration"},{"Speaker":"葛行舟","Kind":"Thought"}]}""");
            Check.True(c.Success);var s=new DialogueSession();Check.True(s.Start("soup.tomorrow",c.Catalog!).Success);
            Check.Equal("张大炮",s.Speaker);Check.Equal("Spoken",Kind(s));
            s.Tick(1,0);s.Advance(.2);Check.Equal("葛行舟",s.Speaker);Check.Equal("Spoken",Kind(s));
            s.Tick(1,0);s.Advance(.4);Check.Equal("Narration",Kind(s));
            s.Tick(1,0);s.Advance(.6);Check.Equal("Thought",Kind(s));
            s.Cancel();Check.True(!s.Advance(10).Applied);
        }));
        foreach(var (name,metadata) in new[]{
            ("RejectsMetadataLength","[]"),
            ("RejectsNullMetadataEntry","[null]"),
            ("RejectsUnknownKind","[{\"Kind\":\"unknown\"}]"),
            ("RejectsIntegerKind","[{\"Kind\":999}]"),
            ("RejectsEmptySpeaker","[{\"Speaker\":\" \"}]")})
        tests.Add(("Dialogue",name,()=>{
            var c=Load("{\"Speaker\":\"张大炮\",\"Lines\":[\"早点来。\"],\"LineMetadata\":"+metadata+"}");
            Check.True(!c.Success);Check.True(c.Catalog==null);Check.True(c.Errors.Any(e=>e.Contains("soup.tomorrow")));
        }));
    }
}
