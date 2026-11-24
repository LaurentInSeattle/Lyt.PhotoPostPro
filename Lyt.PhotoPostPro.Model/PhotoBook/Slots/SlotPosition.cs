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

    public override string ToString()
        => string.Format(
            " X: {0:D3}  Y: {1:D3}  DX: {2:D3}  DY: {3:D3}  ",
            (int)MathF.Round(this.X),
            (int)MathF.Round(this.Y),
            (int)MathF.Round(this.Dx),
            (int)MathF.Round(this.Dy)); 
}
