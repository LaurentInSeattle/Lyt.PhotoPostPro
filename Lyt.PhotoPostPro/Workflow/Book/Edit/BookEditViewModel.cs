namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

public class BookEditViewModel : ViewModel<BookEditView>
    // , IRecipient<DocPageNavigateMessage>
{
    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    public BookEditViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
    }
}
