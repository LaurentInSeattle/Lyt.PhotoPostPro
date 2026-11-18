namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

using Pages = Lyt.PhotoPostPro.Model.PhotoBook.Pages; 

public partial class BookPageViewModel : ViewModel<BookPageView>
    // , IRecipient<DocPageNavigateMessage>
{
    private readonly PhotoPostProModel model;

    private readonly Pages.Page? page; 

    [ObservableProperty]
    public partial bool IsDummy { get; set; }

    public BookPageViewModel(PhotoPostProModel model, Pages.Page? page)
    {
        this.model = model;
        this.page = page;
        this.IsDummy = page is null; 
    }
}
