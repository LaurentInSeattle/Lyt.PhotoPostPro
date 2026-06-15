namespace Lyt.PhotoPostPro.Model.Utilities;

public sealed class Histogram
{
    public required bool IsClippingLow { get; set; }

    public required bool IsClippingHigh { get; set; }

    public required float[] Bins { get; set; }
}