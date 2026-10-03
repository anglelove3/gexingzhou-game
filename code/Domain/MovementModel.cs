namespace GeXingzhou.Domain;
public static class MovementModel
{
    public static float Step(float velocity,float axis,bool fast,bool locked,float dt,float walk=112,float run=168,float acceleration=800,float deceleration=1200)
    {
        if(locked) return 0;
        if(!float.IsFinite(dt) || dt<=0) return velocity;
        axis=float.IsFinite(axis)?Math.Clamp(axis,-1,1):0;
        var target=axis*(fast?run:walk); var amount=(axis==0?deceleration:acceleration)*dt;
        return Math.Abs(target-velocity)<=amount?target:velocity+Math.Sign(target-velocity)*amount;
    }
}
