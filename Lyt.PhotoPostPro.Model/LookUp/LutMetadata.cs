namespace Lyt.PhotoPostPro.Model.LookUp;

public enum LutFormat
{
    None = 0,
    Cube,
    ThreeDL,
}

public sealed record class LutMetadata(string FriendlyName, string Path, LutFormat LutFormat, bool IsEmbedded)
{
    public static readonly LutMetadata Empty =
        new(string.Empty, string.Empty, LutFormat: LutFormat.None, IsEmbedded: false);
}
