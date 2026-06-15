namespace Lyt.PhotoPostPro.Workflow.Process.Denoise;

public sealed partial class DenoiseToolboxViewModel : ViewModel<DenoiseToolboxView>
{
#pragma warning disable CA1822 // Mark members as static
    // RelayCommand's cannot be static 

    [RelayCommand]
    public void OnNext()
    {
        var model = App.GetRequiredService<PhotoPostProModel>();
        model.ClearFirstRun();
        // ViewSelector<ActivatedView>.Select(ActivatedView.Encoding);
    }

#pragma warning restore CA1822
}
