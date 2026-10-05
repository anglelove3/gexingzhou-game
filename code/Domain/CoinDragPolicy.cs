namespace GeXingzhou.Domain;

public sealed record DragPoint(float X, float Y);
public sealed record DeliveryRect(float X, float Y, float Width, float Height);
public static class CoinDragPolicy
{
    public static bool IsDelivery(DragPoint start, DragPoint end, DeliveryRect area) =>
        float.IsFinite(start.X) && float.IsFinite(start.Y) &&
        float.IsFinite(end.X) && float.IsFinite(end.Y) &&
        float.IsFinite(area.X) && float.IsFinite(area.Y) &&
        float.IsFinite(area.Width) && float.IsFinite(area.Height) &&
        area.Width > 0 && area.Height > 0 && end.Y < start.Y &&
        end.X >= area.X && end.X <= (double)area.X + area.Width &&
        end.Y >= area.Y && end.Y <= (double)area.Y + area.Height;
}
