namespace Lyt.PhotoPostPro.Dialogs;

public sealed partial class ConfirmRemoveDialogModel :  DialogViewModel<ConfirmRemoveDialog, object>
{
    [ObservableProperty]
    public partial string? Message { get; set; }

    [ObservableProperty]
    public partial string? Title { get; set; }

    public ConfirmRemoveDialogModel()
    {
        this.Title = "Remove from Library ?";
        this.CanEnter = false;
        this.CanEscape = true;
        this.Message = "This master image and all its attached data such as thumbnails and edit logs will be deleted. This operation cannot be undone.";
    }

    [RelayCommand]
    public async Task OnCancel() => this.Cancel();

    [RelayCommand]
    public async Task OnRemove() => this.TrySaveAndClose() ;
}
