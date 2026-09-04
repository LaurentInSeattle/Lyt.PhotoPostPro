namespace Lyt.PhotoPostPro.Workflow.Tools.Exporting;

public sealed partial class ExportsViewModel : ViewModel<ExportsView>
{
    private readonly PhotoPostProModel model;
    private readonly EditorViewModel editorViewModel;

    public ExportsViewModel(PhotoPostProModel model)
    {
        this.model = model;
        this.editorViewModel = new EditorViewModel(this.model);
    }
    public override void OnViewLoaded()
    {
        base.OnViewLoaded();
        this.View.EditorView.DataContext = this.editorViewModel;
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
        var vm = new ExportEditViewModel(this.model);
        var editingForm = vm.CreateViewAndBind();

        // TODO 
        // Need to refactor the model to have a collection of available exports, similar to watermarks and signatures,
        // so that we can populate the editor view with them.
        //
        // this.editorViewModel.Populate(this.model.Exports.AvailableExports, editingForm);

        this.editorViewModel.Populate(this.model.Signatures.AvailableSignatures, editingForm);
    }
}
