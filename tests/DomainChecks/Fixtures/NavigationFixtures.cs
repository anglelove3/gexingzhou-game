using GeXingzhou.Domain;
namespace GeXingzhou.DomainChecks;

public static class NavigationFixtures
{
    public static NavigationProfile Soup()=>new("soup_shop",WorldMode.Depth2D,960,540,8,
        new Position2[]{new(24,360),new(936,360),new(936,520),new(24,520)},
        new[]{new Position2[]{new(460,414),new(558,414),new(558,442),new(460,442)}},
        new Dictionary<string,Position2>{["entry"]=new(100,460),["exit"]=new(48,460),
            ["stand"]=new(400,430),["memory_return"]=new(400,430),["safe"]=new(120,480),["seat"]=new(430,430)});

    public const string SoupJson="""
    {"schema_version":1,"profiles":[{"scene_id":"soup_shop","mode":"depth2d","width":960,"height":540,"foot_radius":8,
    "ground":[{"x":24,"y":360},{"x":936,"y":360},{"x":936,"y":520},{"x":24,"y":520}],
    "obstacles":[[{"x":460,"y":414},{"x":558,"y":414},{"x":558,"y":442},{"x":460,"y":442}]],
    "anchors":{"entry":{"x":100,"y":460},"exit":{"x":48,"y":460},"stand":{"x":400,"y":430},
    "memory_return":{"x":400,"y":430},"safe":{"x":120,"y":480},"seat":{"x":430,"y":430}}}]}
    """;
}
