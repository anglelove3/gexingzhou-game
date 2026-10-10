using GeXingzhou.Domain;
using System.Security.Cryptography;
namespace GeXingzhou.DomainChecks;
public static class CommunitySaveChecks
{
    // These exercise protocol boundaries, independent literal coordinates and real isolated files.
    public static void Register(List<(string Group,string Name,Action Run)> tests)
    {
        void Add(string name,Action run)=>tests.Add(("CommunitySave",name,run));
        Add("NewAtSafe",()=>{
            var s=SaveV3Codec.CreateNew(new(){SubtitleSize=32},CommunityFixtures.Current());
            Check.Equal(3,s.SchemaVersion);Check.Equal("vs01-0.3",s.ContentVersion);Check.Equal(new Position2(320,460),s.PlayerPosition);Check.Equal(32,s.Settings.SubtitleSize);
        });
        Add("V2CommunityMapsLane",()=>{
            var old=Old() with {PlayerPosition=new(960,280),DiscoveredIds=new(){"soup.sign","soup.menu","soup.note"},CompletedActions=new(){"observation.community.first"},SceneActiveMilliseconds=new(){["community_gate"]=741},InvitationState=new(){Elapsed=12}};
            var result=Upgrade(old);Check.Equal(LoadStatus.Loaded,result.Status);var s=result.Snapshot!;
            Check.Equal(new Position2(960,460),s.PlayerPosition);Check.Equal(3,s.DiscoveredIds.Count);Check.True(s.CompletedActions.Contains("observation.community.first"));Check.Equal(741L,s.SceneActiveMilliseconds["community_gate"]);Check.Equal(12d,s.InvitationState.Elapsed);Check.Equal(old.PlaythroughId,s.PlaythroughId);
            foreach(var scene in new[]{"convenience_street","soup_shop"}){
                var pos=scene=="soup_shop"?new Position2(400,430):new(100,280);
                Check.Equal(pos,Upgrade(Old() with{SceneId=scene,PlayerPosition=pos}).Snapshot!.PlayerPosition);
            }
        });
        Add("V1MemoryPipeline",()=>{
            foreach(var replay in new[]{false,true})foreach(var choice in new FoodChoice?[]{null,FoodChoice.Take,FoodChoice.Wait,FoodChoice.Share}){
                var old=SaveMigrationFixtures.LegacyMemory(choice,replay);var result=Upgrade(old);Check.Equal(LoadStatus.Loaded,result.Status);var s=result.Snapshot!;
                Check.Equal(3,s.SchemaVersion);Check.Equal(new Position2(400,430),s.ReturnContext!.Position);Check.Equal(old.MemoryState!.FoodChoice,s.MemoryState!.FoodChoice);Check.Equal(old.MemoryState.Replay,s.MemoryState.Replay);Check.Equal(old.MemoryState.InstanceId,s.MemoryState.InstanceId);Check.Equal(old.MemoryVisitOrdinal,s.MemoryVisitOrdinal);Check.Equal(old.Stage,s.Stage);
            }
        });
        Add("FrozenV2RejectsCommunityId",()=>{
            var old=Old() with {DiscoveredIds=new(){"community.sign"}};
            Check.Equal(LoadStatus.Corrupt,new SaveV2Codec(CommunityFixtures.FrozenV2()).Read(CommunityFixtures.Json(old)).Status);
            Check.Equal(LoadStatus.Corrupt,Upgrade(old).Status);
            var all=CommunityFixtures.V3() with{DiscoveredIds=new(){"community.sign","community.notice","community.planter","soup.sign","soup.menu","soup.note"}};
            Check.Equal(LoadStatus.Loaded,Codec().Read(CommunityFixtures.Json(all)).Status);
            Check.Equal(LoadStatus.Corrupt,Codec().Read(CommunityFixtures.Json(all with{DiscoveredIds=new(){"community.unknown"}})).Status);
        });
        Add("BackupOnlyPreventsDowngrade",()=>{
            var r=Repos();File.WriteAllText(Path.Combine(r.CurrentDir,"save.bak.json"),CommunityFixtures.Json(CommunityFixtures.V3()));
            Check.True(r.V2.Save(Old()).Success);Check.Equal(VersionedResumeSource.Blocked,Probe(r).Source);Check.Equal(LoadStatus.NotFound,Probe(r).Result.Status);
            Check.Equal(VersionedResumeSource.Current,Probe(r,true).Source);
            var other=Repos();Check.True(other.V1.Save(new()).Success);File.WriteAllText(Path.Combine(other.V2Dir,"save.bak.json"),CommunityFixtures.Json(Old()));
            Check.Equal(VersionedResumeSource.Blocked,Probe(other).Source);Check.Equal(VersionedResumeSource.V2,Probe(other,true).Source);
        });
        Add("BadCurrentBlocksOld",()=>{
            foreach(var payload in new[]{"broken",CommunityFixtures.Json(CommunityFixtures.V3() with{SchemaVersion=9})}){
                var r=Repos();File.WriteAllText(Path.Combine(r.CurrentDir,"save.json"),payload);Check.True(r.V2.Save(Old()).Success);Check.Equal(VersionedResumeSource.Blocked,Probe(r).Source);
            }
            var io=Repos();var occupied=Path.Combine(io.CurrentDir,"occupied");File.WriteAllText(occupied,"not a directory");
            var result=SaveV3UpgradePolicy.Probe(new SaveRepository(occupied,codec:Codec()),io.V2,io.V1);
            Check.Equal(VersionedResumeSource.Blocked,result.Source);Check.Equal(LoadStatus.IoError,result.Result.Status);
        });
        Add("ExactBackupSlot",()=>{
            var r=Repos();Check.Equal(VersionedResumeSource.Missing,Probe(r).Source);
            Check.True(r.V1.Save(new()).Success);Check.True(r.V1.Save(new WorldSnapshot() with{PlayerPosition=new(500,280)}).Success);
            Check.Equal(new Position2(320,280),Probe(r,true).Result.Snapshot!.PlayerPosition);
            Check.True(r.V2.Save(Old()).Success);Check.Equal(VersionedResumeSource.V2,Probe(r).Source);Check.Equal(VersionedResumeSource.Blocked,Probe(r,true).Source);
            Check.True(r.V2.Save(Old() with{PlayerPosition=new(800,280)}).Success);Check.Equal(new Position2(320,280),Probe(r,true).Result.Snapshot!.PlayerPosition);
            var manual=Repos("manual");Check.True(manual.V1.Save(new()).Success);Check.Equal(VersionedResumeSource.Legacy,Probe(manual).Source);Check.Equal(VersionedResumeSource.Blocked,Probe(manual,true).Source);
            File.WriteAllText(Path.Combine(manual.CurrentDir,"save.json"),"other slot");Check.Equal(VersionedResumeSource.Legacy,Probe(manual).Source);
            Check.True(manual.Current.Save(CommunityFixtures.V3()).Success);Check.Equal(VersionedResumeSource.Current,Probe(manual).Source);
        });
        Add("CorruptPositionsRejected",()=>{
            foreach(var pos in new[]{new Position2(-1,280),new(1593,280),new(float.NaN,280),new(float.PositiveInfinity,280),new(320,460)})Check.Equal(LoadStatus.Corrupt,Upgrade(Old() with{PlayerPosition=pos}).Status);
            foreach(var pos in new[]{new Position2(15,460),new(320,299),new(float.NaN,460),new(320,float.NegativeInfinity)})Check.True(Codec().Validate(CommunityFixtures.V3() with{PlayerPosition=pos})!=null);
            var blocked=CommunityFixtures.V3() with{PlayerPosition=new(960,380)};Check.True(Codec().Validate(blocked)!=null);
            var relocated=Codec().Read(CommunityFixtures.Json(blocked));Check.Equal(LoadStatus.Loaded,relocated.Status);Check.Equal(new Position2(320,460),relocated.Snapshot!.PlayerPosition);
            Check.Equal(LoadStatus.Corrupt,Codec().Read(CommunityFixtures.Json(CommunityFixtures.V3() with{SceneId="unknown"})).Status);
        });
        Add("CollectionsCopied",()=>{
            var old=SaveMigrationFixtures.LegacyMemory(FoodChoice.Share,false);var s=Upgrade(old).Snapshot!;
            s.CompletedActions.Clear();s.ChoiceCodes.Clear();s.DiscoveredIds.Add("community.sign");s.SceneActiveMilliseconds.Clear();s.MemoryState!.PushedCoinIds.Clear();
            Check.True(old.CompletedActions.Count>0);Check.True(old.ChoiceCodes.Count>0);Check.Equal(0,old.DiscoveredIds.Count);Check.Equal(2,old.SceneActiveMilliseconds.Count);Check.Equal(4,old.MemoryState!.PushedCoinIds.Count);
            var v2=Old() with{DiscoveredIds=new(){"soup.sign"}};var v3=Upgrade(v2).Snapshot!;v3.DiscoveredIds.Clear();Check.Equal(1,v2.DiscoveredIds.Count);
        });
        Add("SourceHashesUnchanged",()=>{
            var r=Repos();Check.True(r.V2.Save(Old()).Success);Check.True(r.V2.Save(Old() with{PlayerPosition=new(720,280)}).Success);Check.True(r.V1.Save(new()).Success);Check.True(r.V1.Save(new()).Success);
            File.WriteAllText(Path.Combine(r.V2Dir,"preferences.json"),"private old preferences");
            var files=Directory.GetFiles(r.V2Dir).Concat(Directory.GetFiles(r.V1Dir)).Append("content/vs01/compat/navigation-v2.json").ToArray();
            var before=files.Select(Hash).ToArray();Check.Equal("517BBC24C6CDC04F6186A7A699766DC5FA7D640417E12E4952CC421C3154E280",Hash(files[^1]));
            var source=Probe(r).Result.Snapshot!;Check.True(r.Current.Save(Upgrade(source).Snapshot!).Success);Probe(r,true);
            for(int i=0;i<files.Length;i++)Check.Equal(before[i],Hash(files[i]));
        });
        Add("DuplicateDiscoveryJsonRejects",()=>{
            Check.Equal(LoadStatus.Loaded,Codec().Read(CommunityFixtures.Json(CommunityFixtures.V3())).Status);
            var text=CommunityFixtures.Json(CommunityFixtures.V3()).Replace("\"discovered_ids\":[]","\"discovered_ids\":[\"community.sign\",\"community.sign\"]");
            Check.Equal(LoadStatus.Corrupt,Codec().Read(text).Status);
        });
        Add("DuplicateRootRejects",()=>{
            Check.Equal(LoadStatus.Loaded,Codec().Read(CommunityFixtures.Json(CommunityFixtures.V3())).Status);
            var text=CommunityFixtures.Json(CommunityFixtures.V3());Check.Equal(LoadStatus.Corrupt,Codec().Read(text.Insert(1,"\"scene_id\":\"community_gate\",")).Status);
            Check.Equal(LoadStatus.Corrupt,Codec().Read(text.Insert(1,"\"unknown\":true,")).Status);
        });
        Add("WrongV3Version",()=>{
            Check.Equal(LoadStatus.UnsupportedVersion,Codec().Read(CommunityFixtures.Json(Old())).Status);
            Check.Equal(LoadStatus.UnsupportedVersion,Codec().Read("{\"schema_version\":4,\"content_version\":\"vs01-0.4\",\"future\":true}").Status);
            Check.Equal(LoadStatus.Corrupt,Codec().Read("{\"schema_version\":3,\"content_version\":\"vs01-0.3\"}").Status);
        });
        tests.Add(("CommunityPreferences","ParentIsFile",()=>{
            var root=CommunityFixtures.DirectoryFor("preferences");var occupied=Path.Combine(root,"preferences");File.WriteAllText(occupied,"do not overwrite");
            var settings=new SettingsRepository(occupied);Check.Equal(24,settings.Load().SubtitleSize);Check.True(settings.Message.Length>0);Check.Equal("do not overwrite",File.ReadAllText(occupied));
        }));
    }
    private static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static WorldSnapshot Old()=>SaveV2Codec.CreateNew(new());
    private static SaveV3Codec Codec()=>new(CommunityFixtures.Current());
    private static LoadResult Upgrade(WorldSnapshot s)=>SaveV3Adapter.Upgrade(s,CommunityFixtures.FrozenV2(),CommunityFixtures.Current());
    private sealed record Repositories(string CurrentDir,string V2Dir,SaveRepository Current,SaveRepository V2,SaveRepository V1,string V1Dir);
    private static Repositories Repos(string slot="auto")
    {
        var root=CommunityFixtures.DirectoryFor("slots");var dirs=new[]{"v3","v2","v1"}.Select(v=>Path.Combine(root,v)).ToArray();foreach(var dir in dirs)Directory.CreateDirectory(dir);
        return new(dirs[0],dirs[1],new(dirs[0],slot,Codec()),new(dirs[1],slot,new SaveV2Codec(CommunityFixtures.FrozenV2())),new(dirs[2],slot),dirs[2]);
    }
    private static VersionedResumeOffer Probe(Repositories r,bool backup=false)=>SaveV3UpgradePolicy.Probe(r.Current,r.V2,r.V1,backup);
}
