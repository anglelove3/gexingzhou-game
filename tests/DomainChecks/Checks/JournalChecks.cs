using GeXingzhou.Domain;
using System.Security.Cryptography;
namespace GeXingzhou.DomainChecks;
public static class JournalChecks
{
    public static void Register(List<(string,string,Action)> tests)
    {
        // Break: deriving the current goal from UI state or losing a stage leaves HUD and journal disagreeing.
        tests.Add(("Journal","CurrentGoalAllStages",()=>{
            var goals=new[]{"走走，看看搬迁后的故乡","等待张大炮，可以按Tab看手机","等待张大炮，可以按Tab看手机","去便利店街，把喜糖交给Hey哥","去汤店找张大炮","坐下，听张大炮聊聊近况","推过硬币，决定如何接过那碗汤；Esc可暂时离开","听听明天的伴郎安排","今天先到这里 · 明天见"};
            foreach(var stage in Enum.GetValues<SliceStage>()){
                var s=SaveV2Codec.CreateNew(new()) with {Stage=stage};
                Check.Equal(goals[(int)stage],JournalProjection.CurrentGoal(s));
                Check.Equal(goals[(int)stage],JournalProjection.Build(s).CurrentGoal);
            }
            foreach(var stage in new[]{SliceStage.InvitationPending,SliceStage.InvitationResolved})
                Check.Equal("张大炮到了，小区门口见",JournalProjection.CurrentGoal(new(){Stage=stage,InvitationState=new(){CarArrived=true}}));
        }));
        // Break: treating stage ordering as completed facts reveals events the player never finished.
        tests.Add(("Journal","HistoryFactsOnly",()=>{
            Check.Equal(0,JournalProjection.Build(new(){Stage=SliceStage.SliceComplete}).History.Count);
            var s=new WorldSnapshot{CompletedActions=new(){"slice.complete:slice-1","memory.return:soup-1","soup.payment:soup-payment-1","soup.meet:soup-seat-1","candy.hey.delivered:hey-1","invitation.meeting_complete:invitation-1","memory.food.resolve:soup-1"}};
            var history=JournalProjection.Build(s).History;
            Check.Equal("meeting,hey,soup,memory,payment,slice",string.Join(',',history.Select(e=>e.Id)));
            Check.Equal("在小区门口见到了张大炮|把喜糖交给了Hey哥|和张大炮坐下来聊近况|想起了那碗汤|张大炮结了这顿的账|听过明天的安排，今天先到这里",string.Join('|',history.Select(e=>e.Title)));
            foreach(var key in new[]{"memory.return:soup-1","memory.food.resolve:soup-1"})Check.Equal(0,JournalProjection.Build(new(){CompletedActions=new(){key}}).History.Count);
        }));
        tests.Add(("Journal","AnsweredIsNotMeeting",()=>{
            var pending=new WorldSnapshot{Stage=SliceStage.InvitationPending,InvitationState=new(){PhoneRinging=true,VoiceReceived=true,CarArrived=true}};
            var answered=QuestReducer.Apply(pending,new("invitation.answer","answered","invitation-1")).Next;
            Check.Equal(0,JournalProjection.Build(answered).History.Count);
            foreach(var s in new[]{pending,answered}){
                var met=QuestReducer.Apply(s,new("invitation.meeting_complete","meeting","invitation-1"));
                Check.True(met.Applied);Check.Equal("meeting",JournalProjection.Build(met.Next).History.Single().Id);
            }
        }));
        // Break: inspecting only the current replay instance drops, duplicates, or invents first-memory history.
        tests.Add(("Journal","ReplayKeepsFirstHistory",()=>{
            foreach(var complete in new[]{false,true}){
                var replay=new WorldSnapshot{MemoryState=new(){InstanceId="soup-2",Replay=true,Completed=complete},CompletedActions=new(){"memory.return:soup-1","memory.food.resolve:soup-1","memory.return:soup-2","memory.food.resolve:soup-2"}};
                Check.Equal("memory",JournalProjection.Build(replay).History.Single().Id);
                Check.Equal(0,JournalProjection.Build(replay with {CompletedActions=new(){"memory.return:soup-2","memory.food.resolve:soup-2","memory.return:soup-999"}}).History.Count);
            }
        }));
        tests.Add(("Journal","DiscoveryKnownOrder",()=>{
            Check.Equal(0,JournalProjection.Build(new()).Discoveries.Count);
            var s=new WorldSnapshot{DiscoveredIds=new(){"soup.note","soup.fake","SOUP.SIGN","soup.menu","soup.sign"}};
            var items=JournalProjection.Build(s).Discoveries;
            Check.Equal("soup.sign,soup.menu,soup.note",string.Join(',',items.Select(e=>e.Id)));
            Check.Equal("旧招牌,看看菜单,柜台便条",string.Join(',',items.Select(e=>e.Title)));
            foreach(var item in items)Check.Equal("在鸭血粉丝汤店看过，可回到场景再次观察",item.Body);
            var marked=DiscoveryPolicy.Mark(new(),"soup.note");Check.Equal(1,JournalProjection.Build(DiscoveryPolicy.Mark(marked,"soup.note")).Discoveries.Count);
        }));
        tests.Add(("Journal","DoesNotMutate",()=>{
            var s=new WorldSnapshot{CompletedActions=new(){"soup.meet:soup-seat-1"},DiscoveredIds=new(){"soup.note"},ChoiceCodes=new(){["soup-seat-1"]="sit"}};
            var before=SaveMigrationFixtures.Json(s);var view=JournalProjection.Build(s);
            Check.Equal(before,SaveMigrationFixtures.Json(s));s.CompletedActions.Clear();s.DiscoveredIds.Clear();
            Check.Equal(1,view.History.Count);Check.Equal(1,view.Discoveries.Count);
            Check.Throws<NotSupportedException>(()=>((IList<JournalEntry>)view.History).Clear());
            Check.Throws<NotSupportedException>(()=>((IList<JournalEntry>)view.Discoveries).Clear());
        }));
        // Break: persisting a second journal list would survive reload/upgrade independently of real facts.
        tests.Add(("Journal","SaveAndUpgradeProjection",()=>{
            var dir=SaveMigrationFixtures.DirectoryFor("journal-v2");var repo=new SaveRepository(dir,"manual",SaveMigrationFixtures.Codec());
            var s=SaveMigrationFixtures.Depth() with {DiscoveredIds=new(){"soup.note","soup.sign"}};
            Check.True(repo.Save(s).Success);var loaded=repo.Load();Check.Equal(LoadStatus.Loaded,loaded.Status);
            Check.Equal("soup.sign,soup.note",string.Join(',',JournalProjection.Build(loaded.Snapshot!).Discoveries.Select(e=>e.Id)));
            Check.True(repo.Save(SaveMigrationFixtures.Depth()).Success);Check.Equal(0,JournalProjection.Build(repo.Load().Snapshot!).Discoveries.Count);
            Check.Equal(2,JournalProjection.Build(repo.LoadBackup().Snapshot!).Discoveries.Count);
            Check.True(SaveMigrationFixtures.Codec().Validate(s with {DiscoveredIds=new(){"soup.fake"}})!=null);
            var legacyDir=SaveMigrationFixtures.DirectoryFor("journal-v1");var oldRepo=new SaveRepository(legacyDir);
            Check.True(oldRepo.Save(SaveMigrationFixtures.LegacyMemory(null,false)).Success);
            Check.True(oldRepo.Save(SaveMigrationFixtures.LegacyMemory(FoodChoice.Share,false)).Success);
            var hashes=Directory.GetFiles(legacyDir).ToDictionary(p=>p,p=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
            foreach(var backup in new[]{false,true}){
                var old=(backup?oldRepo.LoadBackup():oldRepo.Load()).Snapshot!;
                var upgraded=LegacySaveAdapter.Upgrade(old,SaveMigrationFixtures.Navigation());Check.Equal(LoadStatus.Loaded,upgraded.Status);
                var view=JournalProjection.Build(upgraded.Snapshot!);Check.Equal("推过硬币，决定如何接过那碗汤；Esc可暂时离开",view.CurrentGoal);
                Check.Equal("soup",string.Join(',',view.History.Select(e=>e.Id)));Check.Equal(0,view.Discoveries.Count);
            }
            foreach(var hash in hashes)Check.Equal(hash.Value,Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(hash.Key))));
        }));
    }
}
