namespace Lyt.PhotoPostPro.Workflow.Book;

public partial class BookViewModel :
    ViewModel<BookView>
    // , IRecipient<DocPageNavigateMessage>
{
    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    public BookViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
        this.BookEditViewModel = new BookEditViewModel(this.model, this.toaster);
        this.BookManageViewModel = new BookManageViewModel(this.model, this.toaster);
    }

    [ObservableProperty]
    public partial BookEditViewModel BookEditViewModel { get; set; }

    [ObservableProperty]
    public partial BookManageViewModel BookManageViewModel { get; set; }

}
