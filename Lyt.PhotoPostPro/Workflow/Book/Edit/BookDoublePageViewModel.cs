namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

public partial class BookDoublePageViewModel : ViewModel<BookDoublePageView>
// , IRecipient<DocPageNavigateMessage>
{
    private readonly PhotoPostProModel model;

    [ObservableProperty]
    public partial BookPageViewModel LeftPage { get; set; }

    [ObservableProperty]
    public partial BookPageViewModel RightPage { get; set; }

    public BookDoublePageViewModel(
        PhotoPostProModel model, BookPageViewModel left, BookPageViewModel right)
    {
        this.model = model;
        this.LeftPage = left;
        this.RightPage = right;
    }
}
