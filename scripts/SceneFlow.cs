using Godot;
using GeXingzhou.Domain;
public sealed record SceneChangeResult(bool Success,string? ErrorCode=null);
public partial class SceneFlow : Node
{
    public MainView Main {get;set;}=null!;private bool busy;
    public async Task<SceneChangeResult> TryEnter(string sceneId,Position2 position)
    {
        if(busy)return new(false,"busy");
        var s=GetNode<GameSession>("/root/GameSession");var source=s.Flow;busy=true;s.Flow=FlowState.Transition;
        var fade=new ColorRect{Size=new Vector2(1280,720),Color=new Color(0,0,0,0),MouseFilter=Control.MouseFilterEnum.Ignore};Main.AddChild(fade);
        bool success=false;
        try
        {
            var tween=CreateTween();tween.TweenProperty(fade,"color:a",1f,.325);await ToSignal(tween,Tween.SignalName.Finished);
            success=sceneId=="memory_soup_table"?Main.EnterMemoryView():Main.ChangeWorld(sceneId,position);
            if(success)s.UpdateScene(sceneId,position);
            tween=CreateTween();tween.TweenProperty(fade,"color:a",0f,.325);await ToSignal(tween,Tween.SignalName.Finished);
            return new(success,success?null:"missing_scene");
        }
        catch(Exception ex){return new(false,ex.GetType().Name);}
        finally{fade.QueueFree();busy=false;s.Flow=success?(sceneId=="memory_soup_table"?FlowState.Memory:FlowState.Field):source;}
    }
    public Task<SceneChangeResult> TryReturn(SceneReturnContext context)=>TryEnter(context.SceneId,context.Position);
}
