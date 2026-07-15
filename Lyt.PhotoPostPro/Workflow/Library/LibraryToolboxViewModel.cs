namespace Lyt.PhotoPostPro.Workflow.Library;

public sealed partial class LibraryToolboxViewModel : ViewModel<LibraryToolboxView>, IDropPathHandler
{
    [ObservableProperty]
    public partial DropViewModel? DropViewModel { get; set; } 

    public void OnDropPath(string path, bool isDirectory) => throw new NotImplementedException();

#pragma warning disable CA1822 // Mark members as static
    // RelayCommand's cannot be static 

    [RelayCommand]
    public void OnNext()
    {
        var model = App.GetRequiredService<PhotoPostProModel>();
        model.ClearFirstRun();
        ViewSelector<ActivatedView>.Select(ActivatedView.Library);
    }

#pragma warning restore CA1822
}
