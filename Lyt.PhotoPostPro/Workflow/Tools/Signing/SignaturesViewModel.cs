namespace Lyt.PhotoPostPro.Workflow.Tools.Signing;

public sealed partial class SignaturesViewModel : ViewModel<SignaturesView>
{
    private readonly PhotoPostProModel model;
    private readonly EditorViewModel editorViewModel;

    public SignaturesViewModel(PhotoPostProModel model)
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
        var vm = new SignatureEditViewModel(this.model);
        var editingForm = vm.CreateViewAndBind();
        this.editorViewModel.Populate(this.model.Signatures.AvailableSignatures, editingForm); 
    } 
}
