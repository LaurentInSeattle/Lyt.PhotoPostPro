namespace Lyt.PhotoPostPro.Workflow.Documentation;

public sealed partial class DocPageViewModel(int pageNumber, Bitmap pageBitmap) : ViewModel<DocPageView>
{
    public readonly int PageNumber = pageNumber; 

    [ObservableProperty]
    public partial string PageNumberString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Bitmap PageBitmap { get; set; } = pageBitmap;
}
