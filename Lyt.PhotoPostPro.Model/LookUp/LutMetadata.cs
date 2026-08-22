namespace Lyt.PhotoPostPro.Model.LookUp;

[JsonConverter(typeof(JsonStringEnumConverter<LutFormat>))]
public enum LutFormat
{
    // Use to clear embedded LUT == No LUT 
    None = 0,

    // Uses when a file path is provided and the LUT type is still unknown 
    Unknown = 1,

    // For Embedded LUTs or successfully decoded LUTs 
    Cube,
    ThreeDL,
}

public sealed record class LutMetadata(string FriendlyName, string Path, LutFormat LutFormat, bool IsEmbedded)
{
    public static readonly LutMetadata Empty =
        new(string.Empty, string.Empty, LutFormat: LutFormat.None, IsEmbedded: false);

    public bool IsEmpty 
        =>
        this.LutFormat == LutFormat.None ||
        ( string.IsNullOrWhiteSpace(this.FriendlyName) && string.IsNullOrWhiteSpace(this.Path)); 
}
