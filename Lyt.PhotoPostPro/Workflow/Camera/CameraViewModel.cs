namespace Lyt.PhotoPostPro.Workflow.Camera;

public sealed partial class CameraViewModel :
    ViewModel<CameraView>,
    ISelectListener,
    IRecipient<DevicesFoundMessage>,
    IRecipient<DeviceStatusMessage>,
    IRecipient<DeviceFileListMessage>,
    IRecipient<DeviceFileDownloadedMessage>
{
    private readonly PhotoPostProModel model;
    private readonly CameraManager cameraMgr;
    private readonly List<string> selectedFiles;
    private readonly List<string> downloadedFiles;

    private FoundDevice? foundDevice;

    public CameraViewModel(PhotoPostProModel photoPostProModel)
    {
        this.model = photoPostProModel;
        this.cameraMgr = this.model.CameraManager;
        this.selectedFiles = [];
        this.downloadedFiles = [];
        this.ThumbnailsPanelViewModel = new(this.model, this);
        
        this.Subscribe<DevicesFoundMessage>();
        this.Subscribe<DeviceStatusMessage>();
        this.Subscribe<DeviceFileListMessage>();
        this.Subscribe<DeviceFileDownloadedMessage>();
    }

    [ObservableProperty]
    public partial ThumbnailsPanelViewModel ThumbnailsPanelViewModel { get; set; } 
 
    [ObservableProperty]
    public partial string DevicesFound { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DeviceStatus { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FileCount { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FileDownloaded { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FileError { get; set; } = string.Empty;

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
        this.NullifyDevice();
        this.selectedFiles.Clear();
        this.downloadedFiles.Clear();

        this.DeviceStatus = string.Empty;
        this.FileCount = string.Empty;
        this.FileDownloaded = string.Empty;
        this.FileError = string.Empty;

        this.cameraMgr.BeginMonitoringCameraConnexion();
    }

    public override void Deactivate()
    {
        base.Deactivate();
        this.NullifyDevice();
        this.selectedFiles.Clear();
        this.downloadedFiles.Clear();
        this.ThumbnailsPanelViewModel.Thumbnails.Clear();
        this.cameraMgr.EndMonitoringCameraConnexion();
        this.cameraMgr.EndDownloadingFiles();
    }

    public void Receive(DevicesFoundMessage message)
        => Dispatch.OnUiThread(() => { this.OnDevicesConnected(message); }, DispatcherPriority.Background);

    public void Receive(DeviceStatusMessage message)
        => Dispatch.OnUiThread(() => { this.OnDeviceStatus(message); }, DispatcherPriority.Background);

    public void Receive(DeviceFileListMessage message)
        => Dispatch.OnUiThread(() => { this.OnDeviceFileList(message); }, DispatcherPriority.Background);

    public void Receive(DeviceFileDownloadedMessage message)
        => Dispatch.OnUiThread(() => { this.OnDeviceFileDownloaded(message); }, DispatcherPriority.Background);

    private void NullifyDevice ()
    {
        if (this.foundDevice is not null)
        {
            this.model.CameraManager.DisposeDevice(this.foundDevice);
            this.foundDevice = null; 
        } 
    }

    private void OnDevicesConnected(DevicesFoundMessage message)
    {
        var list = message.Devices; 
        if ( list.Count == 0)
        {
            // TODO: localize
            this.DevicesFound = "No camera or device found...";

            this.NullifyDevice();
            Schedule.OnUiThread(
                CameraManager.FastCameraMonitoringTime_ms / 2, 
                () => { this.DevicesFound = string.Empty; }, 
                DispatcherPriority.ApplicationIdle);
        }
        else
        {
            // TODO: localize
            string detected = "Detected: ";
            foreach (var device in message.Devices)
            {
                Debug.WriteLine(" Detected: " + device.FriendlyName + "  " + device.Description);
                detected = detected + device.Description + "  " ;  
            }

            this.DevicesFound = detected;
        }
    }

    private void OnDeviceStatus(DeviceStatusMessage message)
    {
        // TODO: localize
        this.DeviceStatus = 
            message.Device.Description + ": " + 
            (message.IsConnected ? "Connected" : "Not responding.");
        this.FileCount = string.Empty;
        this.FileDownloaded = string.Empty;
    }

    private void OnDeviceFileList(DeviceFileListMessage message)
    {
        // TODO: localize
        if (message.Files.Count == 0)
        {
            this.FileCount = message.Device.Description + ": No files on device.";
            this.FileDownloaded = string.Empty;
            this.selectedFiles.Clear() ;
            this.NullifyDevice();
        }
        else
        {
            this.foundDevice = message.Device;
            this.FileCount =
                message.Device.Description + ": " + message.Files.Count + " files ready to download.";
            this.selectedFiles.Clear();
            this.selectedFiles.AddRange(message.Files);
            this.downloadedFiles.Clear();
        }
    }

    private void OnDeviceFileDownloaded(DeviceFileDownloadedMessage message)
    {
        if (message.IsSuccess)
        {
            this.downloadedFiles.Add(message.File);
            this.FileDownloaded =
                message.Device.FriendlyName + ":  " + message.File + "  downloaded to: " + message.Path;
            if (message.ThumbnailBytes is not null)
            {
                var thumbnail = new CameraThumbnailViewModel(this, message.ThumbnailBytes);
                this.ThumbnailsPanelViewModel.Thumbnails.Add(thumbnail); 
            } 
        } 
        else
        {
            this.FileError =
                message.Device.FriendlyName + ":  " + message.File + "  download error.";
        }
    }

    [RelayCommand]
    public void OnDownload()
    {
        if ( this.foundDevice is null || this.selectedFiles.Count == 0)
        {
            return;
        }

        this.cameraMgr.BeginDownloadingFiles(this.foundDevice, this.selectedFiles);
    }

    public void OnSelect(object selectedObject) { } 
}
