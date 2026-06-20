namespace Lyt.PhotoPostPro.Workflow.Single;

public sealed partial class SingleToolboxViewModel : ViewModel<SingleToolboxView>
{
    [ObservableProperty]
    public partial DropViewModel DropViewModel { get; set; } 

    [ObservableProperty]
    public partial SpinViewModel SpinViewModel { get; set; } 

    [ObservableProperty]
    public partial bool ProcessIsDisabled { get; set; } = true;

    public SingleToolboxViewModel()
    {
        this.SpinViewModel = new SpinViewModel()
        {
            IsVisible = false,
            IsActive = false,
        };

        this.DropViewModel = new DropViewModel
        {
            IsVisible = true
        };
    }
#pragma warning disable CA1822 // Mark members as static
    // RelayCommand's cannot be static 

    [RelayCommand]
    public void OnNext()
    {
        var viewModel = App.GetRequiredService<SingleViewModel>();
        viewModel.ProcessCurrentImage();
    }

#pragma warning restore CA1822
}
