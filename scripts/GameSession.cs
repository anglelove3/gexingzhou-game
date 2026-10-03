using Godot;
using GeXingzhou.Domain;
using System.Text.Json;
public partial class GameSession : Node
{
    public WorldSnapshot Snapshot {get;private set;} = new();
    public ContentCatalog? Catalog {get;private set;}
    public string ContentError {get;private set;} = "";
    public FlowState Flow {get;set;} = FlowState.Field;
    public void UpdatePosition(Position2 position) => Snapshot=Snapshot with {PlayerPosition=position};
    public void UpdateScene(string sceneId,Position2 position)=>Snapshot=Snapshot with {SceneId=sceneId,PlayerPosition=position};
    public void BeginMemory(SceneReturnContext context,bool replay)
    {
        var m=MemorySession.Begin(Snapshot,context,replay);Snapshot=Snapshot with {MemoryState=m,ReturnContext=context,MemoryOrdinal=int.Parse(m.InstanceId.Split('-')[1])};
        if(Snapshot.Stage==SliceStage.SoupMeet)TryDispatch(new("memory.soup.enter","enter",m.InstanceId));
    }
    public override void _Ready()
    {
        var result=ContentCatalog.LoadText(name => Godot.FileAccess.GetFileAsString("res://content/vs01/"+name));
        Catalog=result.Catalog; ContentError=string.Join("\n",result.Errors);
        if(Catalog!=null)try{Catalog.Dialogues=JsonSerializer.Deserialize<Dictionary<string,DialogueNode>>(Godot.FileAccess.GetFileAsString("res://content/vs01/dialogues.json"))??new();}catch(JsonException ex){ContentError=ex.Message;Catalog=null;}
    }
    public void NewGame(){Snapshot=new();Flow=FlowState.Field;}
    public void AdvanceClock(double delta)
    {
        if(Catalog==null||Snapshot.Stage>=SliceStage.CandyHeyPending)return;
        var p=Catalog.Parameters;var inv=InvitationClock.Advance(Snapshot.InvitationState,delta,Flow==FlowState.Field,p["invitation.voice_delay"],p["invitation.call_delay"],p["invitation.car_fallback"]);
        Snapshot=Snapshot with {InvitationState=inv,Stage=Snapshot.Stage==SliceStage.FreeArrival&&inv.VoiceReceived?SliceStage.InvitationPending:Snapshot.Stage};
    }
    public TransitionResult TryDispatch(StoryAction action){var r=QuestReducer.Apply(Snapshot,action);if(r.Applied)Snapshot=r.Next;return r;}
    public static string TaskText(WorldSnapshot s)=>s.Stage switch{SliceStage.FreeArrival=>"走走，看看搬迁后的故乡",SliceStage.InvitationPending or SliceStage.InvitationResolved=>s.InvitationState.CarArrived?"张大炮到了，小区门口见":"等待张大炮，可以按Tab看手机",SliceStage.CandyHeyPending=>"去便利店街，把喜糖交给Hey哥",SliceStage.CandyHeyDelivered=>"去汤店找张大炮",SliceStage.SoupMeet=>"坐下，听张大炮聊聊近况",SliceStage.MemoryActive=>"推过硬币，决定如何接过那碗汤；Esc可暂时离开",SliceStage.MemoryReturned=>"听听明天的伴郎安排",SliceStage.SliceComplete=>"首段结束 · 明天见（完整版尚未制作）",_=>"首段故事尚在开发"};
}
