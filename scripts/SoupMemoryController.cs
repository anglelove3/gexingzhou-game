using Godot;
using GeXingzhou.Domain;

public partial class SoupMemoryController : Control
{
    public MainView Main {get;set;}=null!;
    private Control table=null!, delivery=null!;
    private Label status=null!, help=null!;
    private TextureRect bowl=null!;
    private Button[] coins=Array.Empty<Button>(), foods=Array.Empty<Button>();
    private Vector2[] near=Array.Empty<Vector2>(), far=Array.Empty<Vector2>();
    private readonly Dictionary<int,Tween> moves=new();
    private Tween? bowlMove;
    private int selected, dragged=-1;
    private Vector2 dragStart, grabOffset;
    private bool foodReady;
    private MemoryState? shown;
    private GameSession Session=>GetNode<GameSession>("/root/GameSession");
    public override void _Ready()
    {
        try
        {
            table=SceneBindings.Require<Control>(this,"Table");
            Resized+=UpdateTableLayout;UpdateTableLayout();
            delivery=SceneBindings.Require<Control>(table,"DeliveryArea");
            status=SceneBindings.Require<Label>(this,"Status");
            help=SceneBindings.Require<Label>(this,"Help");
            bowl=SceneBindings.Require<TextureRect>(table,"Bowl");
            coins=Enumerable.Range(1,4).Select(i=>SceneBindings.Require<Button>(table,"Coin"+i)).ToArray();
            near=Enumerable.Range(1,4).Select(i=>SceneBindings.Require<Marker2D>(table,"Near"+i).Position).ToArray();
            far=Enumerable.Range(1,4).Select(i=>SceneBindings.Require<Marker2D>(table,"Far"+i).Position).ToArray();
            foods=new[]{"Take","Wait","Share"}.Select(id=>SceneBindings.Require<Button>(table,"Foods/"+id)).ToArray();
            for(int i=0;i<4;i++){var index=i;coins[i].Pressed+=()=>{if(dragged<0)TryPush(index);};}
            for(int i=0;i<3;i++){var index=i;foods[i].Pressed+=()=>Resolve(index);}
            SceneBindings.Require<Button>(this,"HelpToggle").Pressed+=()=>help.Visible=!help.Visible;
            GetWindow().FocusExited+=CancelDrag;
            RestoreCoinPositions();
        }
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    private void UpdateTableLayout()
    {
        if(table==null||GetNodeOrNull<TextureRect>("Background")?.Texture is not {} texture||Size.X<=0||Size.Y<=0)return;
        var dimensions=texture.GetSize();
        if(dimensions.X<=0||dimensions.Y<=0)return;
        // Authored points use the 1280x720 cover crop; apply the same cover ratio
        // around the same centre as the background, rather than screen coordinates.
        var reference=Math.Max(1280f/dimensions.X,720f/dimensions.Y);
        var cover=Math.Max(Size.X/dimensions.X,Size.Y/dimensions.Y);
        table.Scale=Vector2.One*(cover/reference);
    }
    public override void _Process(double delta)
    {
        if(HasMeta("binding_failure"))return;
        var state=Session.Snapshot.MemoryState;
        if(state!=shown)RestoreCoinPositions();
    }
    private void SetStatus(MemoryState state)=>status.Text=$"那年清晨 · {state.PushedTotal}/5"+(state.Replay?" · 重看":"");
    public void RestoreCoinPositions()
    {
        if(coins.Length!=4)return;
        CancelDrag();foreach(var move in moves.Values)move.Kill();moves.Clear();bowlMove?.Kill();bowlMove=null;
        var state=Session.Snapshot.MemoryState;shown=state;if(state==null)return;
        SetStatus(state);
        for(int i=0;i<4;i++)
        {
            var pushed=state.PushedCoinIds.Contains("c"+(i+1));
            coins[i].Disabled=pushed;coins[i].Position=pushed?far[i]:near[i];
        }
        foodReady=state.PushedTotal==5;
        bowl.Position=SceneBindings.Require<Marker2D>(table,foodReady?"BowlNear":"BowlFar").Position;
        foreach(var item in foods)item.Disabled=!foodReady;
        FocusCoin(-1);
    }
    private Vector2 Local(Vector2 viewportPosition)=>table.GetGlobalTransformWithCanvas().AffineInverse()*viewportPosition;
    public override void _Input(InputEvent ev)
    {
        if(HasMeta("binding_failure")||Session.Flow!=FlowState.Memory)return;
        if(ev is InputEventMouseButton click&&click.ButtonIndex==MouseButton.Left)
        {
            var p=Local(click.Position);
            if(click.Pressed)
            {
                for(int i=0;i<4;i++)if(!coins[i].Disabled&&new Rect2(coins[i].Position,coins[i].Size).HasPoint(p))
                {
                    dragged=i;selected=i;dragStart=p;grabOffset=p-coins[i].Position;coins[i].GrabFocus();GetViewport().SetInputAsHandled();break;
                }
            }
            else if(dragged>=0)
            {
                var index=dragged;dragged=-1;
                var valid=CoinDragPolicy.IsDelivery(new(dragStart.X,dragStart.Y),new(p.X,p.Y),new(delivery.Position.X,delivery.Position.Y,delivery.Size.X,delivery.Size.Y));
                if(!valid||!TryPush(index))coins[index].Position=Session.Snapshot.MemoryState!.PushedCoinIds.Contains("c"+(index+1))?far[index]:near[index];
                GetViewport().SetInputAsHandled();
            }
        }
        else if(ev is InputEventMouseMotion motion&&dragged>=0){coins[dragged].Position=Local(motion.Position)-grabOffset;GetViewport().SetInputAsHandled();}
    }
    public void CancelDrag()
    {
        if(dragged<0)return;
        var index=dragged;dragged=-1;
        coins[index].Position=Session.Snapshot.MemoryState?.PushedCoinIds.Contains("c"+(index+1))==true?far[index]:near[index];
    }
    public bool TryPush(int index)
    {
        if(index<0||index>=4||Session.Flow!=FlowState.Memory||Session.Snapshot.MemoryState is not {} state)return false;
        var id="c"+(index+1);
        if(!Session.TryDispatch(new("memory.coin.push",id,state.InstanceId+":"+id)).Applied)return false;
        Main?.Audio.PlayCue(AudioCue.Coin);
        shown=Session.Snapshot.MemoryState!;SetStatus(shown);coins[index].Disabled=true;
        if(moves.Remove(index,out var old))old.Kill();
        var tween=CreateTween();moves[index]=tween;
        tween.TweenProperty(coins[index],"position",far[index],Session.Options.ReducedMotion?.08:.35).SetTrans(Tween.TransitionType.Sine);
        tween.Finished+=()=>moves.Remove(index);
        if(shown.PushedTotal==5)
        {
            foodReady=false;foreach(var item in foods)item.Disabled=true;
            bowlMove=CreateTween();bowlMove.TweenProperty(bowl,"position",SceneBindings.Require<Marker2D>(table,"BowlNear").Position,Session.Options.ReducedMotion?.15:.65);
            bowlMove.Finished+=()=>{bowlMove=null;if(!IsInsideTree())return;Main?.Audio.PlayCue(AudioCue.Bowl);foodReady=true;foreach(var item in foods)item.Disabled=false;selected=0;foods[0].GrabFocus();};
        }
        else FocusCoin(index);
        return true;
    }
    private void FocusCoin(int after)
    {
        if(foodReady){selected=0;foods[0].GrabFocus();return;}
        for(int offset=1;offset<=4;offset++){var next=(after+offset+4)%4;if(!coins[next].Disabled){selected=next;coins[next].GrabFocus();break;}}
    }
    private void Resolve(int index)
    {
        if(index<0||index>=foods.Length||!foodReady||Session.Flow!=FlowState.Memory||Session.Snapshot.MemoryState is not {} state||Main==null)return;
        if(Session.TryDispatch(new("memory.food.resolve",new[]{"Take","Wait","Share"}[index],state.InstanceId)).Applied)_=Main.ReturnMemory(true);
    }
    public void HandleKey(Key key)
    {
        if(Session.Flow!=FlowState.Memory)return;
        if(key==Key.Escape){CancelDrag();if(Main!=null)_=Main.ReturnMemory(false);return;}
        if(key is Key.Left or Key.A or Key.Right or Key.D)
        {
            var direction=key is Key.Left or Key.A?-1:1;
            if(foodReady){selected=(selected+direction+3)%3;foods[selected].GrabFocus();}
            else for(int offset=1;offset<=4;offset++){var next=(selected+direction*offset+8)%4;if(!coins[next].Disabled){selected=next;coins[next].GrabFocus();break;}}
        }
        else if(key is Key.E or Key.Enter){if(foodReady)Resolve(selected);else TryPush(selected);}
    }
    public override void _ExitTree(){Resized-=UpdateTableLayout;GetWindow().FocusExited-=CancelDrag;CancelDrag();foreach(var move in moves.Values)move.Kill();moves.Clear();bowlMove?.Kill();bowlMove=null;}
}
