namespace Lyt.PhotoPostPro.Model.Export;

public sealed class ImageExportsCollection
{
    public List<ImageExport> AvailableImageExports { get; set; } = [];

    public ImageExportsCollection() { /* Required for JSON serialization */ }

    public ImageExport? FromFriendlyName(string friendlyName)
            => this.AvailableImageExports.FirstOrDefault(s => s.FriendlyName == friendlyName);
}
