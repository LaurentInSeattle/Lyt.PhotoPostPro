namespace Lyt.PhotoPostPro.Model.PhotoBook.Slots;

public sealed class TextSlot : Slot
{
    public string Text { get; set; } = string.Empty;

    public SlotTextStyle Style { get; set; } = SlotTextStyle.SmallBodyText;
}