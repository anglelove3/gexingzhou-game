namespace GeXingzhou.Domain;
public sealed record ApproachPath(IReadOnlyList<Position2> Points,float Length);
public static class ShortApproachPolicy
{
    public static ApproachPath? TryPlan(NavigationProfile nav,Position2 from,Position2 to,float maxLength=72)
    {
        if(nav==null||!float.IsFinite(maxLength)||maxLength<0||!NavigationGeometry.CanStand(nav,from)||!NavigationGeometry.CanStand(nav,to)||NavigationGeometry.Distance(from,to)>maxLength)return null;
        if(from==to)return new(Array.AsReadOnly(new[]{from}),0);
        if(NavigationGeometry.CanTraverse(nav,from,to))return new(Array.AsReadOnly(new[]{from,to}),NavigationGeometry.Distance(from,to));
        var queue=new PriorityQueue<(int X,int Y),(float Cost,int X,int Y)>();
        var costs=new Dictionary<(int X,int Y),float>{[(0,0)]=0};var parents=new Dictionary<(int X,int Y),(int X,int Y)>();
        queue.Enqueue((0,0),(0,0,0));float best=maxLength+.001f; (int X,int Y)? end=null;
        Position2 Point((int X,int Y) node)=>new(from.X+node.X*4,from.Y+node.Y*4);
        while(queue.TryDequeue(out var node,out var priority)){
            if(priority.Cost!=costs[node]||priority.Cost>=best)continue;
            var current=Point(node);var remaining=NavigationGeometry.Distance(current,to);
            if(priority.Cost+remaining<=maxLength&&priority.Cost+remaining<best&&NavigationGeometry.CanTraverse(nav,current,to)){best=priority.Cost+remaining;end=node;}
            for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++){
                if(x==0&&y==0)continue;var next=(X:node.X+x,Y:node.Y+y);var point=Point(next);
                var cost=priority.Cost+4*(x!=0&&y!=0?MathF.Sqrt(2):1);
                if(cost>=best||cost+NavigationGeometry.Distance(point,to)>maxLength||costs.TryGetValue(next,out var prior)&&prior<=cost||!NavigationGeometry.CanTraverse(nav,current,point))continue;
                costs[next]=cost;parents[next]=node;queue.Enqueue(next,(cost,next.X,next.Y));
            }
        }
        if(end==null)return null;
        var points=new List<Position2>();var cursor=end.Value;points.Add(Point(cursor));
        while(cursor!=(0,0)){cursor=parents[cursor];points.Add(Point(cursor));}
        points.Reverse();if(points[^1]!=to)points.Add(to);
        return new(points.AsReadOnly(),best);
    }
}
