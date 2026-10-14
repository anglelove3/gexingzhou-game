using GeXingzhou.Domain;
using System.Security.Cryptography;
namespace GeXingzhou.DomainChecks;
public static class StreetSaveChecks
{
    public static void Register(List<(string Group,string Name,Action Run)> tests)
    {
        tests.Add(("StreetSave","NewAtCommunitySafe",()=>
        {
            var type=typeof(WorldSnapshot).Assembly.GetType("GeXingzhou.Domain.SaveV4Codec");
            Check.True(type!=null);
            var method=type!.GetMethod("CreateNew",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
            Check.True(method!=null);
            var snapshot=(WorldSnapshot)method!.Invoke(null,new object[]{new GameSettings{SubtitleSize=32},StreetFixtures.Current()})!;
            Check.Equal(4,snapshot.SchemaVersion);Check.Equal("vs01-0.4",snapshot.ContentVersion);
            Check.Equal(new Position2(320,460),snapshot.PlayerPosition);Check.Equal(32,snapshot.Settings.SubtitleSize);
        }));
        tests.Add(("StreetSave","StrictV4DiscoveriesAndDuplicates",()=>
        {
            var codec=new SaveV4Codec(StreetFixtures.Current());
            var ids=new HashSet<string>{"community.sign","community.notice","community.planter","street.doorplate","street.newsstand","street.bike_shed","street.drain","street.wall_paint","street.laundry_rack","street.stool","soup.sign","soup.menu","soup.note"};
            var valid=StreetFixtures.V4() with{DiscoveredIds=ids};
            Check.Equal(LoadStatus.Loaded,codec.Read(StreetFixtures.Json(valid)).Status);
            var duplicate=StreetFixtures.Json(StreetFixtures.V4()).Replace("\"discovered_ids\":[]","\"discovered_ids\":[\"street.stool\",\"street.stool\"]");
            Check.Equal(LoadStatus.Corrupt,codec.Read(duplicate).Status);
            var text=StreetFixtures.Json(StreetFixtures.V4());
            Check.Equal(LoadStatus.Corrupt,codec.Read(text.Insert(1,"\"scene_id\":\"community_gate\",")).Status);
            Check.Equal(LoadStatus.Corrupt,codec.Read(text.Insert(1,"\"unknown\":true,")).Status);
            Check.Equal(LoadStatus.Corrupt,codec.Read(StreetFixtures.Json(valid with{DiscoveredIds=new(){"street.unknown"}})).Status);
            Check.Equal(LoadStatus.UnsupportedVersion,codec.Read("{\"schema_version\":5,\"content_version\":\"vs01-0.5\",\"future\":true}").Status);
        }));
        tests.Add(("StreetSave","InvalidVsBlockedPositions",()=>
        {
            var codec=new SaveV4Codec(StreetFixtures.Current());
            foreach(var position in new[]{new Position2(-1,520),new(1593,520),new(float.NaN,520),new(240,float.PositiveInfinity)})
                Check.True(codec.Validate(StreetFixtures.V4() with{SceneId="convenience_street",PlayerPosition=position})!=null);
            var blocked=StreetFixtures.V4() with{SceneId="convenience_street",PlayerPosition=new Position2(800,400)};
            Check.True(codec.Validate(blocked)!=null);
            var relocated=codec.Read(StreetFixtures.Json(blocked));
            Check.Equal(LoadStatus.Loaded,relocated.Status);Check.Equal(new Position2(240,520),relocated.Snapshot!.PlayerPosition);
            Check.Equal(LoadStatus.Corrupt,codec.Read(StreetFixtures.Json(StreetFixtures.V4() with{SceneId="unknown"})).Status);
        }));
        tests.Add(("StreetSave","V3StreetMapsMainLine",()=>
        {
            var type=typeof(WorldSnapshot).Assembly.GetType("GeXingzhou.Domain.SaveV4Adapter");Check.True(type!=null);
            var method=type!.GetMethod("Upgrade",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);Check.True(method!=null);
            var source=CommunityFixtures.V3() with{SceneId="convenience_street",PlayerPosition=new Position2(800,280)};
            var result=(LoadResult)method!.Invoke(null,new object[]{source,CommunityFixtures.FrozenV2(),StreetFixtures.FrozenV3(),StreetFixtures.Current()})!;
            Check.Equal(LoadStatus.Loaded,result.Status);Check.Equal(4,result.Snapshot!.SchemaVersion);
            Check.Equal("vs01-0.4",result.Snapshot.ContentVersion);Check.Equal(new Position2(800,520),result.Snapshot.PlayerPosition);
        }));
        tests.Add(("StreetSave","StreetBlockedMappingUsesSafe",()=>
        {
            var source=CommunityFixtures.V3() with{SceneId="convenience_street",PlayerPosition=new Position2(800,280)};
            var result=SaveV4Adapter.Upgrade(source,CommunityFixtures.FrozenV2(),StreetFixtures.FrozenV3(),StreetFixtures.CurrentWithStreetBlock(800));
            Check.Equal(LoadStatus.Loaded,result.Status);Check.Equal(new Position2(240,520),result.Snapshot!.PlayerPosition);
            Check.True(result.Message.Contains("安全位置"));
        }));
        tests.Add(("StreetSave","V1V2PipelineToV4",()=>
        {
            var v2=SaveV2Codec.CreateNew(new(){SubtitleSize=20}) with{PlayerPosition=new Position2(900,280),DiscoveredIds=new(){"soup.sign"}};
            var fromV2=Upgrade(v2);Check.Equal(LoadStatus.Loaded,fromV2.Status);Check.Equal(4,fromV2.Snapshot!.SchemaVersion);
            Check.Equal(new Position2(900,460),fromV2.Snapshot.PlayerPosition);Check.True(fromV2.Snapshot.DiscoveredIds.SetEquals(new[]{"soup.sign"}));
            foreach(var replay in new[]{false,true})foreach(var choice in new FoodChoice?[]{null,FoodChoice.Take,FoodChoice.Wait,FoodChoice.Share})
            {
                var legacy=SaveMigrationFixtures.LegacyMemory(choice,replay);var result=Upgrade(legacy);Check.Equal(LoadStatus.Loaded,result.Status);
                Check.Equal(4,result.Snapshot!.SchemaVersion);Check.Equal(legacy.MemoryState!.FoodChoice,result.Snapshot.MemoryState!.FoodChoice);
                Check.Equal(legacy.MemoryState.Replay,result.Snapshot.MemoryState.Replay);Check.Equal(new Position2(400,430),result.Snapshot.ReturnContext!.Position);
            }
        }));
        tests.Add(("StreetSave","CollectionsCopiedWithoutNewDiscoveries",()=>
        {
            var source=CommunityFixtures.V3() with
            {
                DiscoveredIds=new(){"community.sign","community.notice","community.planter","soup.sign","soup.menu","soup.note"},
                CompletedActions=new(){"observation.community.first"},ChoiceCodes=new(),SceneActiveMilliseconds=new(){["convenience_street"]=999}
            };
            var target=Upgrade(source).Snapshot!;
            Check.Equal(6,target.DiscoveredIds.Count);Check.True(target.DiscoveredIds.All(id=>!id.StartsWith("street.",StringComparison.Ordinal)));
            target.DiscoveredIds.Clear();target.CompletedActions.Clear();target.SceneActiveMilliseconds.Clear();
            Check.Equal(6,source.DiscoveredIds.Count);Check.Equal(1,source.CompletedActions.Count);Check.Equal(1,source.SceneActiveMilliseconds.Count);
        }));
        tests.Add(("StreetSave","V4BackupBlocksV3Main",()=>
        {
            var repositories=Repositories();
            Check.True(repositories.Current.Save(StreetFixtures.V4()).Success);
            Check.True(repositories.Current.Save(StreetFixtures.V4() with{PlayerPosition=new Position2(400,460)}).Success);
            File.Delete(Path.Combine(repositories.CurrentDir,"save.json"));
            Check.True(repositories.V3.Save(CommunityFixtures.V3()).Success);
            var type=typeof(WorldSnapshot).Assembly.GetType("GeXingzhou.Domain.SaveV4UpgradePolicy");Check.True(type!=null);
            var method=type!.GetMethod("Probe",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);Check.True(method!=null);
            var result=(VersionedResumeOffer)method!.Invoke(null,new object[]{repositories.Current,repositories.V3,repositories.V2,repositories.V1,false})!;
            Check.Equal(VersionedResumeSource.Blocked,result.Source);Check.Equal(LoadStatus.NotFound,result.Result.Status);
        }));
        tests.Add(("StreetSave","V3BackupBlocksV2Main",()=>
        {
            var r=Repositories();Check.True(r.V3.Save(CommunityFixtures.V3()).Success);Check.True(r.V3.Save(CommunityFixtures.V3() with{PlayerPosition=new Position2(500,460)}).Success);
            var v3Directory=Path.GetFullPath(Path.Combine(r.CurrentDir,"..","v3"));File.Delete(Path.Combine(v3Directory,"save.json"));
            Check.True(r.V2.Save(SaveV2Codec.CreateNew(new())).Success);
            var main=SaveV4UpgradePolicy.Probe(r.Current,r.V3,r.V2,r.V1);
            Check.Equal(VersionedResumeSource.Blocked,main.Source);Check.Equal(LoadStatus.NotFound,main.Result.Status);
            var backup=SaveV4UpgradePolicy.Probe(r.Current,r.V3,r.V2,r.V1,true);Check.Equal(VersionedResumeSource.V3,backup.Source);
        }));
        tests.Add(("StreetSave","V2PrecedesLegacy",()=>
        {
            var r=Repositories();Check.True(r.V1.Save(new()).Success);Check.True(r.V2.Save(SaveV2Codec.CreateNew(new())).Success);
            Check.Equal(VersionedResumeSource.V2,SaveV4UpgradePolicy.Probe(r.Current,r.V3,r.V2,r.V1).Source);
            var legacy=Repositories();Check.True(legacy.V1.Save(new()).Success);
            Check.Equal(VersionedResumeSource.Legacy,SaveV4UpgradePolicy.Probe(legacy.Current,legacy.V3,legacy.V2,legacy.V1).Source);
        }));
        tests.Add(("StreetSave","ExactManualAndAutoSlots",()=>
        {
            var automatic=Repositories();Check.True(automatic.V1.Save(new()).Success);
            var root=Directory.GetParent(automatic.CurrentDir)!.FullName;
            var manualCurrent=new SaveRepository(Path.Combine(root,"v4"),"manual",new SaveV4Codec(StreetFixtures.Current()));
            var manualV3=new SaveRepository(Path.Combine(root,"v3"),"manual",new SaveV3Codec(StreetFixtures.FrozenV3()));
            var manualV2=new SaveRepository(Path.Combine(root,"v2"),"manual",new SaveV2Codec(CommunityFixtures.FrozenV2()));
            var manualV1=new SaveRepository(Path.Combine(root,"v1"),"manual");
            Check.True(manualCurrent.Save(StreetFixtures.V4()).Success);
            Check.Equal(VersionedResumeSource.Legacy,SaveV4UpgradePolicy.Probe(automatic.Current,automatic.V3,automatic.V2,automatic.V1).Source);
            Check.Equal(VersionedResumeSource.Current,SaveV4UpgradePolicy.Probe(manualCurrent,manualV3,manualV2,manualV1).Source);
            Check.Equal(VersionedResumeSource.Blocked,SaveV4UpgradePolicy.Probe(manualCurrent,manualV3,manualV2,manualV1,true).Source);
        }));
        tests.Add(("StreetSave","BadCurrentBlocksOld",()=>
        {
            foreach(var payload in new[]{"broken",StreetFixtures.Json(StreetFixtures.V4() with{SchemaVersion=9})})
            {
                var r=Repositories();File.WriteAllText(Path.Combine(r.CurrentDir,"save.json"),payload);Check.True(r.V3.Save(CommunityFixtures.V3()).Success);
                var result=SaveV4UpgradePolicy.Probe(r.Current,r.V3,r.V2,r.V1);Check.Equal(VersionedResumeSource.Blocked,result.Source);
                Check.True(result.Result.Status is LoadStatus.Corrupt or LoadStatus.UnsupportedVersion);
            }
        }));
        tests.Add(("StreetSave","SourceHashesUnchanged",()=>
        {
            var r=Repositories();Check.True(r.V3.Save(CommunityFixtures.V3()).Success);Check.True(r.V3.Save(CommunityFixtures.V3() with{PlayerPosition=new Position2(600,460)}).Success);
            Check.True(r.V2.Save(SaveV2Codec.CreateNew(new())).Success);Check.True(r.V1.Save(new()).Success);
            var root=Directory.GetParent(r.CurrentDir)!.FullName;var files=Directory.GetFiles(Path.Combine(root,"v3")).Concat(Directory.GetFiles(Path.Combine(root,"v2"))).Concat(Directory.GetFiles(Path.Combine(root,"v1"))).Append("content/vs01/compat/navigation-v3.json").Append("content/vs01/compat/navigation-v2.json").ToArray();
            var before=files.Select(Hash).ToArray();Check.Equal("F918EE94AC3A74320FF2EEA567AC86E3526E905E7B581E5EBD84CE19F4F5A18F",before[^2]);
            var offer=SaveV4UpgradePolicy.Probe(r.Current,r.V3,r.V2,r.V1);Check.Equal(VersionedResumeSource.V3,offer.Source);
            Check.True(r.Current.Save(Upgrade(offer.Result.Snapshot!).Snapshot!).Success);
            for(int i=0;i<files.Length;i++)Check.Equal(before[i],Hash(files[i]));
        }));
    }
    private static LoadResult Upgrade(WorldSnapshot source)=>SaveV4Adapter.Upgrade(source,CommunityFixtures.FrozenV2(),StreetFixtures.FrozenV3(),StreetFixtures.Current());
    private static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private sealed record Repos(string CurrentDir,SaveRepository Current,SaveRepository V3,SaveRepository V2,SaveRepository V1);
    private static Repos Repositories(string slot="auto")
    {
        var root=StreetFixtures.DirectoryFor("slots");var dirs=new[]{"v4","v3","v2","v1"}.Select(v=>Path.Combine(root,v)).ToArray();foreach(var dir in dirs)Directory.CreateDirectory(dir);
        return new(dirs[0],new(dirs[0],slot,new SaveV4Codec(StreetFixtures.Current())),new(dirs[1],slot,new SaveV3Codec(StreetFixtures.FrozenV3())),new(dirs[2],slot,new SaveV2Codec(CommunityFixtures.FrozenV2())),new(dirs[3],slot));
    }
}
