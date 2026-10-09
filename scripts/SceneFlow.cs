using Godot;
using GeXingzhou.Domain;
public sealed record SceneChangeResult(bool Success,string? ErrorCode=null);
public partial class SceneFlow : Node
{
    public MainView Main {get;set;}=null!;private bool busy;
    public async Task<SceneChangeResult> TryEnter(string sceneId,Position2 position)
    {
        if(busy)return new(false,"busy");
        WorldView? candidate=null;
        if(sceneId!="memory_soup_table"){candidate=Main.PrepareWorld(sceneId,position);if(candidate==null)return new(false,"invalid_scene_or_position");}
        else if(position.Y!=280||position.X<8||position.X>632||!ResourceLoader.Exists("res://scenes/world/MemorySoupTable.tscn"))return new(false,"invalid_memory");
        var s=GetNode<GameSession>("/root/GameSession");var source=s.Flow;busy=true;s.Flow=FlowState.Transition;
        var fade=Main.GetNode<ColorRect>("TransitionOverlay");fade.Color=new Color(0,0,0,0);fade.Visible=true;
        bool success=false;
        try
        {
            var half=s.Options.FadeDuration/2;var tween=CreateTween();tween.TweenProperty(fade,"color:a",1f,half);await ToSignal(tween,Tween.SignalName.Finished);
            success=sceneId=="memory_soup_table"?Main.EnterMemoryView():Main.CommitWorld(candidate!,position);
            if(success)candidate=null;
            if(success)s.UpdateScene(sceneId,position);
            Main.MoveChild(fade,Main.GetChildCount()-1);tween=CreateTween();tween.TweenProperty(fade,"color:a",0f,half);await ToSignal(tween,Tween.SignalName.Finished);
            return new(success,success?null:"missing_scene");
        }
        catch(Exception ex){return new(false,ex.GetType().Name);}
        finally{if(candidate!=null&&GodotObject.IsInstanceValid(candidate))candidate.Free();fade.Color=new Color(0,0,0,0);fade.Visible=false;busy=false;s.Flow=success?(sceneId=="memory_soup_table"?FlowState.Memory:FlowState.Field):source;}
    }
    public Task<SceneChangeResult> TryReturn(SceneReturnContext context)=>TryEnter(context.SceneId,context.Position);
}
