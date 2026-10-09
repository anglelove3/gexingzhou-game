namespace GeXingzhou.Domain;
public static class Movement2DModel
{
    public static Position2 Step(Position2 velocity,Position2 axis,bool fast,bool locked,float dt)
    {
        if(locked||!Finite(velocity)||!Finite(axis)||!float.IsFinite(dt)||dt<0)return new(0,0);
        if(dt==0)return velocity;
        var length=Math.Sqrt((double)axis.X*axis.X+(double)axis.Y*axis.Y);
        var speed=fast?168d:112d;
        var tx=length==0?0:axis.X/length*speed;
        var ty=length==0?0:axis.Y/length*speed;
        var dx=tx-velocity.X;var dy=ty-velocity.Y;
        var distance=Math.Sqrt(dx*dx+dy*dy);
        var amount=(length==0?1200d:800d)*dt;
        if(distance<=amount)return new((float)tx,(float)ty);
        return new((float)(velocity.X+dx/distance*amount),(float)(velocity.Y+dy/distance*amount));
    }
    private static bool Finite(Position2 value)=>float.IsFinite(value.X)&&float.IsFinite(value.Y);
}
