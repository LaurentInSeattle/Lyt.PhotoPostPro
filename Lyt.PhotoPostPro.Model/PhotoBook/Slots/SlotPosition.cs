namespace Lyt.PhotoPostPro.Model.PhotoBook.Slots;

public sealed class SlotPosition(float x, float y, float dx, float dy)
{
    public float X { get; set; } = x;

    public float Y { get; set; } = y;

    public float Dx { get; set; } = dx;

    public float Dy { get; set; } = dy;
}
