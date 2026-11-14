namespace Lyt.PhotoPostPro.Model.PhotoBook.Slots;

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