using GeXingzhou.Domain;
namespace GeXingzhou.DomainChecks;
public static class CoinDragChecks
{
    public static void Register(List<(string,string,Action)> tests)
    {
        tests.Add(("CoinDrag","ForwardOnly",()=>{
            var area=new DeliveryRect(200,100,200,80);var start=new DragPoint(250,260);
            Check.True(CoinDragPolicy.IsDelivery(start,new(250,140),area));
            Check.True(!CoinDragPolicy.IsDelivery(start,new(500,260),area));
            Check.True(!CoinDragPolicy.IsDelivery(start,new(250,360),area));
            Check.True(!CoinDragPolicy.IsDelivery(new(250,120),new(250,140),area));
            Check.True(CoinDragPolicy.IsDelivery(start,new(200,100),area));
            Check.True(CoinDragPolicy.IsDelivery(start,new(400,180),area));
        }));
        tests.Add(("CoinDrag","RejectsBadNumbers",()=>{
            foreach(var bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
            {
                Check.True(!CoinDragPolicy.IsDelivery(new(bad,260),new(250,140),new(200,100,200,80)));
                Check.True(!CoinDragPolicy.IsDelivery(new(250,260),new(250,bad),new(200,100,200,80)));
                Check.True(!CoinDragPolicy.IsDelivery(new(250,260),new(250,140),new(200,bad,200,80)));
            }
            foreach(var size in new[]{0f,-1,float.NaN,float.PositiveInfinity})
            {Check.True(!CoinDragPolicy.IsDelivery(new(250,260),new(250,140),new(200,100,size,80)));Check.True(!CoinDragPolicy.IsDelivery(new(250,260),new(250,140),new(200,100,200,size)));}
        }));
    }
}
