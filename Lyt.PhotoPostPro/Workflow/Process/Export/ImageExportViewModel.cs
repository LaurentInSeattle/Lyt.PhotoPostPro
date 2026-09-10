namespace Lyt.PhotoPostPro.Workflow.Process.Export;

public sealed partial class ImageExportViewModel : ViewModel<ImageExportView>
{
    private readonly ExportViewModel parent;
    private readonly ImageExport imageExport;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsExportIncluded { get; set; } = false;

    [ObservableProperty]
    public partial bool IsExportIncludedEnabled { get; set; } = false;

    public ImageExport ImageExport => this.imageExport;

    public ImageExportViewModel(
        ExportViewModel parent, ImageExport imageExport)
    {
        this.parent = parent;
        this.imageExport = imageExport;

        // Special format is always exported 
        this.IsExportIncluded = imageExport.IsGalleryFormat;
        this.IsExportIncludedEnabled = ! imageExport.IsGalleryFormat;
        this.Name = 
            string.Format( "{0}  -  {1}" , imageExport.FriendlyName, imageExport.OutputFormat.ToString());
        this.Description = imageExport.Description; 
    }

    partial void OnIsExportIncludedChanged(bool value) => this.parent.OnExportSelectionChanged();
}
