namespace GeXingzhou.Domain;
public static class NavigationGeometry
{
    public const double ContactTolerance=.01;

    public static bool CanStand(NavigationProfile p,Position2 foot)
    {
        if(p==null||!Finite(foot))return false;
        if(p.Mode==WorldMode.Horizontal)return foot.Y==280&&foot.X>=8&&foot.X<=p.Width-8;
        if(p.Mode!=WorldMode.Depth2D||!InsideGround(p,foot))return false;
        foreach(var obstacle in p.Obstacles)
            if(Contains(obstacle,foot)||Edges(obstacle).Any(e=>PointSegmentDistance(foot,e.A,e.B)<p.FootRadius-ContactTolerance))return false;
        return true;
    }

    public static bool InsideGround(NavigationProfile p,Position2 foot)
    {
        if(!Finite(foot)||p.Ground==null||p.Ground.Length<3||!Contains(p.Ground,foot))return false;
        return Edges(p.Ground).All(e=>PointSegmentDistance(foot,e.A,e.B)>=p.FootRadius-ContactTolerance);
    }

    public static bool CanTraverse(NavigationProfile p,Position2 from,Position2 to)
    {
        if(!CanStand(p,from)||!CanStand(p,to))return false;
        if(p.Mode==WorldMode.Horizontal)return true;
        // A swept circle must clear every edge, including concave ground notches.
        foreach(var polygon in new[]{p.Ground}.Concat(p.Obstacles))
            if(Edges(polygon).Any(e=>SegmentDistance(from,to,e.A,e.B)<p.FootRadius-ContactTolerance))return false;
        return true;
    }

    public static float Distance(Position2 a,Position2 b)
    {
        var dx=(double)a.X-b.X;var dy=(double)a.Y-b.Y;
        return (float)Math.Sqrt(dx*dx+dy*dy);
    }

    internal static bool IsSimplePolygon(Position2[] polygon,float width,float height)
    {
        if(polygon==null||polygon.Length is <3 or >128||polygon.Any(v=>!Finite(v)||v.X<0||v.Y<0||v.X>width||v.Y>height))return false;
        double area=0;
        for(int i=0;i<polygon.Length;i++){
            var next=(i+1)%polygon.Length;
            if(Distance(polygon[i],polygon[next])<.001)return false;
            area+=(double)polygon[i].X*polygon[next].Y-(double)polygon[next].X*polygon[i].Y;
            for(int j=i+1;j<polygon.Length;j++){
                var jnext=(j+1)%polygon.Length;
                if(j==next||jnext==i)continue;
                if(SegmentsIntersect(polygon[i],polygon[next],polygon[j],polygon[jnext]))return false;
            }
        }
        return Math.Abs(area)>.001;
    }

    private static IEnumerable<(Position2 A,Position2 B)> Edges(Position2[] polygon)
    {
        for(int i=0;i<polygon.Length;i++)yield return (polygon[i],polygon[(i+1)%polygon.Length]);
    }
    private static bool Finite(Position2 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y);
    private static bool Contains(Position2[] polygon,Position2 point)
    {
        bool inside=false;
        foreach(var (a,b) in Edges(polygon)){
            if(PointSegmentDistance(point,a,b)<1e-7)return true;
            if((a.Y>point.Y)!=(b.Y>point.Y)&&point.X<(double)(b.X-a.X)*(point.Y-a.Y)/(b.Y-a.Y)+a.X)inside=!inside;
        }
        return inside;
    }
    private static double PointSegmentDistance(Position2 p,Position2 a,Position2 b)
    {
        var dx=(double)b.X-a.X;var dy=(double)b.Y-a.Y;
        var length=dx*dx+dy*dy;
        var t=length==0?0:Math.Clamp(((p.X-a.X)*dx+(p.Y-a.Y)*dy)/length,0,1);
        var ex=p.X-a.X-t*dx;var ey=p.Y-a.Y-t*dy;
        return Math.Sqrt(ex*ex+ey*ey);
    }
    private static double SegmentDistance(Position2 a,Position2 b,Position2 c,Position2 d)
    {
        if(SegmentsIntersect(a,b,c,d))return 0;
        return Math.Min(Math.Min(PointSegmentDistance(a,c,d),PointSegmentDistance(b,c,d)),
            Math.Min(PointSegmentDistance(c,a,b),PointSegmentDistance(d,a,b)));
    }
    private static double Cross(Position2 a,Position2 b,Position2 c)=>(double)(b.X-a.X)*(c.Y-a.Y)-(double)(b.Y-a.Y)*(c.X-a.X);
    private static bool SegmentsIntersect(Position2 a,Position2 b,Position2 c,Position2 d)
    {
        var c1=Cross(a,b,c);var c2=Cross(a,b,d);var c3=Cross(c,d,a);var c4=Cross(c,d,b);
        if((c1>0&&c2<0||c1<0&&c2>0)&&(c3>0&&c4<0||c3<0&&c4>0))return true;
        return Math.Abs(c1)<1e-7&&PointSegmentDistance(c,a,b)<1e-7||Math.Abs(c2)<1e-7&&PointSegmentDistance(d,a,b)<1e-7||
            Math.Abs(c3)<1e-7&&PointSegmentDistance(a,c,d)<1e-7||Math.Abs(c4)<1e-7&&PointSegmentDistance(b,c,d)<1e-7;
    }
}
