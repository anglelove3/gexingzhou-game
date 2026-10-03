using Godot;
using GeXingzhou.Domain;
public partial class PlayerController : CharacterBody2D
{
    private bool locked; private float facing=1;
    public override void _Ready()
    {
        CollisionLayer=2;CollisionMask=1;
        AddChild(new CollisionShape2D{Position=new Vector2(0,-28),Shape=new RectangleShape2D{Size=new Vector2(16,56)}});
        AddChild(new Camera2D{Name="Camera2D",Position=new Vector2(0,-100),PositionSmoothingEnabled=false,LimitLeft=0,LimitTop=0,LimitBottom=360});
    }
    public void SetInputLocked(bool value) {locked=value;if(value)Velocity=Vector2.Zero;}
    public override void _PhysicsProcess(double delta)
    {
        var session=GetNode<GameSession>("/root/GameSession");
        float axis=(Input.IsPhysicalKeyPressed(Key.D)||Input.IsPhysicalKeyPressed(Key.Right)?1:0)-(Input.IsPhysicalKeyPressed(Key.A)||Input.IsPhysicalKeyPressed(Key.Left)?1:0);
        var p=session.Catalog!.Parameters;
        float vx=MovementModel.Step(Velocity.X,axis,Input.IsPhysicalKeyPressed(Key.Shift),locked||(session.Flow!=FlowState.Field), (float)delta,(float)p["move.walk_speed"],(float)p["move.run_speed"],(float)p["move.acceleration"],(float)p["move.deceleration"]);
        Velocity=new Vector2(vx,0);MoveAndSlide(); if(axis!=0)facing=Math.Sign(axis);
        session.UpdatePosition(new(Position.X,Position.Y));QueueRedraw();
    }
    public override void _Draw()
    {
        DrawRect(new Rect2(-9,-33,8,31),new Color("26313d"));DrawRect(new Rect2(1,-33,8,31),new Color("26313d"));
        DrawRect(new Rect2(-11,-50,22,26),new Color("475b6d"));DrawRect(new Rect2(-5,-49,10,22),new Color("171e26"));
        DrawCircle(new Vector2(0,-57),9,new Color("c6a58b"));DrawRect(new Rect2(-10,-66,20,8),new Color("22262c"));
        DrawLine(new Vector2(-facing*9,-62),new Vector2(-facing*15,-49),new Color("202329"),3);
        DrawLine(new Vector2(6,-30),new Vector2(11,-15),new Color("99a6a8"),1);
        DrawRect(new Rect2(-10,-5,10,5),new Color("111820"));DrawRect(new Rect2(1,-5,10,5),new Color("111820"));
    }
}
