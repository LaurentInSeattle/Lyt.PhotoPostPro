namespace Lyt.PhotoPostPro.Workflow.Import.Folder;

using Lyt.FileSystem;

using static Lyt.PhotoPostPro.Workflow.Culling.CullingViewModel;

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

    private List<string> preloadedFiles;
    private FolderStatistics statistics;
    private DispatcherTimer? timer;
    private float totalSpaceRequiredMB;
    private float availableMegabytes;

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
    public partial string DownloadButtonText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool DownloadButtonIsDisabled { get; set; } = true;

    [ObservableProperty]
    public partial string FileImported { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool AddToLibraryButtonIsDisabled { get; set; } = true;

    [ObservableProperty]
    public partial bool RemoveFromCameraButtonIsDisabled { get; set; } = true;

    public FolderImportViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
        this.statistics = new FolderStatistics(string.Empty);
        this.preloadedFiles = new();

        this.downloadFolderPath =
            System.IO.Path.Combine(this.model.RootPath, PhotoPostProModel.PhotoPostProAppName, "CameraDownloads");
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
                // string format = "Workflow.Import.Folder.ReqSPace";
                // string format = "Workflow.Import.Folder.DriveInfo";
                this.TotalSpaceRequiredString = "Required Space: " + this.DiskSpaceString(0.0f);
                string diskSpace = this.DiskSpaceString(this.availableMegabytes);
                this.AvailableSpace =
                    string.Format("Available Space on {0} ({1}): {2}", name, label, diskSpace);
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
        this.TotalSpaceRequiredString = string.Format("Required Space: {0}", diskSpace);

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
        // Go back to import drop screen 
        var importVm = App.GetRequiredService<ImportViewModel>();
        importVm.SetInitialState();
    }

    [RelayCommand]
    public void OnBack() => this.GoBack();

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

        this.preloadedFiles = new(pathList.Count);
        this.ImportThumbnailsPanelViewModel.Thumbnails.Clear();
        this.ClearSelection(); 
        
        //this.FileDownloaded = string.Empty;
        //this.downloadedFiles.Clear();
        //this.ThumbnailsPanelViewModel.Thumbnails.Clear();
        //this.cameraMgr.BeginDownloadingFiles(this.foundDevice, this.selectedFiles);
        //this.DownloadButtonText = this.Localize(CancelTransferLocKey);

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

        //string transferedTo = this.Localize(TransferedToLocKey);
        //string transferError = this.Localize(TransferErrorLocKey);
        if (message.IsSuccess)
        {
            this.preloadedFiles.Add(message.Path);
            if (message.ThumbnailBytes is not null && message.Metadata is not null)
            {
                var thumbnail = new ImportThumbnailViewModel(this, message.Metadata, message.ThumbnailBytes);
                this.ImportThumbnailsPanelViewModel.Thumbnails.Add(thumbnail);
            }
        }
        else
        {
            // this.FileDownloaded = message.Device.FriendlyName + ":  " + message.File + "  " + transferError;
        }
    }

    private async void BeginImport(List<string> pathList)
    {
        bool completed = false;
        int errors = 0;
        int imports = 0;
        try
        {
            // TODO 
            // Speed up this loop 
            foreach (string file in pathList)
            {
                if (!this.ImportFile(file))
                {
                    ++errors;
                    this.Logger.Warning("Download error");
                }
                else
                {
                    ++imports;
                }

                // Throttle so that the UI has enough time to show the thumbanil 
                await Task.Delay(60);
            }

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
                    IsSuccess: true,
                    Path: file,
                    Message: "Success",
                    loadedImage.Metadata,
                    loadedImage.JpgThumbnail!).Publish();
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

    private string DiskSpaceString(float megabytes)
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
