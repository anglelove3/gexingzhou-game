using Godot;

// Measured regions of the v1 atlases. Source PNGs are kept intact; only runtime views are cropped.
public static class ArtAssets
{
    public const string Root="res://assets/art/vs01-v1/";
    private static readonly Dictionary<string,Texture2D> textures=new();
    private static readonly Dictionary<string,AtlasTexture> frames=new();
    private static readonly Rect2[] playerRegions={new(247,9,125,434),new(645,10,126,433),new(1034,10,126,433),new(1422,10,126,433),new(166,443,318,423),new(616,443,158,423),new(953,443,315,423),new(1405,443,155,424)};
    private static readonly Rect2[] npcRegions={new(219,11,158,497),new(707,11,138,497),new(1184,11,161,500),new(161,576,301,410),new(685,518,260,481),new(1187,518,191,484)};
    private static readonly Rect2[] propRegions={new(95,84,334,335),new(602,84,330,335),new(1039,76,463,374),new(60,521,449,422),new(645,504,247,456),new(1024,547,482,353)};
    public static Texture2D Texture(string file)
    {
        if(!textures.TryGetValue(file,out var texture))
        {
            texture=GD.Load<Texture2D>(Root+file)??throw new InvalidOperationException("Required game artwork missing: "+file);
            textures[file]=texture;
        }
        return texture;
    }
    private static AtlasTexture Frame(string file,int index,Rect2[] regions)
    {
        var key=file+":"+index;
        if(!frames.TryGetValue(key,out var frame))
        {frame=new AtlasTexture{Atlas=Texture(file),Region=regions[index],FilterClip=true};frames[key]=frame;}
        return frame;
    }
    public static AtlasTexture Player(int frame)=>Frame("player-v1.png",frame,playerRegions);
    public static AtlasTexture Npc(int frame)=>Frame("npcs-v1.png",frame,npcRegions);
    public static AtlasTexture Prop(int frame)=>Frame("props-v1.png",frame,propRegions);
    public static Sprite2D Grounded(Texture2D texture,Vector2 feet,float height=68,string name="Artwork")
    {
        float scale=height/(texture.GetHeight()-4);
        return new Sprite2D{Name=name,Texture=texture,Scale=Vector2.One*scale,Position=feet+new Vector2(0,-(texture.GetHeight()/2f-2)*scale),TextureFilter=CanvasItem.TextureFilterEnum.Linear};
    }
    public static TextureRect Picture(Texture2D texture,Vector2 position,Vector2 size,string name="Artwork")=>new()
    {
        Name=name,ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,Texture=texture,Position=position,Size=size,
        StretchMode=TextureRect.StretchModeEnum.KeepAspectCovered,MouseFilter=Control.MouseFilterEnum.Ignore,TextureFilter=CanvasItem.TextureFilterEnum.Linear
    };
}
