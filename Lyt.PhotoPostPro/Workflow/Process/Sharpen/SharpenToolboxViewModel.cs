namespace Lyt.PhotoPostPro.Workflow.Process.Sharpen;

public sealed partial class SharpenToolboxViewModel : ViewModel<SharpenToolboxView>
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
