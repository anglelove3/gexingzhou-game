using Godot;
public static class UiStyles
{
    public static StyleBoxFlat Panel()=>new(){BgColor=new Color("17202a"),BorderColor=new Color("526273"),BorderWidthLeft=1,BorderWidthRight=1,BorderWidthTop=1,BorderWidthBottom=1,ContentMarginLeft=16,ContentMarginRight=16,ContentMarginTop=12,ContentMarginBottom=12};
    public static ColorRect Dim()=>new(){Size=new Vector2(1280,720),Color=new Color(0,0,0,.72f),MouseFilter=Control.MouseFilterEnum.Ignore};
}
