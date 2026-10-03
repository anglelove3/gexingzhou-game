using GeXingzhou.Domain;
namespace GeXingzhou.DomainChecks;
public static class SaveChecks
{
    private static string DirectoryFor(string purpose){var path=Path.Combine("test-output",purpose+"-"+Guid.NewGuid());Directory.CreateDirectory(path);return Path.GetFullPath(path);}
    public static void Register(List<(string,string,Action)> tests)
    {
        tests.Add(("Save","CheckpointRoundTripsAndBackup",()=>{
            var dir=DirectoryFor("save");var repo=new SaveRepository(dir);Check.Equal(LoadStatus.NotFound,repo.Load().Status);
            var s=new WorldSnapshot{Stage=SliceStage.CandyHeyPending,CandyCount=1};Check.True(repo.Save(s).Success);
            var first=repo.Load();Check.Equal(LoadStatus.Loaded,first.Status);Check.Equal(1,first.Snapshot!.CandyCount);
            s=s with {Stage=SliceStage.CandyHeyDelivered,CandyCount=0,SceneId="convenience_street"};Check.True(repo.Save(s).Success);
            Check.Equal(SliceStage.CandyHeyDelivered,repo.Load().Snapshot!.Stage);Check.Equal(1,repo.LoadBackup().Snapshot!.CandyCount);
            Check.True(File.ReadAllText(Path.Combine(dir,"save.json")).Contains("candy_hey_delivered"));
        }));
        tests.Add(("Save","CorruptMainRetainsBackupAndRefusesOverwrite",()=>{
            var dir=DirectoryFor("corrupt");var repo=new SaveRepository(dir);Check.True(repo.Save(new()).Success);Check.True(repo.Save(new(){PlayerPosition=new(400,280)}).Success);
            var path=Path.Combine(dir,"save.json");File.WriteAllText(path,"{broken");
            Check.Equal(LoadStatus.Corrupt,repo.Load().Status);Check.Equal(LoadStatus.Loaded,repo.LoadBackup().Status);Check.True(!repo.Save(new()).Success);Check.Equal("{broken",File.ReadAllText(path));
            Check.True(repo.PreserveForNewGame().Success);Check.True(repo.Save(new()).Success);Check.True(Directory.GetFiles(dir,"preserved-*.json").Any(p=>File.ReadAllText(p)=="{broken"));
        }));
        tests.Add(("Save","UnsupportedVersionsRemainUntouched",()=>{
            foreach(var json in new[]{"{\"schema_version\":2,\"content_version\":\"vs01-0.1\"}","{\"schema_version\":1,\"content_version\":\"future\"}"})
            {var dir=DirectoryFor("version");var path=Path.Combine(dir,"save.json");File.WriteAllText(path,json);var repo=new SaveRepository(dir);Check.Equal(LoadStatus.UnsupportedVersion,repo.Load().Status);Check.True(!repo.Save(new()).Success);Check.Equal(json,File.ReadAllText(path));}
        }));
        tests.Add(("Save","UnavailableDirectoryDoesNotCrash",()=>{
            var dir=DirectoryFor("blocked");var file=Path.Combine(dir,"not-a-directory");File.WriteAllText(file,"keep");Check.True(!new SaveRepository(file).Save(new()).Success);Check.Equal("keep",File.ReadAllText(file));
        }));
        tests.Add(("Save","MalformedMemoryDoesNotThrow",()=>{
            var dir=DirectoryFor("null-memory");File.WriteAllText(Path.Combine(dir,"save.json"),"{\"schema_version\":1,\"content_version\":\"vs01-0.1\",\"scene_id\":\"soup_shop\",\"stage\":\"soup_meet\",\"memory_state\":{\"instance_id\":null,\"pushed_coin_ids\":[]}}");Check.Equal(LoadStatus.Corrupt,new SaveRepository(dir).Load().Status);
        }));
        tests.Add(("Resume","MemoryAndPhoneRestoreSafe",()=>{
            var dir=DirectoryFor("memory");var repo=new SaveRepository(dir);var m=MemorySession.PushCoin(new(),"c4");
            var s=new WorldSnapshot{SceneId="memory_soup_table",Stage=SliceStage.MemoryActive,MemoryState=m,MemoryOrdinal=1,ReturnContext=new("soup_shop",new(440,280),"soup.return",true),PhoneState="open"};
            Check.True(repo.Save(s).Success);var normalized=ResumePolicy.Normalize(repo.Load().Snapshot!);
            Check.Equal("closed",normalized.PhoneState);Check.Equal(2,normalized.MemoryState!.PushedTotal);Check.Equal("soup-1",normalized.MemoryState.InstanceId);Check.Equal(440f,normalized.ReturnContext!.Position.X);Check.Equal(SliceStage.MemoryActive,normalized.Stage);
        }));
    }
}
