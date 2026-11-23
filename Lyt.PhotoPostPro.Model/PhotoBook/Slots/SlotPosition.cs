namespace Lyt.PhotoPostPro.Model.PhotoBook.Slots;

public sealed class SlotPosition
{
    public SlotPosition() { /* for serialization */ } 

    public float X { get; set; }

    public float Y { get; set; }

    public float Dx { get; set; }

    public float Dy { get; set; }

    public SlotPosition(float x, float y, float dx, float dy)
    {
        this.X = x;
        this.Y = y;
        this.Dx = dx;
        this.Dy = dy;
    }
}
