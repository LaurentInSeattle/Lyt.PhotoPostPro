namespace Lyt.PhotoPostPro.Model.Export;

public sealed class WatermarksCollection
{
    public List<Watermark> AvailableWatermarks { get; set; } = [];

    public WatermarksCollection() { /* Required for JSON serialization */ }

    public Watermark? FromFriendlyName(string friendlyName)
            => this.AvailableWatermarks.FirstOrDefault(s => s.FriendlyName == friendlyName);
}
