namespace Lyt.PhotoPostPro.Workflow.Process.Export;

public sealed partial class ImageExportViewModel(
    ExportViewModel parent, ImageExport imageExport) : ViewModel<ImageExportView>
{
    private readonly ExportViewModel parent = parent;
    private readonly ImageExport imageExport = imageExport;

    [ObservableProperty]
    public partial string Name { get; set; } =
            string.Format("{0}  -  {1}", imageExport.FriendlyName, imageExport.OutputFormat.ToString());

    [ObservableProperty]
    public partial string Description { get; set; } = imageExport.Description;

    [ObservableProperty]
    public partial bool IsExportIncluded { get; set; } = imageExport.IsGalleryFormat;

    [ObservableProperty]
    public partial bool IsExportIncludedEnabled { get; set; } = !imageExport.IsGalleryFormat;

    public ImageExport ImageExport => this.imageExport;

    partial void OnIsExportIncludedChanged(bool value) => this.parent.OnExportSelectionChanged();
}
