namespace Lyt.PhotoPostPro.Model.PhotoBook.Slots;

public class SlotPosition
{
    public int X { get; set; }

    public int Y { get; set; }

    public int Dx { get; set; }

    public int Dy { get; set; }
}

public class Slot
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public SlotPosition Position { get; set; } = new SlotPosition();
}

public sealed class ImageSlot : Slot
{
    public string ImageFileName { get; set; } = string.Empty;
}

public sealed class SlotTextStyle
{
    public string Key { get; set; } = string.Empty;

    public string FontFamily { get; set; } = string.Empty;

    public int FontSize { get; set; }

    public string Color { get; set; } = string.Empty;
}

public sealed class TextSlot : Slot
{
    public string Text { get; set; } = string.Empty;

    public SlotTextStyle Style { get; set; } = new SlotTextStyle();
}