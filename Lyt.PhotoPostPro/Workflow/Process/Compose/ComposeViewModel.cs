namespace Lyt.PhotoPostPro.Workflow.Process.Compose;

public sealed partial class ComposeViewModel : StepViewModel<ComposeView>
{
    [ObservableProperty]
    public partial CropGridViewModel CropGridViewModel { get; set; }

    [ObservableProperty]
    public partial double ImageWidth { get; set; }

    [ObservableProperty]
    public partial double ImageHeight { get; set; }

    public override void OnViewLoaded()
    { 
        base.OnViewLoaded();
        this.CreateCropVmIfNeeded();
        this.CropGridViewModel.OnActivate();
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
        this.CreateCropVmIfNeeded();
        this.CropGridViewModel.OnActivate();
    }

    public override void Deactivate() 
    {
        base.Deactivate();
        this.CropGridViewModel.OnDeactivate();
    } 

    protected override void OnImageReceived(WriteableBitmap bitmap) 
    {
        var imageSize = bitmap.Size;
        this.ImageWidth = imageSize.Width;
        this.ImageHeight = imageSize.Height;
    }

    internal void SelectCropGuidelines(int cgIndex) => this.CropGridViewModel.SelectCg(cgIndex);

    private void CreateCropVmIfNeeded()
    {
        if (this.CropGridViewModel is null)
        {
            // This cannot be done in the CTOR 
            var composeToolboxViewModel = App.GetRequiredService<ComposeToolboxViewModel>();
            this.CropGridViewModel = new CropGridViewModel(this, composeToolboxViewModel);
        }
    }
}