namespace Lyt.PhotoPostPro.Workflow.Tools.Libraries;

public sealed partial class LibrariesViewModel : ViewModel<LibrariesView>, IEditorDataProvider
{
    private readonly PhotoPostProModel model;
    private readonly EditorViewModel editorViewModel;

    public LibrariesViewModel(PhotoPostProModel model)
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
        var vm = new LibrariesEditViewModel(this.model, this.editorViewModel);
        var editingForm = vm.CreateViewAndBind();

        this.editorViewModel.Populate(this.model.LibraryFolders.AvailableLibraryFolders, editingForm);
    }

    public void Refresh() => this.editorViewModel.Refresh(this.model.LibraryFolders.AvailableLibraryFolders);
}
