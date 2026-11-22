namespace Lyt.PhotoPostPro.Model.PhotoBook.Typography;

public sealed class SlotTextStylesCollection
{
    public List<SlotTextStyle> AvailableSlotTextStyles { get; set; } = [];

    public SlotTextStylesCollection() { /* Required for JSON serialization */ }

    public SlotTextStyle? FromFriendlyName(string friendlyName)
        => this.AvailableSlotTextStyles.FirstOrDefault(
            s => s.FriendlyName.Equals( friendlyName, StringComparison.InvariantCultureIgnoreCase));

    public bool IsAvailable(string friendlyName)
        => this.FromFriendlyName(friendlyName) is not null;
}
