using GeXingzhou.Domain;
namespace GeXingzhou.DomainChecks;
public static class ContentChecks
{
    public static void Register(List<(string,string,Action)> tests)
    {
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
            File.WriteAllText(Path.Combine(dir,"scenes.json"),scenes); File.WriteAllText(Path.Combine(dir,"events.json"),events);
            var result=ContentCatalog.Load(dir); Check.True(!result.Success); Check.True(result.Errors.Count>0);
        }));
    }
}
