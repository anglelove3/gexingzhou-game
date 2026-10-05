using Godot;
using GeXingzhou.Domain;
using System.Text.Json;
public sealed record RestoreResult(bool Success,string? ErrorCode=null);
public partial class GameSession : Node
{
    public WorldSnapshot Snapshot {get;private set;} = new();
    public ContentCatalog? Catalog {get;private set;}
    public string ContentError {get;private set;} = "";
    public string SaveMessage {get;private set;}="";
    public string SaveDirectory {get;private set;}="";
    public SaveRepository Saves {get;private set;}=null!;
    public SaveRepository ManualSaves {get;private set;}=null!;
    public WorldSnapshot? PendingRestore {get;set;}
    public GameSettings Options {get;private set;}=new();
    public SettingsRepository Preferences {get;private set;}=null!;
    public LocalEventRecorder Recorder {get;private set;}=null!;
    public string EventWarning {get;private set;}="";
    public string FontWarning {get;private set;}="";
    public FlowState Flow {get;set;} = FlowState.Field;
    private readonly Dictionary<string,double> activeSeconds=new();
    public void UpdatePosition(Position2 position) => Snapshot=Snapshot with {PlayerPosition=position};
    public void UpdateScene(string sceneId,Position2 position)=>Snapshot=Snapshot with {SceneId=sceneId,PlayerPosition=position};
    public void BeginMemory(SceneReturnContext context,bool replay)
    {
        var m=MemorySession.Begin(Snapshot,context,replay);Snapshot=Snapshot with {MemoryState=m,ReturnContext=context,MemoryOrdinal=int.Parse(m.InstanceId.Split('-')[1]),MemoryVisitOrdinal=Snapshot.MemoryVisitOrdinal+1};
        Record("soup.memory.enter","entered",m.InstanceId+":visit-"+Snapshot.MemoryVisitOrdinal);
        if(Snapshot.Stage==SliceStage.SoupMeet)TryDispatch(new("memory.soup.enter","enter",m.InstanceId));
    }
    public override void _Ready()
    {
        var result=ContentCatalog.LoadText(name => Godot.FileAccess.GetFileAsString("res://content/vs01/"+name));
        Catalog=result.Catalog; ContentError=string.Join("\n",result.Errors);
        var args=OS.GetCmdlineUserArgs();var supplied=args.FirstOrDefault(a=>a.StartsWith("--test-save-root="))?.Split('=',2)[1];
        var location=args.Any(a=>a.StartsWith("--suite="))?"res://test-output/integration/"+Guid.NewGuid():"user://saves/vs01";
        if(supplied!=null&&supplied.StartsWith("res://test-output/")&&!supplied.Contains(".."))location=supplied;
        SaveDirectory=ProjectSettings.GlobalizePath(location);Saves=new(SaveDirectory);ManualSaves=new(SaveDirectory,"manual");
        Preferences=new(System.IO.Path.Combine(SaveDirectory,"preferences"));Options=Preferences.Load();Snapshot=Snapshot with {Settings=Options};Recorder=new(System.IO.Path.Combine(SaveDirectory,"behavior"),Options.RecordEventsEnabled);
    }
    public void NewGame(){Snapshot=new(){Settings=Options};activeSeconds.Clear();PendingRestore=null;Flow=FlowState.Field;SaveMessage="";}
    public RestoreResult Restore(WorldSnapshot snapshot)
    {
        var error=SaveRepository.Validate(snapshot);if(error!=null)return new(false,error);
        Snapshot=ResumePolicy.Normalize(snapshot) with {Settings=Options};activeSeconds.Clear();foreach(var time in Snapshot.SceneActiveMilliseconds)activeSeconds[time.Key]=time.Value/1000.0;
        Flow=Snapshot.SceneId=="memory_soup_table"?FlowState.Memory:FlowState.Field;PendingRestore=null;return new(true);
    }
    public void SetOptions(GameSettings options,bool persist=true){Options=options.Normalize();Snapshot=Snapshot with {Settings=Options};Recorder.Enabled=Options.RecordEventsEnabled;if(persist)EventWarning=Preferences.Save(Options).Message;}
    public Theme CreateUiTheme()
    {
        var template=GD.Load<Theme>("res://assets/theme.tres");var font=template.DefaultFont;
        FontWarning=font.HasChar('中')?"":"缺少中文字体，请安装支持中文的系统字体；未改成英文。";
        var theme=(Theme)template.Duplicate();theme.DefaultFontSize=Options.SubtitleSize;return theme;
    }
    public void RecordMemoryReturned(){if(Snapshot.MemoryState is {} m)Record("soup.memory.return","returned",m.InstanceId+":visit-"+Snapshot.MemoryVisitOrdinal);}
    private void Record(string id,string choice,string opportunity)
    {
        var r=Recorder.TryAppend(new(){PlaythroughId=Snapshot.PlaythroughId,SceneId=Snapshot.SceneId,EventId=id,OpportunityId=opportunity,ChoiceCode=choice.ToLowerInvariant(),CheckpointId=JsonNamingPolicy.SnakeCaseLower.ConvertName(Snapshot.Stage.ToString()),ActiveMilliseconds=Snapshot.SceneActiveMilliseconds.GetValueOrDefault(Snapshot.SceneId)});if(!r.Success)EventWarning=r.Message;
    }
    public SaveResult SaveCheckpoint(){var r=Saves.Save(Snapshot);SaveMessage=r.Success?"":r.Message;return r;}
    public SaveResult SaveManual(){var r=ManualSaves.Save(Snapshot);SaveMessage=r.Message;return r;}
    public void MarkFirstCommunityObservation()
    {
        if(Snapshot.CompletedActions.Contains("observation.community.first"))return;
        Snapshot=Snapshot with {CompletedActions=new HashSet<string>(Snapshot.CompletedActions){"observation.community.first"}};
        SaveCheckpoint();
    }
    public void AdvanceClock(double delta)
    {
        if(!double.IsFinite(delta)||delta<0)return;
        if(Flow is FlowState.Field or FlowState.Memory){var scene=Snapshot.SceneId;activeSeconds[scene]=activeSeconds.GetValueOrDefault(scene)+delta;var times=new Dictionary<string,long>(Snapshot.SceneActiveMilliseconds){[scene]=(long)Math.Round(activeSeconds[scene]*1000)};Snapshot=Snapshot with {SceneActiveMilliseconds=times};}
        if(Catalog==null||Snapshot.Stage>=SliceStage.CandyHeyPending)return;
        var p=Catalog.Parameters;var inv=InvitationClock.Advance(Snapshot.InvitationState,delta,Flow==FlowState.Field,p["invitation.voice_delay"],p["invitation.call_delay"],p["invitation.car_fallback"]);
        Snapshot=Snapshot with {InvitationState=inv,Stage=Snapshot.Stage==SliceStage.FreeArrival&&inv.VoiceReceived?SliceStage.InvitationPending:Snapshot.Stage};
    }
    public TransitionResult TryDispatch(StoryAction action)
    {
        var r=QuestReducer.Apply(Snapshot,action);if(r.Applied)
        {
            Snapshot=r.Next;
            if(action.Id is "invitation.answer" or "invitation.meeting_complete")Record("invitation.resolve",Snapshot.InvitationState.Resolution==InvitationResolution.Answered?"answered":"arrived","invitation-1");
            else if(action.Id=="candy.hey.delivered")Record(action.Id,"delivered","hey-1");
            else if(action.Id=="memory.coin.push"&&Snapshot.MemoryState!.PushedTotal==5)Record("memory.coin.complete","completed",Snapshot.MemoryState.InstanceId);
            else if(action.Id=="memory.food.resolve")Record(action.Id,action.ChoiceCode,Snapshot.MemoryState!.InstanceId);
            else if(action.Id=="slice.complete")Record(action.Id,"completed","slice-1");
            if(action.Id is "invitation.meeting_complete" or "candy.hey.delivered" or "soup.meet" or "soup.response" or "memory.soup.enter" or "memory.coin.push" or "memory.food.resolve" or "memory.return" or "soup.payment" or "slice.complete")SaveCheckpoint();
        }return r;
    }
    public static string TaskText(WorldSnapshot s)=>s.Stage switch{SliceStage.FreeArrival=>"走走，看看搬迁后的故乡",SliceStage.InvitationPending or SliceStage.InvitationResolved=>s.InvitationState.CarArrived?"张大炮到了，小区门口见":"等待张大炮，可以按Tab看手机",SliceStage.CandyHeyPending=>"去便利店街，把喜糖交给Hey哥",SliceStage.CandyHeyDelivered=>"去汤店找张大炮",SliceStage.SoupMeet=>"坐下，听张大炮聊聊近况",SliceStage.MemoryActive=>"推过硬币，决定如何接过那碗汤；Esc可暂时离开",SliceStage.MemoryReturned=>"听听明天的伴郎安排",SliceStage.SliceComplete=>"首段结束 · 明天见（完整版尚未制作）",_=>"首段故事尚在开发"};
}
