using GeXingzhou.Domain;
namespace GeXingzhou.DomainChecks;
public static class ContentChecks
{
    public static void Register(List<(string,string,Action)> tests)
    {
        foreach(var missing in new[]{"soup.return.take","soup.return.wait","soup.return.share","observation.community.quiet","observation.community.answered","observation.community.unanswered"})
        tests.Add(("Content","RequiresBranch_"+missing,()=>{
            var dialogues=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText("content/vs01/dialogues.json"))!.AsObject();
            foreach(var id in new[]{"soup.return.take","soup.return.wait","soup.return.share","observation.community.quiet","observation.community.answered","observation.community.unanswered"})
                dialogues[id]=System.Text.Json.JsonSerializer.SerializeToNode(new DialogueNode("测试",new[]{"分支内容"}));
            dialogues.Remove(missing);
            var result=ContentCatalog.LoadText(name=>name=="dialogues.json"?dialogues.ToJsonString():File.ReadAllText(Path.Combine("content/vs01",name)));
            Check.True(!result.Success);Check.True(result.Errors.Any(e=>e.Contains(missing)));
        }));
        tests.Add(("Content","RejectsNullOrMissingDialogue",()=>{
            foreach(var bad in new[]{"{\"soup.start\":null}","{}"}) {
                var result=ContentCatalog.LoadText(name=>name=="dialogues.json"?bad:File.ReadAllText(Path.Combine("content/vs01",name)));
                Check.True(!result.Success);Check.True(result.Catalog==null);
            }
        }));
        tests.Add(("Content","LoadsValidContract", () => {
            var loaded = ContentCatalog.Load("content/vs01"); Check.True(loaded.Success);
            Check.Equal(112d,loaded.Catalog!.Parameters["move.walk_speed"]);
            Check.Equal(168d,loaded.Catalog.Parameters["move.run_speed"]);
            Check.Equal(20d,loaded.Catalog.Parameters["invitation.voice_delay"]);
        }));
        tests.Add(("Content","ReportsMissingDirectory", () => Check.True(!ContentCatalog.Load("test-output/missing-content").Success)));
        foreach (var (name, scenes, events) in new[] {
            ("MissingSceneId","[{\"width\":1600,\"stage\":\"free_arrival\"}]","[\"ok\"]"),
            ("UnknownStage","[{\"scene_id\":\"x\",\"width\":100,\"stage\":\"impossible\"}]","[\"ok\"]"),
            ("DuplicateEvent","[{\"scene_id\":\"x\",\"width\":100,\"stage\":\"free_arrival\"}]","[\"a\",\"a\"]") })
        tests.Add(("Content",name,()=>{
            var dir=Path.Combine("test-output",Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
            File.Copy("content/vs01/parameters.json",Path.Combine(dir,"parameters.json"));
            File.Copy("content/vs01/dialogues.json",Path.Combine(dir,"dialogues.json"));
            File.WriteAllText(Path.Combine(dir,"scenes.json"),scenes); File.WriteAllText(Path.Combine(dir,"events.json"),events);
            var result=ContentCatalog.Load(dir); Check.True(!result.Success); Check.True(result.Errors.Count>0);
        }));
    }
}
