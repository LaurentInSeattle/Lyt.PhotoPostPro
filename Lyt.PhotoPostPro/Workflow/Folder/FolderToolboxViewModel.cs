namespace Lyt.PhotoPostPro.Workflow.Folder;

using Lyt.PhotoPostPro.Workflow.Shared;

public sealed partial class FolderToolboxViewModel : ViewModel<FolderToolboxView>
{
    [ObservableProperty]
    public partial DropViewModel DropViewModel { get; set; } = new DropViewModel();

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
