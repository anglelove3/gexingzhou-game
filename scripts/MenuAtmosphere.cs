using Godot;
public partial class MenuAtmosphere : Control
{
    private double phase;
    public override void _Ready()
    {
        // Reuse the authored leaf shapes; keep all effects mouse-transparent and behind the menu.
        var templates=GetChildren().OfType<Polygon2D>().ToArray();
        for(int i=templates.Length;i<18;i++)AddChild(templates[i%templates.Length].Duplicate());
    }
    public override void _Process(double delta)
    {
        Visible=!GetNode<GameSession>("/root/GameSession").Options.ReducedMotion;
        if(!Visible)return;
        phase+=delta;
        int index=0;
        foreach(var leaf in GetChildren().OfType<Polygon2D>())
        {
            var i=index++;var distance=phase*(22+i%5*4)+i*173;
            // Foreground leaves favour the open right side, leaving the title readable.
            var across=.34+((i*.173)% .64);
            leaf.Position=new Vector2((float)(Size.X*across+Math.Sin(phase*.55+i)*36+Math.Sin(phase*.17+i)*15),(float)(distance%(Size.Y+100))-50);
            var scale=.65f+(i%4)*.22f;leaf.Scale=new Vector2(scale,(float)(scale*(.6+.4*Math.Cos(phase*.8+i))));
            leaf.Color=new Color(.76f+i%3*.06f,.43f+i%4*.05f,.14f,.56f+i%3*.09f);
            leaf.Rotation=(float)(phase*(i%2==0?.22:-.19)+Math.Sin(phase*.65+i)*.55);
        }
    }
}
