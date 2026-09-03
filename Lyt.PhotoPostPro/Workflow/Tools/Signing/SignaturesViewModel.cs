namespace Lyt.PhotoPostPro.Workflow.Tools.Signing;

public sealed partial class SignaturesViewModel : ViewModel<SignaturesView>
{
    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;
    private readonly EditorViewModel editorViewModel;

    public SignaturesViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
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

        this.editorViewModel.Populate(
            this.model.Signatures.AvailableSignatures, 
            new UserControl() { Background = Brushes.MintCream}); 
    } 
}
