namespace Lyt.PhotoPostPro.Workflow.Import.Folder;

// Do not add those ImageSharp namespaces to global using as some class definitions conflict
// with the ones from Avalonia. (Point, Rectangle, etc.) 
//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.PixelFormats;

public sealed partial class FolderImportViewModel :
    ViewModel<FolderImportView>,
    ISelectListener,
    IRecipient<ImportCompleteMessage>,
    IRecipient<ImportFileMessage>
{
    private const float MegaByte = 1024.0f * 1024.0f;
    private const float MinimumDiskAvailableMegaByte = 8.0f * 1024.0f; // 8 GB 

    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;
    private readonly string downloadFolderPath;

    private int expectedFileCount;
    private FolderStatistics statistics;
    private DispatcherTimer? timer;
    private float totalSpaceRequiredMB;
    private float selectedSpaceRequiredMB;
    private float availableMegabytes;
    private bool cancelPreload;

    [ObservableProperty]
    public partial bool IsFolderMode { get; set; }

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsMessageVisible { get; set; }

    [ObservableProperty]
    public partial bool AreStatisticsVisible { get; set; }

    [ObservableProperty]
    public partial bool IsImportVisible { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<ImageCategoryViewModel> ImageCategories { get; set; }

    [ObservableProperty]
    public partial bool ShowImportButton { get; set; }

    [ObservableProperty]
    public partial string TotalSpaceRequiredString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AvailableSpace { get; set; } = string.Empty;

    [ObservableProperty]
    public partial WriteableBitmap? SelectedThumbnail { get; set; }

    [ObservableProperty]
    public partial MetadataViewModel? SelectedThumnailMetadataViewModel { get; set; }

    [ObservableProperty]
    public partial ImportThumbnailsPanelViewModel ImportThumbnailsPanelViewModel { get; set; }

    [ObservableProperty]
    public partial string ImportStatus { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool CancelButtonIsDisabled { get; set; } = true;

    [ObservableProperty]
    public partial string SelectedSpaceRequiredString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DiskSpaceAlert { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FileImported { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool AddButtonIsDisabled { get; set; } = true;

    [ObservableProperty]
    public partial bool BackButtonIsDisabled { get; set; } = true;

    public FolderImportViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
        this.statistics = new FolderStatistics(string.Empty);

        this.downloadFolderPath =
            System.IO.Path.Combine(
                this.model.RootPath, PhotoPostProModel.PhotoPostProAppName, LibraryManager.CameraDownloadsFolderName);
        this.IsFolderMode = false;
        this.IsMessageVisible = false;
        this.AreStatisticsVisible = false;
        this.IsImportVisible = false;
        this.ImageCategories = new();
        this.ImportThumbnailsPanelViewModel = new(this.model, this);
        this.Subscribe<ImportFileMessage>();
        this.Subscribe<ImportCompleteMessage>();
    }

    [RelayCommand]
    public void OnCancelImport()
    {
        this.model.LibraryManager.CancelImport();
        this.ImportStatus = this.Localize("Workflow.Import.Folder.PreloadCancelled");
    }

    public void OnFolderDrop(string path)
    {
        this.IsMessageVisible = false;
        this.AreStatisticsVisible = false;
        this.IsImportVisible = false;
        this.statistics.Clear();
        this.statistics = ImageLoader.InspectFolder(path);
        if (this.statistics.IsEmpty)
        {
            // Empty 
            this.ShowMessage("Workflow.Import.Folder.Empty");
        }
        else if (this.statistics.HasNoImage)
        {
            // With files but no images 
            this.ShowMessage("Workflow.Import.Folder.WithFilesNoImages");
        }
        else if (this.statistics.Success)
        {
            if (FileSystemExtensions.DriveInfo(this.model.RootPath) is DriveInfo driveInfo)
            {
                this.totalSpaceRequiredMB = 0.0f;
                this.TotalSpaceRequiredString = string.Empty;
                long availableBytes = driveInfo.AvailableFreeSpace;
                this.availableMegabytes = (float)availableBytes / MegaByte;
                string name = driveInfo.Name;
                string label = driveInfo.VolumeLabel;
                string reqSpaceFormat = this.Localize("Workflow.Import.Folder.ReqSpaceFormat");
                this.TotalSpaceRequiredString =
                    string.Format(reqSpaceFormat, Metadata.DiskSpaceString(0.0f));
                string diskSpace = Metadata.DiskSpaceString(this.availableMegabytes);
                string diskSpaceFormat = this.Localize("Workflow.Import.Folder.DriveInfoFormat");
                this.AvailableSpace = string.Format(diskSpaceFormat, name, label, diskSpace);
            }

            this.ShowStatistics();
        }
        else
        {
            // Error ? 
            this.ShowMessage("Workflow.Import.Folder.ErrorWhileCollecting");
        }
    }

    private void ShowMessage(string message)
    {
        this.Message = this.Localize(message);
        this.IsMessageVisible = true;
        this.AreStatisticsVisible = false;

        // Launch Timer to go back automatically after 10 seconds 
        this.timer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromSeconds(7),
        };
        timer.Tick += this.OnTimerTick;
        timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        this.DestroyTimer();
        this.GoBack();
    }

    private void DestroyTimer()
    {
        this.timer?.Stop();
        this.timer = null;
    }

    private void ShowStatistics()
    {
        this.Message = string.Empty;
        this.IsMessageVisible = false;
        this.AreStatisticsVisible = true;
        this.IsImportVisible = false;
        this.ShowImportButton = false;
        this.AddButtonIsDisabled = true;

        string path = this.statistics.Path;
        var list = new List<ImageCategoryViewModel>(this.statistics.ImageStatistics.Count);
        foreach (var imageCategory in this.statistics.ImageStatistics)
        {
            list.Add(new ImageCategoryViewModel(this, path, imageCategory));
        }

        this.ImageCategories = new(list);
    }

    internal void OnImportSelectionChanged(ImageCategoryViewModel vm, bool shouldImport)
    {
        if (shouldImport)
        {
            this.totalSpaceRequiredMB += vm.ImageStatistics.SizeOnDiskMB;

        }
        else
        {
            this.totalSpaceRequiredMB -= vm.ImageStatistics.SizeOnDiskMB;
        }

        string diskSpace = Metadata.DiskSpaceString(this.totalSpaceRequiredMB);
        string reqSpaceFormat = this.Localize("Workflow.Import.Folder.ReqSpaceFormat");
        this.TotalSpaceRequiredString = string.Format(reqSpaceFormat, diskSpace);
        this.ShowImportButton = true;
    }

    private void GoBack()
    {
        this.DestroyTimer();

        // Go back to import drop screen 
        var importVm = App.GetRequiredService<ImportViewModel>();
        importVm.SetInitialState();
    }

    [RelayCommand]
    public void OnBack() => this.GoBack();

    [RelayCommand]
    public void OnAddToLibrary()
    {
        this.CancelButtonIsDisabled = false;
        this.AddButtonIsDisabled = true;
        this.BackButtonIsDisabled = true;

        this.ImportStatus = this.Localize("Workflow.Import.Folder.Adding");

        Schedule.OnUiThread(120, () =>
        {
            // See if we need to do that in a secondary thread 
            var libraryManager = this.model.LibraryManager;
            foreach (var thumbnailVM in this.ImportThumbnailsPanelViewModel.Thumbnails)
            {
                if (!thumbnailVM.IsToAddToLibrary)
                {
                    continue;
                }

                var loadedImage = thumbnailVM.LoadedImage;
                if (libraryManager.IsAlreadyInLibrary(thumbnailVM.Metadata))
                {
                    continue;
                }

                libraryManager.AddDroppedFile(loadedImage, doSort: false);
            }

            libraryManager.SortTrees();

            Schedule.OnUiThread(120, () =>
            {
                this.ImportStatus = this.Localize("Workflow.Import.Folder.Added");
                this.BackButtonIsDisabled = false;
            }, DispatcherPriority.Background);
        }, DispatcherPriority.Background);
    }

    [RelayCommand]
    public void OnImport()
    {
        this.ShowImportButton = false;

        // Collect the list of files to import 
        var pathDictionary = new Dictionary<string, float>();
        foreach (ImageCategoryViewModel vm in this.ImageCategories)
        {
            if (!vm.IsImportIncluded)
            {
                continue;
            }

            foreach (var kvp in vm.ImageStatistics.Paths)
            {
                pathDictionary[kvp.Key] = kvp.Value;
            }
        }

        this.IsMessageVisible = false;
        this.AreStatisticsVisible = false;
        this.IsImportVisible = true;

        this.expectedFileCount = pathDictionary.Count;
        this.selectedSpaceRequiredMB = 0.0f;
        string reqSpaceFormat = this.Localize("Workflow.Import.Folder.ReqSpaceFormat");
        this.SelectedSpaceRequiredString = string.Format(reqSpaceFormat, Metadata.DiskSpaceString(0.0f));
        this.ImportThumbnailsPanelViewModel.Thumbnails.Clear();
        this.ImportStatus = this.Localize("Workflow.Import.Folder.Preloading");
        this.cancelPreload = false;
        this.CancelButtonIsDisabled = false;
        this.AddButtonIsDisabled = true;
        this.ClearSelection();

        // copy the dictionary entry keys
        var pathList = pathDictionary.Keys.ToList();
        this.model.LibraryManager.BeginImport(pathList);
    }

    public void Receive(ImportCompleteMessage message)
        => Dispatch.OnUiThread(() => { this.ReceiveOnUiThread(message); }, DispatcherPriority.Background);

    public void ReceiveOnUiThread(ImportCompleteMessage message)
    {
        this.cancelPreload = false;
        this.CancelButtonIsDisabled = true;
        string importStatusFormat = this.Localize("Workflow.Import.Folder.PreloadCompleteFormat");
        this.ImportStatus = string.Format(importStatusFormat, message.Imports, message.Errors);

        var thumbnails = this.ImportThumbnailsPanelViewModel.Thumbnails;
        if (thumbnails.Count > 0)
        {
            this.OnSelect(thumbnails[0]);
            this.AddButtonIsDisabled = false;
        }

        this.BackButtonIsDisabled = false;

        // We need to count available space when the preview step is complete
        this.CheckAvailableSpace();
    }

    private void CheckAvailableSpace()
    {
        // We need to count available space when the preview step is complete
        this.DiskSpaceAlert = string.Empty;
        if (this.selectedSpaceRequiredMB > this.availableMegabytes - MinimumDiskAvailableMegaByte)
        {
            // Alert 
            string diskSpaceAlert = this.Localize("Workflow.Import.Folder.DiskSpaceAlert");
            this.DiskSpaceAlert = diskSpaceAlert;

            // Hide Add button
            this.AddButtonIsDisabled = true;
        }
        else
        {
            this.AddButtonIsDisabled = this.selectedSpaceRequiredMB <= 0.0f;
        }
    }

    public void Receive(ImportFileMessage message)
        => Dispatch.OnUiThread(() => { this.ReceiveOnUiThread(message); }, DispatcherPriority.Background);

    public void ReceiveOnUiThread(ImportFileMessage message)
    {
        if (!this.IsActivated)
        {
            // TODO : Look up parent 
            // ignore messages if we just moved away 
            // return;
        }

        if (message.IsSuccess)
        {
            var loadedImage = message.LoadedImage;
            if (loadedImage is not null && loadedImage.IsPreLoaded)
            {
                var thumbnail = new ImportThumbnailViewModel(this, loadedImage);
                var thumbnails = this.ImportThumbnailsPanelViewModel.Thumbnails;
                thumbnails.Add(thumbnail);
                if (thumbnails.Count == 1)
                {
                    // Select the first recieved so that user has a clue about selecting images
                    this.OnSelect(thumbnail);
                }
            }
        }
        else
        {
            // this.FileDownloaded = message.Device.FriendlyName + ":  " + message.File + "  " + transferError;
        }

        this.ImportStatus =
            this.Localize("Workflow.Import.Folder.Preloading") +
            string.Format("  {0} / {1}", this.ImportThumbnailsPanelViewModel.Thumbnails.Count, this.expectedFileCount);
    }

    public void OnSelect(object selectedObject)
    {
        if (selectedObject is ImportThumbnailViewModel importThumbnailViewModel)
        {
            this.SelectedThumbnail = importThumbnailViewModel.Thumbnail;
            if (this.SelectedThumnailMetadataViewModel is null)
            {
                this.SelectedThumnailMetadataViewModel = new MetadataViewModel(importThumbnailViewModel.Metadata);
            }
            else
            {
                this.SelectedThumnailMetadataViewModel.Update(importThumbnailViewModel.Metadata);
            }
        }
    }

    private void ClearSelection()
    {
        this.SelectedThumbnail = null;
        this.SelectedThumnailMetadataViewModel = null;
    }

    internal void OnIsToAddToLibraryChanged(ImportThumbnailViewModel importThumbnailViewModel)
    {
        float sizeOnDiskMB = importThumbnailViewModel.Metadata.SizeOnDiskMB;
        if (importThumbnailViewModel.IsToAddToLibrary)
        {
            this.selectedSpaceRequiredMB += sizeOnDiskMB;
        }
        else if (this.selectedSpaceRequiredMB > 0.0f)
        {
            this.selectedSpaceRequiredMB -= sizeOnDiskMB;
        }
        else
        {
            // This should never happen 
            if (Debugger.IsAttached) { Debugger.Break(); }
            this.selectedSpaceRequiredMB = 0.0f;
        }

        string reqSpaceFormat = this.Localize("Workflow.Import.Folder.ReqSpaceFormat");
        this.SelectedSpaceRequiredString = string.Format(reqSpaceFormat, Metadata.DiskSpaceString(this.selectedSpaceRequiredMB));

        // We need to count available space when any step change
        this.CheckAvailableSpace();
    }
}
