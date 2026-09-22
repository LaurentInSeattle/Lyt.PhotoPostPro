namespace Lyt.PhotoPostPro.Workflow.Documentation;

public sealed partial class DocPageViewModel : ViewModel<DocPageView>
{
    public readonly int PageNumber; 

    [ObservableProperty]
    public partial string PageNumberString { get; set; } = string.Empty; 

    [ObservableProperty]
    public partial Bitmap PageBitmap { get; set; }

    public DocPageViewModel(int pageNumber, Bitmap pageBitmap)
    {
        this.PageNumber = pageNumber;
        this.PageBitmap = pageBitmap; 
    }
}
