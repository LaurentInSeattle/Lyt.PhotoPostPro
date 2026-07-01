namespace Lyt.PhotoPostPro.Model.ProcessModels;

public sealed class ExportParameters
{
    public List<ImageParameters> Images { get; set; } = [];

    public ExportParameters()
    {
        // Adds the default export
        this.Images.Add(ImageParameters.Default);

        // Adds the HD sized export
        this.Images.Add(ImageParameters.FullHd);

        // Adds the thumbnail export
        this.Images.Add(ImageParameters.Thumbnail);
    }
}
