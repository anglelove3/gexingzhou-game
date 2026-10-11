using Godot;
public partial class SmokeHarness
{
    private async Task MovementPolishChecks()
    {
        var main=await NewPolishMain();
        try
        {
            var player=main.World.Player;var frames=player.DepthFrames;
            foreach(var direction in new[]{"front","back","side"})
            {
                var name="walk_"+direction;
                Require(frames.GetFrameCount(name)>=8,"Walking cycle still lacks eight phases: "+direction);
                var images=new HashSet<string>();
                for(int i=0;i<frames.GetFrameCount(name);i++)
                {
                    var frame=(AtlasTexture)frames.GetFrameTexture(name,i);
                    images.Add(frame.Atlas.ResourcePath+frame.Region);
                    Require(frame.GetMeta("foot_pivot").AsVector2().IsFinite(),"Walking foot anchor invalid");
                }
                Require(images.Count>=8,"Walking repeats the same few poses: "+direction);
            }
        }
        finally{main.Free();await Frames(2);}
    }
}
