namespace Lyt.PhotoPostPro.Workflow.Library;

public sealed partial class LibraryToolbarViewModel : 
    ViewModel<LibraryToolbarView>, 
    IRecipient<LibraryLoadedMessage>
{
    private readonly PhotoPostProModel model;
    private bool isProgrammaticUpdate ;

    public LibraryToolbarViewModel(PhotoPostProModel photoPostProModel)
    {
        this.model = photoPostProModel;
        this.Subscribe<LibraryLoadedMessage>();
    }

    [ObservableProperty]
    public partial bool ToolbarIsVisible { get; set; } = false;

    [ObservableProperty]
    public partial string LibraryInUse { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableLibraries { get; set; } = [];

    [ObservableProperty]
    public partial int SelectedLibraryIndex { get; set; }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
        this.UpdateToolbar(); 
    } 

    public void Receive(LibraryLoadedMessage _)
        => Dispatch.OnUiThread(this.UpdateToolbar, DispatcherPriority.Background);

    private void UpdateToolbar ()
    {
        var libraryFolders = this.model.LibraryFolders;
        if (libraryFolders.AvailableLibraryFolders.Count < 1)
        {
            this.ToolbarIsVisible = false;
            this.LibraryInUse = string.Empty;
        }
        else
        {
            With.Flag(ref this.isProgrammaticUpdate, () =>
            {
                var available = libraryFolders.AvailableLibraryFolders;
                var list = available.Select(f => f.FriendlyName).ToList();
                this.AvailableLibraries = [with(list)];
                this.SelectedLibraryIndex = -1; 
                for (int i = 0; i < available.Count; i++)
                {
                    if (available[i].FriendlyName == libraryFolders.SelectedLibraryFolderName)
                    {
                        this.SelectedLibraryIndex = i;
                        break;
                    }
                }
            });

            this.LibraryInUse = this.Localize("Library in use: ");
            this.ToolbarIsVisible = true;
        }
    }

    partial void OnSelectedLibraryIndexChanged(int value)
    {
        if (this.isProgrammaticUpdate)
        {
            return;
        }

        this.model.LibraryManager.ClearLibrary();
        Schedule.OnUiThread(120, 
            ()=> 
            {
                this.model.LibraryManager.ReloadLibrary(this.AvailableLibraries[value]);
            }, DispatcherPriority.Background);
    }
}
