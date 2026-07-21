namespace Lyt.PhotoPostPro.Workflow.Camera;

public sealed partial class CameraViewModel :
    ViewModel<CameraView>,
    ISelectListener,
    IRecipient<DevicesFoundMessage>,
    IRecipient<DeviceStatusMessage>,
    IRecipient<DeviceFileListMessage>,
    IRecipient<DeviceFileDownloadedMessage>,
    IRecipient<DeviceDownloadCompleteMessage>,
    IRecipient<DeviceFileDeletedMessage>,
    IRecipient<DeviceDeleteCompleteMessage>
{
    private readonly PhotoPostProModel model;
    private readonly CameraManager cameraMgr;
    private readonly LibraryManager libraryMgr;
    private readonly List<string> selectedFiles;
    private readonly List<string> downloadedFiles;

    private FoundDevice? foundDevice;
    // private bool nothingSaved;
    private bool isDownloading;

    public CameraViewModel(PhotoPostProModel photoPostProModel)
    {
        this.model = photoPostProModel;
        this.cameraMgr = this.model.CameraManager;
        this.libraryMgr = this.model.LibraryManager;
        this.selectedFiles = [];
        this.downloadedFiles = [];
        this.ThumbnailsPanelViewModel = new(this.model, this);
        this.OtherFilesPanelViewModel = new(this.model, this);

        this.Subscribe<DevicesFoundMessage>();
        this.Subscribe<DeviceStatusMessage>();
        this.Subscribe<DeviceFileListMessage>();
        this.Subscribe<DeviceFileDownloadedMessage>();
        this.Subscribe<DeviceDownloadCompleteMessage>();
        this.Subscribe<DeviceFileDeletedMessage>();
        this.Subscribe<DeviceDeleteCompleteMessage>();
    }

    [ObservableProperty]
    public partial bool ShowImages { get; set; } = true;

    [ObservableProperty]
    public partial OtherFilesPanelViewModel OtherFilesPanelViewModel { get; set; }

    [ObservableProperty]
    public partial WriteableBitmap? SelectedThumbnail { get; set; }

    [ObservableProperty]
    public partial MetadataViewModel? SelectedThumnailMetadataViewModel { get; set; }

    [ObservableProperty]
    public partial ThumbnailsPanelViewModel ThumbnailsPanelViewModel { get; set; }

    [ObservableProperty]
    public partial string DeviceStatus { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DownloadButtonText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool DownloadButtonIsDisabled { get; set; } = true;

    [ObservableProperty]
    public partial string FileDownloaded { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool AddToLibraryButtonIsDisabled { get; set; } = true;

    [ObservableProperty]
    public partial bool RemoveFromCameraButtonIsDisabled { get; set; } = true;

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
        this.NullifyDevice();
        this.selectedFiles.Clear();
        this.downloadedFiles.Clear();
        this.model.CameraManager.ClearDownloadFolder();

        this.DeviceStatus = string.Empty;
        this.FileDownloaded = string.Empty;

        // Enforce property changed 
        this.DownloadButtonIsDisabled = false;
        this.DownloadButtonIsDisabled = true;
        this.DownloadButtonText = "Begin Transfer";

        this.AddToLibraryButtonIsDisabled = true;
        this.RemoveFromCameraButtonIsDisabled = true;
        this.ShowImages = true; 
        this.isDownloading = false;
        // this.nothingSaved = true;
        this.cameraMgr.BeginMonitoringCameraConnexion();
    }

    public override void Deactivate()
    {
        base.Deactivate();
        this.NullifyDevice();
        this.cameraMgr.EndMonitoringCameraConnexion();
        this.cameraMgr.EndDownloadingFiles();

        this.selectedFiles.Clear();
        this.downloadedFiles.Clear();
        this.ThumbnailsPanelViewModel.Thumbnails.Clear();

        // Do not clear the download folder in case we need or want to recover
        // any file left in there
    }

    public void Receive(DevicesFoundMessage message)
        => Dispatch.OnUiThread(() => { this.OnDevicesConnected(message); }, DispatcherPriority.Background);

    public void Receive(DeviceStatusMessage message)
        => Dispatch.OnUiThread(() => { this.OnDeviceStatus(message); }, DispatcherPriority.Background);

    public void Receive(DeviceFileListMessage message)
        => Dispatch.OnUiThread(() => { this.OnDeviceFileList(message); }, DispatcherPriority.Background);

    public void Receive(DeviceFileDownloadedMessage message)
        => Dispatch.OnUiThread(() => { this.OnDeviceFileDownloaded(message); }, DispatcherPriority.Background);

    public void Receive(DeviceDownloadCompleteMessage message)
        => Dispatch.OnUiThread(() => { this.OnDeviceDownloadComplete(message); }, DispatcherPriority.Background);

    public void Receive(DeviceFileDeletedMessage message)
        => Dispatch.OnUiThread(() => { this.OnDeviceFileDeleted(message); }, DispatcherPriority.Background);

    public void Receive(DeviceDeleteCompleteMessage message)
        => Dispatch.OnUiThread(() => { this.OnDeviceDeleteComplete(message); }, DispatcherPriority.Background);

    private void NullifyDevice()
    {
        if (this.foundDevice is not null)
        {
            this.model.CameraManager.DisposeDevice(this.foundDevice);
            this.foundDevice = null;
        }
    }

    private void OnDevicesConnected(DevicesFoundMessage message)
    {
        if (!this.IsActivated)
        {
            // ignore messages if we just moved away 
            return;
        }

        var list = message.Devices;
        if (list.Count == 0)
        {
            // TODO: localize
            this.DeviceStatus = "No camera or device found...";

            this.NullifyDevice();
            Schedule.OnUiThread(
                CameraManager.FastCameraMonitoringTime_ms / 2,
                () => { this.DeviceStatus = string.Empty; },
                DispatcherPriority.ApplicationIdle);
        }
        else
        {
            // TODO: localize
            // Friendly name not available yet
            this.DeviceStatus = "Device or Camera detected.";
        }
    }

    private void OnDeviceStatus(DeviceStatusMessage message)
    {
        if (!this.IsActivated)
        {
            // ignore messages if we just moved away 
            return;
        }

        // TODO: localize
        this.DeviceStatus =
            message.Device.FriendlyName + ": " +
            (message.IsConnected ? "Connected" : "Not responding.");
        this.FileDownloaded = string.Empty;
    }

    private void OnDeviceFileList(DeviceFileListMessage message)
    {
        if (!this.IsActivated)
        {
            // ignore messages if we just moved away 
            return;
        }

        // TODO: localize
        this.FileDownloaded = string.Empty;
        var device = message.Device;
        if (message.Files.Count == 0)
        {
            this.DeviceStatus = device.FriendlyName + ": No files on device.";
            this.selectedFiles.Clear();
            this.NullifyDevice();
        }
        else
        {
            this.foundDevice = device;
            this.DeviceStatus = device.FriendlyName + ": " + message.Files.Count + " files ready to transfer.";
            this.DownloadButtonIsDisabled = false;
            this.selectedFiles.Clear();
            this.selectedFiles.AddRange(message.Files);
            this.downloadedFiles.Clear();
        }
    }

    private void OnDeviceFileDownloaded(DeviceFileDownloadedMessage message)
    {
        if (!this.IsActivated)
        {
            // ignore messages if we just moved away 
            return;
        }

        if (message.IsSuccess)
        {
            this.downloadedFiles.Add(message.File);
            this.FileDownloaded =
                message.Device.FriendlyName + ":  " + message.File + "  transfer to: " + message.Path;
            if (message.ThumbnailBytes is not null && message.Metadata is not null)
            {
                var thumbnail = new CameraThumbnailViewModel(this, message.Metadata, message.ThumbnailBytes);
                this.ThumbnailsPanelViewModel.Thumbnails.Add(thumbnail);
            }
        }
        else if ( message.IsDownloaded)
        {
            this.FileDownloaded =
                message.Device.FriendlyName + ":  " + message.File + "  transfer to: " + message.Path;
            var cameraFile = new CameraFileViewModel(this, message);
            this.OtherFilesPanelViewModel.Files.Add(cameraFile);
        }
        else
                {
            this.FileDownloaded = message.Device.FriendlyName + ":  " + message.File + "  transfer error.";
        }
    }

    private void OnDeviceDownloadComplete(DeviceDownloadCompleteMessage message)
    {
        if (!this.IsActivated)
        {
            // ignore messages if we just moved away 
            return;
        }

        // Update UI 
        this.DownloadButtonText = "Begin Transfer";
        if (message.Completed)
        {
            this.FileDownloaded =
                string.Format(
                    "Transfer complete: {0} images transfered, other files or errors: {1} ",
                    message.DownloadedCount, message.ErrorCount);
        }
        else
        {
            this.FileDownloaded = "Transfer Aborted.";
        }

        // Sort thumbnails by date ascending 
        this.ThumbnailsPanelViewModel.Sort(ascending: true);

        // Update visibility of action buttons:  Enable buttons
        this.AddToLibraryButtonIsDisabled = false;
        this.RemoveFromCameraButtonIsDisabled = false;

        // this.UpdateVisibilityOfActionButtons();
    }

    private void UpdateVisibilityOfActionButtons()
    {
        // Update visibility of action buttons depending of checkboxes states 
        var thumbs = this.ThumbnailsPanelViewModel.Thumbnails;
        bool anyToAddToLibrary =
            (from thumb in thumbs where thumb.IsToAddToLibrary select thumb).Any();
        if (anyToAddToLibrary)
        {
            this.AddToLibraryButtonIsDisabled = false;
        }

        bool anyToRemoveFromCamera = (from thumb in thumbs where thumb.IsToRemoveFromCamera select thumb).Any();
        if (anyToRemoveFromCamera)
        {
            this.RemoveFromCameraButtonIsDisabled = false;
        }
    }

    private void OnDeviceFileDeleted(DeviceFileDeletedMessage message)
    {
        if (!this.IsActivated)
        {
            // ignore messages if we just moved away 
            return;
        }

        if (message.IsSuccess)
        {
            this.downloadedFiles.Add(message.File);
            this.FileDownloaded = message.Device.FriendlyName + ":  " + message.File + "  deleted from camera.";

            // Remove thumb from panel 
            var thumbViewModel = 
                (from vm in this.ThumbnailsPanelViewModel.Thumbnails
                 where vm.Metadata.CameraFullPath == message.File
                 select vm )
                 .FirstOrDefault();
            if (thumbViewModel is not null)
            {
                this.ThumbnailsPanelViewModel.Thumbnails.Remove(thumbViewModel);
            }
        }
        else
        {
            this.FileDownloaded = message.Device.FriendlyName + ":  " + message.File + " Delete error.";
        }
    }

    private void OnDeviceDeleteComplete(DeviceDeleteCompleteMessage message)
    {
        if (!this.IsActivated)
        {
            // ignore messages if we just moved away 
            return;
        }

        // Update UI 
        this.DownloadButtonText = "Begin Transfer";
        if (message.Completed)
        {
            this.FileDownloaded =
                string.Format(
                    "Deletion complete: {0} images deleted, errors: {1} ",
                    message.DeletedCount, message.ErrorCount);
        }
        else
        {
            this.FileDownloaded = "Deletion Aborted.";
        }

        // Sort thumbnails by date ascending 
        this.ThumbnailsPanelViewModel.Sort(ascending: true);

        // Update visibility of action buttons: Enable buttons 
        this.AddToLibraryButtonIsDisabled = false;
        this.RemoveFromCameraButtonIsDisabled = false;
    }

    [RelayCommand]
    public void OnDownload()
    {
        if (this.foundDevice is null || this.selectedFiles.Count == 0)
        {
            return;
        }

        if (this.isDownloading)
        {
            this.cameraMgr.EndDownloadingFiles();
            this.DownloadButtonText = "Begin Transfer";
        }
        else
        {
            this.FileDownloaded = string.Empty;
            this.downloadedFiles.Clear();
            this.ThumbnailsPanelViewModel.Thumbnails.Clear();
            this.cameraMgr.BeginDownloadingFiles(this.foundDevice, this.selectedFiles);
            this.DownloadButtonText = "Cancel Transfer";
        }

        this.isDownloading = !this.isDownloading;

        // Disable buttons while transfering 
        this.AddToLibraryButtonIsDisabled = true;
        this.RemoveFromCameraButtonIsDisabled = true;
    }

    [RelayCommand]
    public void OnAddToLibrary()
    {
        // TODO: Later: avoid making that copy 
        var toAddToLibrary =
            (from thumb in this.ThumbnailsPanelViewModel.Thumbnails
             where thumb.IsToAddToLibrary
             select thumb.Metadata).ToList();
        if (toAddToLibrary.Count == 0)
        {
            // TODO : Message User 
            return;
        }

        bool success = this.libraryMgr.AddDownloadedFiles(toAddToLibrary);

        // TODO : Message User about file count 
        //if (success)
        //{
        //    this.nothingSaved = false;
        //}
    }

    [RelayCommand]
    public void OnRemoveFromCamera()
    {
        if (this.foundDevice is null)
        {
            return;
        }

        if (this.ThumbnailsPanelViewModel.Thumbnails.Count == 0)
        {
            return;
        }

        // LATER 
        //if ( this.nothingSaved )
        //{
        //    // TODO : Message User 
        //    return; 
        //}

        // TODO: Later: avoid making that copy 
        var toRemoveFromCamera =
            (from thumb in this.ThumbnailsPanelViewModel.Thumbnails
             where (thumb.IsToRemoveFromCamera) && !string.IsNullOrWhiteSpace(thumb.Metadata.CameraFullPath)
             select thumb.Metadata.CameraFullPath).ToList();
        if (toRemoveFromCamera.Count == 0)
        {
            // TODO : Message User 
            return;
        }

        // Disable buttons while deleting 
        this.AddToLibraryButtonIsDisabled = true;
        this.RemoveFromCameraButtonIsDisabled = true;

        // Delete 
        this.cameraMgr.BeginDeletingFiles(this.foundDevice, toRemoveFromCamera);
    }

    public void OnSelect(object selectedObject)
    {
        if (selectedObject is CameraThumbnailViewModel cameraThumbnailViewModel)
        {
            this.SelectedThumbnail = cameraThumbnailViewModel.Thumbnail;
            if (this.SelectedThumnailMetadataViewModel is null)
            {
                this.SelectedThumnailMetadataViewModel = new MetadataViewModel(cameraThumbnailViewModel.Metadata);
            }
            else
            {
                this.SelectedThumnailMetadataViewModel.Update(cameraThumbnailViewModel.Metadata);
            }
        }
    }

    internal void RemoveFromCamera(CameraFileViewModel cameraFileViewModel, string file)
    {
        if (this.foundDevice is null)
        {
            return;
        }

        // Disable buttons while deleting 
        this.AddToLibraryButtonIsDisabled = true;
        this.RemoveFromCameraButtonIsDisabled = true;

        // Delete 
        this.cameraMgr.BeginDeletingFiles(this.foundDevice, [file]);

        // Remove entry in UI list 
        this.OtherFilesPanelViewModel.Files.Remove(cameraFileViewModel);

    }
}
