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
    public static string TaskText(WorldSnapshot s)=>s.Stage switch{SliceStage.FreeArrival=>"走走，看看搬迁后的故乡",SliceStage.InvitationPending or SliceStage.InvitationResolved=>s.InvitationState.CarArrived?"张大炮到了，小区门口见":"等待张大炮，可以按Tab看手机",SliceStage.CandyHeyPending=>"去便利店街，把喜糖交给Hey哥",SliceStage.CandyHeyDelivered=>"去汤店找张大炮",_=>"首段故事尚在开发"};
}
