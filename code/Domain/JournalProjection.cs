namespace GeXingzhou.Domain;
public sealed record JournalEntry(string Id,string Title,string Body);
public sealed record JournalView(string CurrentGoal,IReadOnlyList<JournalEntry> History,IReadOnlyList<JournalEntry> Discoveries);
public static class JournalProjection
{
    public static string CurrentGoal(WorldSnapshot s)=>s.Stage switch
    {
        SliceStage.FreeArrival=>"走走，看看搬迁后的故乡",
        SliceStage.InvitationPending or SliceStage.InvitationResolved=>s.InvitationState.CarArrived?"张大炮到了，小区门口见":"等待张大炮，可以按Tab看手机",
        SliceStage.CandyHeyPending=>"去便利店街，把喜糖交给Hey哥",
        SliceStage.CandyHeyDelivered=>"去汤店找张大炮",
        SliceStage.SoupMeet=>"坐下，听张大炮聊聊近况",
        SliceStage.MemoryActive=>"推过硬币，决定如何接过那碗汤；Esc可暂时离开",
        SliceStage.MemoryReturned=>"听听明天的伴郎安排",
        SliceStage.SliceComplete=>"今天先到这里 · 明天见",
        _=>"首段故事尚在开发"
    };
    private static readonly (string Fact,string Id,string Title)[] HistoryFacts={
        ("invitation.meeting_complete:invitation-1","meeting","在小区门口见到了张大炮"),
        ("candy.hey.delivered:hey-1","hey","把喜糖交给了Hey哥"),
        ("soup.meet:soup-seat-1","soup","和张大炮坐下来聊近况"),
        ("memory.return:soup-1","memory","想起了那碗汤"),
        ("soup.payment:soup-payment-1","payment","张大炮结了这顿的账"),
        ("slice.complete:slice-1","slice","听过明天的安排，今天先到这里")};
    private static readonly (string Id,string Title)[] KnownDiscoveries={
        ("community.sign","旧路牌"),("community.notice","小区公告"),("community.planter","旧花箱"),
        ("soup.sign","旧招牌"),("soup.menu","看看菜单"),("soup.note","柜台便条")};
    public static JournalView Build(WorldSnapshot snapshot)
    {
        var history=new List<JournalEntry>();
        foreach(var (fact,id,title) in HistoryFacts)
            if(snapshot.CompletedActions.Contains(fact)&&(id!="memory"||snapshot.CompletedActions.Contains("memory.food.resolve:soup-1")))
                history.Add(new(id,title,""));
        var discoveries=new List<JournalEntry>();
        foreach(var (id,title) in KnownDiscoveries)
            if(snapshot.DiscoveredIds.Contains(id))discoveries.Add(new(id,title,id.StartsWith("community.",StringComparison.Ordinal)?"在安置小区看过，可回到场景再次观察":"在鸭血粉丝汤店看过，可回到场景再次观察"));
        return new(CurrentGoal(snapshot),history.AsReadOnly(),discoveries.AsReadOnly());
    }
}
