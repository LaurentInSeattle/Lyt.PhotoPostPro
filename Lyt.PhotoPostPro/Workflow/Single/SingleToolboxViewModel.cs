namespace Lyt.PhotoPostPro.Workflow.Single;

public sealed partial class SingleToolboxViewModel : ViewModel<SingleToolboxView>
{
    [ObservableProperty]
    public partial DropViewModel DropViewModel { get; set; } 

    [ObservableProperty]
    public partial SpinViewModel SpinViewModel { get; set; } 

    [ObservableProperty]
    public partial bool ProcessIsDisabled { get; set; } = true;

    [ObservableProperty]
    public partial MetadataViewModel MetadataViewModel { get; set; }
    
        
    public SingleToolboxViewModel()
    {
        this.MetadataViewModel = new();
        this.SpinViewModel = new SpinViewModel()
        {
            IsVisible = false,
            IsActive = false,
        };

        this.DropViewModel = 
            new DropViewModel(App.GetRequiredService<SingleViewModel>(), "Single.DropZoneHelp") { IsVisible = true };
    }

#pragma warning disable CA1822 // Mark members as static
    // RelayCommand's cannot be static 

    [RelayCommand]
    public void OnProcess()
    {
        var mainWindow = App.MainWindow;
        if (mainWindow.CanMaximize)
        {
            mainWindow.WindowState = WindowState.Maximized;
        } 

        var viewModel = App.GetRequiredService<SingleViewModel>();
        viewModel.ProcessCurrentImage();
    }

#pragma warning restore CA1822
}
