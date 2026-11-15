namespace Lyt.PhotoPostPro.Workflow.Book.Manage;

public class BookManageViewModel : ViewModel<BookManageView>
// , IRecipient<DocPageNavigateMessage>
{
    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    public BookManageViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
    }
}
