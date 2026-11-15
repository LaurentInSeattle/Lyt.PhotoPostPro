namespace Lyt.PhotoPostPro.Model.PhotoBook.Slots;

public sealed class ImageSlot : Slot
{
    public string ImageFileName { get; set; } = string.Empty;

    public int ImageWidth { get; set; }

    public int ImageHeight { get; set; }

    public BookImageStretch ImageStretch { get; set; } = BookImageStretch.Fit;

    public float AspectRatio => (float)this.ImageWidth / (float)this.ImageHeight;
}