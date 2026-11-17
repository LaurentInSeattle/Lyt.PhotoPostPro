namespace Lyt.PhotoPostPro.Workflow.Tools.PhotoBooks;

public sealed partial class PhotoBookViewModel : ViewModel<PhotoBookView>, IEditorDataProvider
{
    private readonly PhotoPostProModel model;
    private readonly EditorViewModel editorViewModel;

    public PhotoBookViewModel(PhotoPostProModel model)
    {
        this.model = model;
        this.editorViewModel = new EditorViewModel(this, this.model);
    }
    public override void OnViewLoaded()
    {
        base.OnViewLoaded();
        this.View.EditorView.DataContext = this.editorViewModel;
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
        var vm = new PhotoBookEditViewModel(this.model, this.editorViewModel);
        var editingForm = vm.CreateViewAndBind();
        this.editorViewModel.Populate(this.model.BookManager.Books, editingForm);
    }

    public bool CanEdit => false;

    public void Refresh() => this.editorViewModel.Refresh(this.model.BookManager.Books);
}
