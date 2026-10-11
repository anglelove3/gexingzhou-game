using Godot;
public partial class MenuAtmosphere : Control
{
    private double phase;
    public override void _Process(double delta)
    {
        if(GetNode<GameSession>("/root/GameSession").Options.ReducedMotion)return;
        phase+=delta;
        int index=0;
        foreach(var leaf in GetChildren().OfType<Polygon2D>())
        {
            var i=index++;var distance=phase*(9+i*1.5)+i*127;
            leaf.Position=new Vector2((float)(Size.X*(.18+i*.135)+Math.Sin(phase*.32+i)*24),(float)(distance%(Size.Y+90))-40);
            leaf.Rotation=(float)Math.Sin(phase*.4+i)*.5f;
        }
    }
}
