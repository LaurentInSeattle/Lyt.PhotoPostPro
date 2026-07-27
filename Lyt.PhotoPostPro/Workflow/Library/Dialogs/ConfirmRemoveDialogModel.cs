namespace Lyt.PhotoPostPro.Workflow.Library.Dialogs;

public sealed partial class ConfirmRemoveDialogModel :  DialogViewModel<ConfirmRemoveDialog, object>
{
    [ObservableProperty]
    public partial string? Message { get; set; }

    [ObservableProperty]
    public partial string? Title { get; set; }

    public ConfirmRemoveDialogModel()
    {
        this.CanEnter = false;
        this.CanEscape = true;
        this.Title = this.Localize("Dialog.ConfirmRemove.Title"); // "Remove from Library ?";
        this.Message = this.Localize("Dialog.ConfirmRemove.Message");
        //"This master image and all its attached data such as thumbnails and edit logs will be deleted. " + 
        //    "\n\nAll files will be moved to the Recycle Bin." +
        //    "\n\nThis operation cannot be undone.";
    }

    [RelayCommand]
    public async Task OnCancel() => this.Cancel();

    [RelayCommand]
    public async Task OnRemove() => this.TrySaveAndClose() ;
}
