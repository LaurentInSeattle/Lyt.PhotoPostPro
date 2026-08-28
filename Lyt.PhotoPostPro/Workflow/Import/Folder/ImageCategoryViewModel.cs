namespace Lyt.PhotoPostPro.Workflow.Import.Folder;

public sealed partial class ImageCategoryViewModel : ViewModel<ImageCategoryView>
{
    private readonly string path; 
    private readonly ImageStatistics imageStatistics;

    public ImageCategoryViewModel(string path, ImageStatistics imageStatistics)
    {
        this.path = path;
        this.imageStatistics = imageStatistics;
    }
    /*

    public ImageKind Kind { get; private set; } = kind;

    public int FileCount { get; private set; } // = 0;

    public float SizeOnDiskMB { get; private set; } // = 0.0f;

    public List<string> Paths { get; private set; } = []; 

    */
}
