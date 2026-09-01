namespace Lyt.PhotoPostPro.Workflow.Import.Folder;

public sealed partial class ImageCategoryViewModel : ViewModel<ImageCategoryView>
{
    private readonly FolderImportViewModel parent;
    private readonly string path;
    private readonly ImageStatistics imageStatistics;

    [ObservableProperty]
    public partial string GlyphSource { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Kind { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FileCount { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SizeOnDisk { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsImportIncluded { get; set; } = false;

    public ImageStatistics ImageStatistics => this.imageStatistics;

    public ImageCategoryViewModel(
        FolderImportViewModel parent, string path, ImageStatistics imageStatistics)
    {
        this.parent = parent;
        this.path = path;
        this.imageStatistics = imageStatistics;

        switch (imageStatistics.Kind)
        {
            default:
            case ImageKind.Unrecognized:
                this.GlyphSource = "document_question_mark";
                this.Kind = "? ? ?";
                this.IsImportIncluded = false;
                break;

            case ImageKind.Jpeg:
                this.GlyphSource = "wallpaper";
                this.Kind = "JPG";
                this.IsImportIncluded = false;
                break;

            case ImageKind.Heic:
                this.GlyphSource = "food_apple";
                this.Kind = "HEIC";
                this.IsImportIncluded = false;
                break;

            case ImageKind.Raw:
                this.GlyphSource = "wallpaper";
                this.Kind = "RAW";
                this.IsImportIncluded = false;
                break;

            case ImageKind.OtherImages:
                this.GlyphSource = "wallpaper";
                this.Kind = "IMAGE";
                this.IsImportIncluded = false;
                break;

            case ImageKind.Movie:
                this.GlyphSource = "movie_and_tv";
                this.Kind = "MOVIE";
                break;
        }

        string fileCountFormat = this.Localize("Workflow.Import.Folder.FileCountFormat");
        this.FileCount = string.Format(fileCountFormat, imageStatistics.FileCount);

        string sizeOnDiskFormat = this.Localize("Workflow.Import.Folder.SizeOnDiskFormat");
        string sizeOnDisk = Metadata.DiskSpaceString(imageStatistics.SizeOnDiskMB);
        this.SizeOnDisk = string.Format(sizeOnDiskFormat, sizeOnDisk);
    }

    partial void OnIsImportIncludedChanged(bool value)
        => this.parent.OnImportSelectionChanged(this, shouldImport: value);
}
