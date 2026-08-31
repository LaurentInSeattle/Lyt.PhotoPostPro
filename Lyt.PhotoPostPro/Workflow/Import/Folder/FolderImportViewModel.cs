namespace Lyt.PhotoPostPro.Workflow.Import.Folder;

using Lyt.FileSystem;

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
        this.cancelPreload = true;
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
                    string.Format(reqSpaceFormat, this.DiskSpaceString(0.0f));
                string diskSpace = this.DiskSpaceString(this.availableMegabytes);
                string diskSpaceFormat = this.Localize("Workflow.Import.Folder.DriveInfoFormat");
                this.AvailableSpace =
                    string.Format(diskSpaceFormat, name, label, diskSpace);
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

        string diskSpace = this.DiskSpaceString(this.totalSpaceRequiredMB);
        string reqSpaceFormat = this.Localize("Workflow.Import.Folder.ReqSpaceFormat");
        this.TotalSpaceRequiredString = string.Format(reqSpaceFormat, diskSpace);

        if (this.totalSpaceRequiredMB > this.availableMegabytes - MinimumDiskAvailableMegaByte)
        {
            // Alert 
            // Hide Import button
            this.ShowImportButton = false;
        }
        else
        {
            this.ShowImportButton = this.totalSpaceRequiredMB > 0.0f;
        }
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
        var pathList = new List<string>();
        foreach (ImageCategoryViewModel vm in this.ImageCategories)
        {
            if (!vm.IsImportIncluded)
            {
                continue;
            }

            pathList.AddRange(vm.ImageStatistics.Paths);
        }

        this.IsMessageVisible = false;
        this.AreStatisticsVisible = false;
        this.IsImportVisible = true;

        this.expectedFileCount = pathList.Count;
        this.ImportThumbnailsPanelViewModel.Thumbnails.Clear();
        this.ImportStatus = this.Localize("Workflow.Import.Folder.Preloading");
        this.cancelPreload = false;
        this.CancelButtonIsDisabled = false;
        this.AddButtonIsDisabled = true;
        this.ClearSelection();

        // Launch the Import thread 
        _ = Task.Run(async () =>
            {
                // copy the list !
                this.BeginImport(pathList.ToList());
            });
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
                this.ImportThumbnailsPanelViewModel.Thumbnails.Add(thumbnail);
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

    private async void BeginImport(List<string> pathList)
    {
        bool completed = false;
        int errors = 0;
        int imports = 0;
        try
        {
            // Speed up this loop 
            var options = new ParallelOptions()
            {
                // Limit to 4 concurrent threads
                MaxDegreeOfParallelism = 4 
            };

            Parallel.For(0, pathList.Count, options, async (index) =>
            {
                if (this.cancelPreload)
                {
                    return;
                }

                string file = pathList[index];
                bool success = this.ImportFile(file);
                if (success)
                {
                    Interlocked.Increment(ref imports);
                }
                else
                {
                    Interlocked.Increment(ref errors);
                    this.Logger.Warning("Download error" + file);
                }

                // Throttle so that the UI has enough time to show the thumbanil 
                await Task.Delay(40);
            });

            completed = true;
        }
        catch (Exception ex)
        {
            this.Logger.Warning($" Error while importing files: {ex.Message}");
        }
        finally
        {
            new ImportCompleteMessage(completed, pathList.Count, imports, errors).Publish();
        }
    }

    private bool ImportFile(string file)
    {
        try
        {
            if (!System.IO.File.Exists(file))
            {
                new ImportFileMessage(IsSuccess: false, Path: file, Message: "No Such File.").Publish();
                return false;
            }

            LoadedImage loadedImage = ImageLoader.PreLoadImage(file);
            if (loadedImage.IsSuccess && loadedImage.IsPreLoaded)
            {
                // ! Verified by loadedImage.IsPreLoaded
                new ImportFileMessage(
                    IsSuccess: true, Path: file, Message: "Success", loadedImage).Publish();
                return true;
            }

            new ImportFileMessage(IsSuccess: false, Path: file, Message: "Unknown Error").Publish();
            return false;
        }
        catch (Exception ex)
        {
            this.Logger.Warning(" Import File: Exception thrown: " + ex);
            new ImportFileMessage(IsSuccess: false, Path: file, Message: "Exception thrown: " + ex).Publish();
            return false;
        }
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

    public string DiskSpaceString(float megabytes)
    {
        if (megabytes <= 0.0)
        {
            return "---";
        }

        bool isBig = megabytes > 1999.999f;
        string unit = isBig ? "GB" : "MB";
        if (isBig)
        {
            megabytes /= 1024.0f;
        }

        bool isHuge = megabytes > 1999.999f;
        if (isHuge)
        {
            unit = "TB";
            megabytes /= 1024.0f;
        }

        return string.Format("{0:F1} {1}", megabytes, unit);
    }
}
