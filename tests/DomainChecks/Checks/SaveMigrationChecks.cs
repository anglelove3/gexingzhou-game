using GeXingzhou.Domain;
using System.Security.Cryptography;
namespace GeXingzhou.DomainChecks;
public static class SaveMigrationChecks
{
    public static void Register(List<(string,string,Action)> tests)
    {
        tests.Add(("SaveMigration","UpgradeCopiesWithoutAliases",()=>{
            var old=SaveMigrationFixtures.LegacyMemory(null,false);Check.Equal<string?>(null,SaveRepository.Validate(old));
            var result=LegacySaveAdapter.Upgrade(old,SaveMigrationFixtures.Navigation());Check.Equal(LoadStatus.Loaded,result.Status);
            var s=result.Snapshot!;Check.Equal(2,s.SchemaVersion);Check.Equal("vs01-0.2",s.ContentVersion);Check.Equal(old.PlaythroughId,s.PlaythroughId);
            Check.Equal(old.MemoryState!.InstanceId,s.MemoryState!.InstanceId);Check.Equal(new Position2(400,430),s.ReturnContext!.Position);
            Check.Equal(old.PlayerPosition,s.PlayerPosition);Check.Equal(0,s.DiscoveredIds.Count);Check.Equal(old.Settings,s.Settings);Check.Equal(old.InvitationState,s.InvitationState);
            Check.True(!ReferenceEquals(old.Settings,s.Settings)&&!ReferenceEquals(old.InvitationState,s.InvitationState));
            s.CompletedActions.Clear();s.ChoiceCodes.Clear();s.MemoryState.PushedCoinIds.Clear();s.SceneActiveMilliseconds.Clear();
            Check.True(old.CompletedActions.Count>0&&old.ChoiceCodes.Count>0&&old.MemoryState.PushedCoinIds.Count>0&&old.SceneActiveMilliseconds.Count>0);
            Check.Equal(new Position2(440,280),old.ReturnContext!.Position);
        }));
        tests.Add(("SaveMigration","AllFoodAndReplay",()=>{
            foreach(var replay in new[]{false,true})foreach(FoodChoice? food in new FoodChoice?[]{null,FoodChoice.Take,FoodChoice.Wait,FoodChoice.Share}){
                var old=SaveMigrationFixtures.LegacyMemory(food,replay);Check.Equal<string?>(null,SaveRepository.Validate(old));
                var result=LegacySaveAdapter.Upgrade(old,SaveMigrationFixtures.Navigation());Check.Equal(LoadStatus.Loaded,result.Status);
                var s=result.Snapshot!;Check.Equal(old.MemoryState!.FoodChoice,s.MemoryState!.FoodChoice);Check.Equal(old.MemoryState.Replay,s.MemoryState.Replay);
                Check.Equal(old.Stage,s.Stage);Check.Equal(old.MemoryOrdinal,s.MemoryOrdinal);Check.Equal(old.MemoryVisitOrdinal,s.MemoryVisitOrdinal);
                Check.True(old.CompletedActions.SetEquals(s.CompletedActions));Check.Equal(old.ChoiceCodes.Count,s.ChoiceCodes.Count);Check.Equal<string?>(null,SaveMigrationFixtures.Codec().Validate(s));
            }
            var community=new WorldSnapshot{PlayerPosition=new(500,280)};Check.Equal(community.PlayerPosition,LegacySaveAdapter.Upgrade(community,SaveMigrationFixtures.Navigation()).Snapshot!.PlayerPosition);
            var soup=new WorldSnapshot{SceneId="soup_shop",PlayerPosition=new(600,280)};Check.Equal(new Position2(120,480),LegacySaveAdapter.Upgrade(soup,SaveMigrationFixtures.Navigation()).Snapshot!.PlayerPosition);
        }));
        tests.Add(("SaveMigration","DepthRoundTrip",()=>{
            var dir=SaveMigrationFixtures.DirectoryFor("round-trip");var repo=new SaveRepository(dir,"manual",SaveMigrationFixtures.Codec());
            var s=SaveMigrationFixtures.Depth() with {DiscoveredIds=new(){"soup.sign","soup.note"}};
            Check.True(repo.Save(s).Success);Check.Equal(s.PlayerPosition,repo.Load().Snapshot!.PlayerPosition);Check.True(s.DiscoveredIds.SetEquals(repo.Load().Snapshot!.DiscoveredIds));
            Check.True(repo.Save(s with {PlayerPosition=new(400,480)}).Success);Check.Equal(s.PlayerPosition,repo.LoadBackup().Snapshot!.PlayerPosition);
            Check.Equal(LoadStatus.NotFound,new SaveRepository(dir,"auto",SaveMigrationFixtures.Codec()).Load().Status);
            Check.Equal(LoadStatus.UnsupportedVersion,new SaveRepository(dir,"manual").Load().Status);
        }));
        tests.Add(("SaveMigration","FiniteBlockedVsInvalid",()=>{
            var codec=SaveMigrationFixtures.Codec();var blocked=SaveMigrationFixtures.Depth() with {PlayerPosition=new(500,430)};
            Check.True(codec.Validate(blocked)!=null);var read=codec.Read(SaveMigrationFixtures.Json(blocked));Check.Equal(LoadStatus.Loaded,read.Status);Check.Equal(new Position2(120,480),read.Snapshot!.PlayerPosition);Check.True(read.Message.Contains("安全"));
            foreach(var pos in new[]{new Position2(24,430),new Position2(400,200),new Position2(970,430),new Position2(float.NaN,430),new Position2(400,float.PositiveInfinity)})Check.True(codec.Validate(blocked with {PlayerPosition=pos})!=null);
            Check.Equal(LoadStatus.Corrupt,codec.Read(SaveMigrationFixtures.Json(blocked with {PlayerPosition=new(400,200)})).Status);
            var memory=LegacySaveAdapter.Upgrade(SaveMigrationFixtures.LegacyMemory(null,false),SaveMigrationFixtures.Navigation()).Snapshot!;
            var recovery=codec.Read(SaveMigrationFixtures.Json(memory with {ReturnContext=memory.ReturnContext! with {Position=new(500,430)}}));
            Check.Equal(LoadStatus.Loaded,recovery.Status);Check.Equal(new Position2(120,480),recovery.Snapshot!.ReturnContext!.Position);
            Check.Equal(LoadStatus.Corrupt,codec.Read(SaveMigrationFixtures.Json(memory with {ReturnContext=memory.ReturnContext! with {Position=new(400,200)}})).Status);
        }));
        tests.Add(("SaveMigration","UnknownIdsAndVersions",()=>{
            var codec=SaveMigrationFixtures.Codec();var good=SaveMigrationFixtures.Depth();
            foreach(var bad in new[]{good with {DiscoveredIds=new(){"soup.fake"}},good with {DiscoveredIds=null!},good with {CompletedActions=new(){"soup.meet:fake"}},
                good with {CompletedActions=new(){"memory.coin.push:soup-1:c5"}},good with {ChoiceCodes=new(){["soup-response-1"]="hacked"}},good with {ChoiceCodes=new(){["hey-99"]="delivered"}},
                good with {SceneActiveMilliseconds=new(){["soup_shop"]=-1}},good with {Stage=(SliceStage)999},good with {PhoneState="hacked"}})Check.True(codec.Validate(bad)!=null);
            foreach(var bad in new[]{good with {SchemaVersion=9},good with {ContentVersion="future"}})Check.Equal(LoadStatus.UnsupportedVersion,codec.Read(SaveMigrationFixtures.Json(bad)).Status);
            Check.Equal(LoadStatus.Corrupt,codec.Read("{\"schema_version\":2,\"content_version\":\"vs01-0.2\",\"scene_id\":\"soup_shop\",\"stage\":999}").Status);
            var legacy=new WorldSnapshot{CompletedActions=new(){"soup.meet:fake"}};Check.Equal(LoadStatus.Corrupt,LegacySaveAdapter.Upgrade(legacy,SaveMigrationFixtures.Navigation()).Status);
            Check.True(SaveRepository.Validate(new(){DiscoveredIds=new(){"soup.note"}})!=null);
        }));
        tests.Add(("SaveMigration","SlotAndBackupPrecedence",()=>{
            var oldDir=SaveMigrationFixtures.DirectoryFor("old-slots");var newDir=SaveMigrationFixtures.DirectoryFor("new-slots");
            var old=new SaveRepository(oldDir);var current=new SaveRepository(newDir,"auto",SaveMigrationFixtures.Codec());
            Check.True(old.Save(new()).Success);Check.True(old.Save(new(){PlayerPosition=new(400,280)}).Success);
            Check.Equal(ResumeSource.Legacy,SaveUpgradePolicy.Probe(current,old).Source);
            File.WriteAllText(Path.Combine(newDir,"save.bak.json"),SaveMigrationFixtures.Json(SaveMigrationFixtures.Depth()));
            Check.True(!current.Save(SaveMigrationFixtures.Depth()).Success);
            Check.True(!File.Exists(Path.Combine(newDir,"save.json")));
            Check.Equal(ResumeSource.Blocked,SaveUpgradePolicy.Probe(current,old).Source);Check.Equal(LoadStatus.NotFound,SaveUpgradePolicy.Probe(current,old).Result.Status);
            Check.Equal(ResumeSource.V2,SaveUpgradePolicy.Probe(current,old,true).Source);
            File.WriteAllText(Path.Combine(newDir,"save.json"),"{bad");Check.Equal(ResumeSource.Blocked,SaveUpgradePolicy.Probe(current,old).Source);
            var manual=new SaveRepository(newDir,"manual",SaveMigrationFixtures.Codec());var oldManual=new SaveRepository(oldDir,"manual");Check.Equal(ResumeSource.Missing,SaveUpgradePolicy.Probe(manual,oldManual).Source);
            Check.True(oldManual.Save(new()).Success);Check.Equal(ResumeSource.Legacy,SaveUpgradePolicy.Probe(manual,oldManual).Source);
            File.WriteAllText(Path.Combine(newDir,"manual.json"),"{\"schema_version\":99,\"content_version\":\"future\"}");Check.Equal(ResumeSource.Blocked,SaveUpgradePolicy.Probe(manual,oldManual).Source);
        }));
        tests.Add(("SaveMigration","SourcesUnchanged",()=>{
            var oldDir=SaveMigrationFixtures.DirectoryFor("source-hashes");var newDir=SaveMigrationFixtures.DirectoryFor("upgraded");
            foreach(var slot in new[]{"auto","manual"}){
                var old=new SaveRepository(oldDir,slot);Check.True(old.Save(SaveMigrationFixtures.LegacyMemory(null,false)).Success);Check.True(old.Save(SaveMigrationFixtures.LegacyMemory(FoodChoice.Share,false)).Success);
            }
            var hashes=Directory.GetFiles(oldDir).ToDictionary(p=>p,p=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
            foreach(var slot in new[]{"auto","manual"}){
                var old=new SaveRepository(oldDir,slot);var current=new SaveRepository(newDir,slot,SaveMigrationFixtures.Codec());
                var offer=SaveUpgradePolicy.Probe(current,old);var upgraded=LegacySaveAdapter.Upgrade(offer.Result.Snapshot!,SaveMigrationFixtures.Navigation());Check.True(current.Save(upgraded.Snapshot!).Success);
                Check.Equal(ResumeSource.V2,SaveUpgradePolicy.Probe(current,old).Source);
            }
            foreach(var pair in hashes)Check.Equal(pair.Value,Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(pair.Key))));Check.Equal(4,Directory.GetFiles(oldDir).Length);
        }));
        tests.Add(("SaveMigration","WriteFailureAndNewGameProtection",()=>{
            var dir=SaveMigrationFixtures.DirectoryFor("protection");var repo=new SaveRepository(dir,"auto",SaveMigrationFixtures.Codec());var good=SaveMigrationFixtures.Depth();
            Check.True(repo.Save(good).Success);Check.True(repo.Save(good with {PlayerPosition=new(400,480)}).Success);
            var primary=Path.Combine(dir,"save.json");var backup=Path.Combine(dir,"save.bak.json");var backupHash=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(backup)));
            File.WriteAllText(primary,"{bad");Check.True(!repo.Save(good).Success);Check.Equal(backupHash,Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(backup))));
            Check.True(repo.PreserveForNewGame().Success);Check.True(repo.Save(good).Success);Check.True(Directory.GetFiles(dir,"preserved-*.json").Any(p=>File.ReadAllText(p)=="{bad"));
            Check.True(Directory.GetFiles(dir,"preserved-*.json").Any(p=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p)))==backupHash));
            using(var locked=new FileStream(primary,FileMode.Open,FileAccess.ReadWrite,FileShare.None)){Check.Equal(LoadStatus.IoError,repo.Load().Status);Check.True(!repo.Save(good).Success);}
            var file=Path.Combine(dir,"file-not-directory");File.WriteAllText(file,"keep");Check.True(!new SaveRepository(file,"auto",SaveMigrationFixtures.Codec()).Save(good).Success);Check.Equal("keep",File.ReadAllText(file));
            var blocked=new SaveRepository(file,"auto",SaveMigrationFixtures.Codec());Check.Equal(LoadStatus.IoError,blocked.Load().Status);
            Check.Equal(ResumeSource.Blocked,SaveUpgradePolicy.Probe(blocked,new SaveRepository(SaveMigrationFixtures.DirectoryFor("legacy-fallback"))).Source);
            var directorySlot=SaveMigrationFixtures.DirectoryFor("directory-slot");Directory.CreateDirectory(Path.Combine(directorySlot,"save.json"));
            Check.True(new SaveRepository(directorySlot,"auto",SaveMigrationFixtures.Codec()).Save(good).Message.Contains("未能"));
        }));
    }
}
