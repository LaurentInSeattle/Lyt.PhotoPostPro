namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

public partial class BookDoublePageViewModel(
    BookPageViewModel left, BookPageViewModel right, double pageWidth, double pageHeight) :
    ViewModel<BookDoublePageView>
{
    private const double separatorWidth = 12.0;

    [ObservableProperty]
    public partial BookPageViewModel LeftPage { get; set; } = left;

    [ObservableProperty]
    public partial BookPageViewModel RightPage { get; set; } = right;

    [ObservableProperty]
    public partial double DoublePageWidth { get; set; } = 2.0 * pageWidth + separatorWidth;

    [ObservableProperty]
    public partial GridLength SeparatorWidth { get; set; } = new GridLength(separatorWidth, GridUnitType.Pixel);

    [ObservableProperty]
    public partial GridLength PageWidth { get; set; } = new GridLength(pageWidth, GridUnitType.Pixel);

    [ObservableProperty]
    public partial GridLength PageHeight { get; set; } = new GridLength(pageHeight, GridUnitType.Pixel);
}
