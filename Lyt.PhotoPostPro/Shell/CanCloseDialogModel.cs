namespace Lyt.PhotoPostPro.Shell;

public sealed partial class CanCloseDialogModel : DialogViewModel<CanCloseDialog, object>
{
    [ObservableProperty]
    public partial string? Message { get; set; }

    [ObservableProperty]
    public partial string? Title { get; set; }

    public CanCloseDialogModel()
    {
        this.CanEnter = false;
        this.CanEscape = true;
        this.Title = this.Localize("Shell.CanClose.Title"); // "Close Application ?";
        this.Message = this.Localize("Shell.CanClose.Message");
    }

    [RelayCommand]
    public void OnCancel() => this.Cancel();

    [RelayCommand]
    public void OnExit() => this.TrySaveAndClose() ;
}
