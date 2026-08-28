namespace Lyt.PhotoPostPro.Workflow.Shared;

public sealed partial class DropViewModel : ViewModel<DropView>, IRecipient<LanguageChangedMessage>
{
    private readonly IDropPathHandler dropPathHandler;
    private readonly string dropZoneHelpKey;

    public DropViewModel(IDropPathHandler dropPathHandler, string dropZoneHelpKey)
    {
        this.dropPathHandler = dropPathHandler;
        this.dropZoneHelpKey = dropZoneHelpKey;
        this.Localize();
        this.Subscribe<LanguageChangedMessage>();
    }

    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    [ObservableProperty]
    public partial string DropZoneHelp { get; set; } = string.Empty; 

    public void Receive(LanguageChangedMessage message) => this.Localize();

    private void Localize() => this.DropZoneHelp = this.Localize(this.dropZoneHelpKey);

    // Dispatch so that the Drag and Drop thread completes
    internal void OnDrop(string path, bool isDirectory)
        => Dispatch.OnUiThread(() => 
            { 
                this.OnDropOnUiThread(path, isDirectory); 
            }, DispatcherPriority.Background); 

    private void OnDropOnUiThread(string path, bool isDirectory)
    {
        try
        {
            this.dropPathHandler.OnDropPath(path, isDirectory);
        }
        catch (Exception ex)
        {
            this.Logger.Warning(ex.ToString());
        }
    }
}
