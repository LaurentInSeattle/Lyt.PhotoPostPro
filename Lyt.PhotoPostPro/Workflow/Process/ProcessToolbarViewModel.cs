namespace Lyt.PhotoPostPro.Workflow.Process;

public sealed partial class ProcessToolbarViewModel : ViewModel<ProcessToolbarView>
{
#pragma warning disable CA1822 // Mark members as static
    // RelayCommand's cannot be static 

    [RelayCommand]
    public void OnNext()
    {
        var model = App.GetRequiredService<PhotoPostProModel>();
        model.ClearFirstRun();
        ViewSelector<ActivatedView>.Select(ActivatedView.Folder);
    }

#pragma warning restore CA1822
}
