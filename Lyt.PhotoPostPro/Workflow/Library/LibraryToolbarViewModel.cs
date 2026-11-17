namespace Lyt.PhotoPostPro.Workflow.Library;

public sealed partial class LibraryToolbarViewModel : 
    ViewModel<LibraryToolbarView>,
    IRecipient<BookListChangedMessage>,
    IRecipient<LibraryLoadedMessage>
{
    private readonly PhotoPostProModel model;
    private bool isProgrammaticUpdate ;

    public LibraryToolbarViewModel(PhotoPostProModel photoPostProModel)
    {
        this.model = photoPostProModel;
        this.Subscribe<LibraryLoadedMessage>();
        this.Subscribe<BookListChangedMessage>();
    }

    [ObservableProperty]
    public partial bool PhotoBookIsVisible { get; set; } = false;

    [ObservableProperty]
    public partial string PhotoBookInUse { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<string> AvailablePhotoBooks { get; set; } = [];

    [ObservableProperty]
    public partial int SelectedPhotoBookIndex { get; set; }

    [ObservableProperty]
    public partial bool LibraryIsVisible { get; set; } = false;

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

    public void Receive(BookListChangedMessage _)
        => Dispatch.OnUiThread(this.UpdateToolbar, DispatcherPriority.Background);

    public void Receive(LibraryLoadedMessage _)
        => Dispatch.OnUiThread(this.UpdateToolbar, DispatcherPriority.Background);

    private void UpdateToolbar ()
    {
        // Update library 
        var libraryFolders = this.model.LibraryFolders;
        if (libraryFolders.AvailableLibraryFolders.Count < 1)
        {
            this.LibraryIsVisible = false;
            this.LibraryInUse = string.Empty;
        }
        else
        {
            With.Flag(ref this.isProgrammaticUpdate, () =>
            {
                var available = libraryFolders.AvailableLibraryFolders;
                var names = available.Select(f => f.FriendlyName).ToList();
                this.AvailableLibraries = [with(names)];
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

            this.LibraryInUse = this.Localize("Shell.Folder") + " :";
            this.LibraryIsVisible = true;
        }

        // Update Photo Books 
        var photoBooks = this.model.BookManager.Books;
        if ((photoBooks.Count == 0) || (this.model.BookManager.CurrentBook is null))
        {
            this.PhotoBookIsVisible = false;
            this.PhotoBookInUse = string.Empty;
        }
        else
        {
            With.Flag(ref this.isProgrammaticUpdate, () =>
            {
                var list = photoBooks.Select(f => f.FriendlyName).ToList();
                this.AvailablePhotoBooks = [with(list)];
                this.SelectedPhotoBookIndex = -1;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] == this.model.BookManager.CurrentBook.FriendlyName)
                    {
                        this.SelectedPhotoBookIndex = i;
                        break;
                    }
                }
            });

            this.PhotoBookInUse = this.Localize("Shell.PhotoBook") + " :";
            this.PhotoBookIsVisible = true;
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

    partial void OnSelectedPhotoBookIndexChanged(int value)
    {
        if (this.isProgrammaticUpdate)
        {
            return;
        }

        Schedule.OnUiThread(50,
            () =>
            {
                this.model.BookManager.SelectBook(this.AvailablePhotoBooks[value]);
            }, DispatcherPriority.Background);
    }
}
