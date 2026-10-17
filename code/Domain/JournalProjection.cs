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
    // Directional help is derived from the current scene, never persisted as a new quest.
    public static string RouteHint(WorldSnapshot s)=>s.Stage switch
    {
        SliceStage.FreeArrival=>"靠近物件按 E 看看。Esc 菜单里可以随时查看操作。",
        SliceStage.InvitationPending or SliceStage.InvitationResolved=>s.InvitationState.CarArrived?"张大炮在小区门口。走近他，按 E 聊两句。":"来电时按 Tab 打开手机，再按 Enter 接听。",
        SliceStage.CandyHeyPending=>s.SceneId switch{
            "community_gate"=>"沿小区主路往右，走近便利店街路口按 E。",
            "convenience_street"=>"Hey哥在主街车棚旁。走近他，按 E 递喜糖。",
            _=>"从左侧门口回便利店街，找 Hey 哥递喜糖。"},
        SliceStage.CandyHeyDelivered or SliceStage.SoupMeet=>ToSoupHint(s.SceneId),
        SliceStage.MemoryActive=>s.SceneId=="memory_soup_table"?"推完四枚硬币，再选择怎样接过汤；Esc 可暂时离开。":s.SceneId=="soup_shop"?"走到桌边按 E，选择“想起那碗汤”，接着回忆。":ToSoupHint(s.SceneId),
        SliceStage.MemoryReturned=>s.SceneId=="soup_shop"?"在桌边按 E，听张大炮把话说完。":ToSoupHint(s.SceneId),
        SliceStage.SliceComplete=>"故事暂告一段落。还可以走走，或从暂停菜单保存退出。",
        _=>""
    };
    private static string ToSoupHint(string scene)=>scene switch{
        "community_gate"=>"沿主路往右进入便利店街，再往右走到汤店。",
        "convenience_street"=>"沿主街往右走，到尽头按 E 进入汤店。",
        "soup_shop"=>"走到张大炮对面的椅子旁，按 E 坐下。",
        _=>""};
    private static readonly (string Fact,string Id,string Title)[] HistoryFacts={
        ("invitation.meeting_complete:invitation-1","meeting","在小区门口见到了张大炮"),
        ("candy.hey.delivered:hey-1","hey","把喜糖交给了Hey哥"),
        ("soup.meet:soup-seat-1","soup","和张大炮坐下来聊近况"),
        ("memory.return:soup-1","memory","想起了那碗汤"),
        ("soup.payment:soup-payment-1","payment","张大炮结了这顿的账"),
        ("slice.complete:slice-1","slice","听过明天的安排，今天先到这里")};
    private static readonly (string Id,string Title)[] KnownDiscoveries={
        ("community.sign","旧路牌"),("community.notice","小区公告"),("community.planter","旧花箱"),
        ("street.doorplate","掉漆的旧门牌"),("street.newsstand","关门的报刊亭"),("street.bike_shed","遮雨自行车棚"),
        ("street.drain","落叶排水沟"),("street.wall_paint","墙面多层旧漆"),("street.laundry_rack","晾晒架与衣夹"),("street.stool","店外塑料凳"),
        ("soup.sign","旧招牌"),("soup.menu","看看菜单"),("soup.note","柜台便条")};
    public static JournalView Build(WorldSnapshot snapshot)
    {
        var history=new List<JournalEntry>();
        foreach(var (fact,id,title) in HistoryFacts)
            if(snapshot.CompletedActions.Contains(fact)&&(id!="memory"||snapshot.CompletedActions.Contains("memory.food.resolve:soup-1")))
                history.Add(new(id,title,HistoryContext(id)));
        var discoveries=new List<JournalEntry>();
        foreach(var (id,title) in KnownDiscoveries)
            if(snapshot.DiscoveredIds.Contains(id))discoveries.Add(new(id,title,DiscoveryContext(id)));
        return new(CurrentGoal(snapshot),history.AsReadOnly(),discoveries.AsReadOnly());
    }
    private static string DiscoveryContext(string id)
    {
        var place=id.StartsWith("community.",StringComparison.Ordinal)?"安置小区":id.StartsWith("street.",StringComparison.Ordinal)?"便利店街":"鸭血粉丝汤店";
        var memory=id switch{
            "community.sign"=>"路是新的，牌还是旧的。认路也得重新学一遍。",
            "community.notice"=>"通道要留空。门口那点地方，也有人替你操心。",
            "community.planter"=>"花箱旧了，花倒还长得精神。",
            "street.doorplate"=>"新刷的门牌，边角还露着旧颜色。",
            "street.newsstand"=>"卷帘门落着，那片阴影还像有人会停下来翻报。",
            "street.bike_shed"=>"雨布补了又补，车把依旧挤在一起。",
            "street.drain"=>"风把落叶吹散一点，过一会儿又堆回沟边。",
            "street.wall_paint"=>"新漆盖住旧漆，旧颜色又从裂缝里露出来。",
            "street.laundry_rack"=>"只剩几只衣夹，风一碰，就轻轻响。",
            "street.stool"=>"凳面磨得发亮。很多人在这里坐过一会儿。",
            "soup.sign"=>"店换了地方，招牌还是那块，边角留着油烟。",
            "soup.menu"=>"菜单上还是鸭血粉丝汤，够吃顿热乎的。",
            "soup.note"=>"柜台便条上写着：先坐，汤马上好。",
            _=>""
        };
        return "在"+place+"看过\n"+memory;
    }
    private static string HistoryContext(string id)=>id switch
    {
        "meeting"=>"在小区门口碰了面。手机里的声音，这回站到了眼前。",
        "hey"=>"喜糖递出去了。街上还有路，不必把每一步都走成赶路。",
        "soup"=>"你们坐在同一张桌边。热气升起来，有些话可以慢一点说。",
        "memory"=>"那碗汤的旧事想过了，眼前的人仍坐在对面。想起，不等于必须想明白。",
        "payment"=>"这顿张大炮结了账。先把他的心意收下，别急着算回来。",
        "slice"=>"听过明天的安排。还有没说清的话，也还有一次见面的约定。",
        _=>""
    };
    // Read-only presentation: stages/live replay choices are not evidence of a completed experience.
    public static string EndReflection(WorldSnapshot snapshot)
    {
        var view=Build(snapshot);var lines=new List<string>{snapshot.CompletedActions.Contains("slice.complete:slice-1")?"你答应了明天的见面。\n有些话还没说完，好在不必今天说完。":"今天先到这里。还有没想清楚的事，可以留到明天。"};
        if(view.History.Count>0){lines.Add("\n今天留下的几笔");lines.AddRange(view.History.Select(x=>"· "+x.Title));}
        var seen=view.Discoveries.Take(2).Select(x=>x.Title).ToArray();
        if(seen.Length>0)lines.Add("路上，你留意过"+string.Join("、",seen)+"。那些小地方，也留在了今天。");
        return string.Join("\n",lines);
    }
}
