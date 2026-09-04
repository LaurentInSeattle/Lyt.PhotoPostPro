namespace Lyt.PhotoPostPro.Model.Export;

/// <summary> Will load from disk ~ LATER </summary>
public sealed class WatermarksCollection
{
    public List<Watermark> AvailableWatermarks { get; set; } = [];

    public WatermarksCollection() => this.AvailableWatermarks.Add(Watermark.Default);

    public Watermark? FromFriendlyName(string friendlyName)
            => this.AvailableWatermarks.FirstOrDefault(s => s.FriendlyName == friendlyName);
}
