namespace Lyt.PhotoPostPro.Model.CameraModels;

#pragma warning disable CA1416 // Windows ONLY ! 

// Here to block both Path from Avalonia ans ImageSharp 
using System.IO;

/*
 * 
MartinKuschnik left a comment (MartinKuschnik/WPDlight#8)

One more thing worth knowing now that you've switched to MediaDevices, since it's a common trip-up:

MediaDevices caches the device handle.

If your app connects to a device, and that device gets unplugged and then plugged back in without restarting the app, 
MediaDevices will keep using the old (now stale) handle instead of picking up the new connection. 
Since the underlying device instance no longer exists, this doesn't work and can cause errors or unexpected behavior.

Whether this matters to you depends on your use case:

If your app always connects fresh on a full restart, you're fine.
If your app is meant to stay running and handle devices being unplugged/replugged, you'll want to detect the 
disconnect and re-establish the connection explicitly, rather than assuming the existing handle still works.

Just flagging it so it doesn't surprise you later.

*/

public class CameraManager
{
    public const int UiResponseDelayTime_ms = 66; // About one frame
    public const int ReQueryDelayTime_ms = 1_000;
    public const int FastCameraMonitoringTime_ms = 2_500;
    public const int SlowCameraMonitoringTime_ms = 5_000;

    private readonly string downloadFolderPath;

    private CancellationTokenSource? ctsMonitoring;
    private CancellationTokenSource? ctsDownloading;

    public CameraManager()
    {
        string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        this.downloadFolderPath = Path.Combine(pictures, PhotoPostProModel.PhotoPostProAppName, "CameraDownloads");
        this.ClearDownloadFolder();
    }

    public bool IsMonitoring { get; private set; }

    public bool IsDownloading { get; private set; }

    public bool IsDeleting { get; private set; }

    public void ClearDownloadFolder()
    {
        try
        {
            // Dont care about permissions or any attributes such as creation date
            // There should be no read-only or delete protected or system files in there 
            // so we try to nuke everything 
            if (Directory.Exists(this.downloadFolderPath))
            {
                Directory.Delete(this.downloadFolderPath, true); // Deletes directory and all contents
                Directory.CreateDirectory(this.downloadFolderPath); // Recreates the empty root directory
            }
        }
        catch (Exception ex)
        {
            // But... Shit 
            // Most possible case is that user is editing / locking one downloaded file 
            Debug.WriteLine(ex);

            // Delete as much stuff as we can 
            foreach (string filePath in Directory.GetFiles(this.downloadFolderPath))
            {
                // Reset attributes in case a file is marked as Read-Only
                File.SetAttributes(filePath, FileAttributes.Normal);
                File.Delete(filePath);
            }

            // Delete all subdirectories recursively
            foreach (string subdirectory in Directory.GetDirectories(this.downloadFolderPath))
            {
                Directory.Delete(subdirectory, recursive: true);
            }
        }

        try
        {
            if (!Directory.Exists(this.downloadFolderPath))
            {
                // Creates the empty root directory
                Directory.CreateDirectory(this.downloadFolderPath);
            }
        }
        catch (Exception ex)
        {
            // But... Shit 
            Debug.WriteLine(ex);

            // We are in potential trouble now 
        }
    }

    #region Connection 

    public void BeginMonitoringCameraConnexion()
    {
        // Force re-invocation of the Media Device CTOR to prevent potential caching issues 
        typeof(MediaDevice).TypeInitializer?.Invoke(null, null);

        this.ctsMonitoring = new CancellationTokenSource();
        Task.Run(async () => { this.MonitorCameraConnexion(this.ctsMonitoring.Token); });
    }

    public void EndMonitoringCameraConnexion()
    {
        this.ctsMonitoring?.Cancel();
        this.IsMonitoring = false;
    }

    private void TryConnectTo(FoundDevice foundDevice, MediaDevice device)
    {
        try
        {
            device.Connect();
            if (device.IsConnected)
            {
                foundDevice.Update(device.FriendlyName, device.Manufacturer, device.Description); 
                new DeviceStatusMessage(IsConnected: true, foundDevice).Publish();
                DebugPrintDeviceInfo(device);
                var files = AllFiles(device);
                Debug.WriteLine("Found files: " + files.Count);
                new DeviceFileListMessage(foundDevice, files).Publish();
                this.IsMonitoring = false;
            }
            else
            {
                new DeviceStatusMessage(IsConnected: false, foundDevice).Publish();
            }
        }
        catch (Exception ex)
        {
            new DeviceStatusMessage(IsConnected: false, foundDevice).Publish();
            Debug.WriteLine($" Error while inspecting device {device.FriendlyName}: {ex.Message}");
        }
    }

    private async void MonitorCameraConnexion(CancellationToken token)
    {
        try
        {
            this.IsMonitoring = true;
            while (!token.IsCancellationRequested && this.IsMonitoring)
            {
                this.CheckConnection();
                await Task.Delay(FastCameraMonitoringTime_ms, token);
            }
        }
        catch (TaskCanceledException tce)
        {
            Debug.WriteLine($" Task Canceled Exception : {tce.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($" Error while monitoring devices: {ex.Message}");
        }
    }

    private void CheckConnection()
    {
        Debug.WriteLine(" Checking Camera Connection");
        try
        {
            var devices = MediaDevice.GetDevices().ToList();
            int deviceCount = devices.Count;
            if (deviceCount == 0)
            {
                // Publish an empty list 
                new DevicesFoundMessage([]).Publish();
                Debug.WriteLine(" No devices found");
            }
            else
            {
                List<FoundDevice> foundDevices = new(deviceCount);
                foreach (MediaDevice device in devices)
                {
                    // Basic device info 
                    var connectedDevice =
                        new FoundDevice(device.DeviceId, device.FriendlyName, device.Manufacturer, device.Description);
                    foundDevices.Add(connectedDevice);
                }

                new DevicesFoundMessage(foundDevices).Publish();

                if (deviceCount == 1)
                {
                    // Single device: try to connect to it  
                    FoundDevice foundDevice = foundDevices[0];
                    MediaDevice device = devices[0];
                    Debug.WriteLine("One device found: " + device.Description);
                    this.TryConnectTo(foundDevice, device);
                }
                else //  (deviceIds.Length > 1)
                {
                    this.IsMonitoring = false;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($" Error while reading device data: {ex.Message}");
        }
    }

    public void DisposeDevice(FoundDevice foundDevice)
    {
        var devices = MediaDevice.GetDevices().ToList();
        MediaDevice? device =
            (from dev in devices where dev.DeviceId == foundDevice.Id select dev)
            .FirstOrDefault();
        if (device is not null)
        {
            device.Dispose();
            return;
        }
    }

    public void ConnectTo(FoundDevice foundDevice)
    {
        var devices = MediaDevice.GetDevices().ToList();
        MediaDevice? device =
            (from dev in devices where dev.DeviceId == foundDevice.Id select dev)
            .FirstOrDefault();
        if (device is null)
        {
            new DeviceStatusMessage(IsConnected: false, foundDevice).Publish();
            return;
        }

        this.TryConnectTo(foundDevice, device);
    }

    private static List<string> AllFiles(MediaDevice device)
    {
        HashSet<string> allFiles = [];

        int retries = 3;
        while (retries > 0)
        {
            allFiles.Clear();
            string[] files =
                device.GetFiles(Path.DirectorySeparatorChar.ToString(), "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                Debug.WriteLine("      File: " + file);
                allFiles.Add(file);
            }

            if (allFiles.Count > 0)
            {
                break;
            }

            --retries;
            Task.Delay(ReQueryDelayTime_ms).Wait();
        }

        return allFiles.ToList();
    }

    #endregion Connection 

    #region Deleting Files 

    public void BeginDeletingFiles(FoundDevice foundDevice, List<string> selectedFiles)
    {
        this.ctsDownloading = new CancellationTokenSource();
        Task.Run(async () =>
        {
            this.DeleteFiles(foundDevice, selectedFiles, this.ctsDownloading.Token);
        });
    }

    public void EndDeletingFiles()
    {
        this.ctsDownloading?.Cancel();
        this.IsDeleting = false;
    }

    public async void DeleteFiles(FoundDevice foundDevice, List<string> toDelete, CancellationToken token)
    {
        bool completed = false;
        int errors = 0;
        int deleted = 0;
        try
        {
            var devices = MediaDevice.GetDevices().ToList();
            MediaDevice? device =
                (from dev in devices where dev.DeviceId == foundDevice.Id select dev)
                .FirstOrDefault();
            if (device is null)
            {
                new DeviceStatusMessage(IsConnected: false, foundDevice).Publish();
                return;
            }

            if (!device.IsConnected)
            {
                this.TryConnectTo(foundDevice, device);
            }

            if (!device.IsConnected)
            {
                // Still not connected 
                new DeviceStatusMessage(IsConnected: false, foundDevice).Publish();
                return;
            }

            this.IsDeleting = true;
            foreach (string file in toDelete)
            {
                if (token.IsCancellationRequested || !this.IsDeleting)
                {
                    break;
                }

                if (!this.DeleteFile(foundDevice, device, file))
                {
                    ++errors;
                    Debug.WriteLine("Delete error");
                }
                else
                {
                    Debug.WriteLine("Deleted: " + file);
                    ++deleted;
                }

                await Task.Delay(UiResponseDelayTime_ms, token);
            }

            completed = true;
        }
        catch (TaskCanceledException tce)
        {
            Debug.WriteLine($" Task Canceled Exception : {tce.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($" Error while monitoring devices: {ex.Message}");
            new DeviceStatusMessage(IsConnected: false, foundDevice).Publish();
        }
        finally
        {
            Debug.WriteLine("Deleted: Files " + deleted + "  Errors: " + errors );
            new DeviceDeleteCompleteMessage(foundDevice, completed, toDelete.Count, deleted, errors).Publish();
        }
    }

    private bool DeleteFile(FoundDevice foundDevice, MediaDevice device, string file)
    {
        try
        {
            device.DeleteFile(file);
            new DeviceFileDeletedMessage(IsSuccess: true, foundDevice, file, "").Publish();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Exception thrown: " + ex);
            new DeviceFileDeletedMessage(IsSuccess: false, foundDevice, file, "Exception thrown: " + ex).Publish();
            return false;
        }
    }

    #endregion Deleting Files 

    #region Downloading Files 

    public void BeginDownloadingFiles(FoundDevice foundDevice, List<string> selectedFiles)
    {
        this.ctsDownloading = new CancellationTokenSource();
        Task.Run(async () =>
        {
            this.DownloadFiles(foundDevice, selectedFiles, this.ctsDownloading.Token);
        });
    }

    public void EndDownloadingFiles()
    {
        this.ctsDownloading?.Cancel();
        this.IsDownloading = false;
    }

    private async void DownloadFiles(FoundDevice foundDevice, List<string> selectedFiles, CancellationToken token)
    {
        bool completed = false;
        int errors = 0;
        int downloads = 0;
        try
        {
            var devices = MediaDevice.GetDevices().ToList();
            MediaDevice? device =
                (from dev in devices where dev.DeviceId == foundDevice.Id select dev)
                .FirstOrDefault();
            if (device is null)
            {
                new DeviceStatusMessage(IsConnected: false, foundDevice).Publish();
                return;
            }

            if (!device.IsConnected)
            {
                this.TryConnectTo(foundDevice, device);
            }

            if (!device.IsConnected)
            {
                // Still not connected 
                new DeviceStatusMessage(IsConnected: false, foundDevice).Publish();
                return;
            }

            this.IsDownloading = true;
            foreach (string file in selectedFiles)
            {
                if (token.IsCancellationRequested || !this.IsDownloading)
                {
                    break;
                }

                if (!this.DownloadFile(foundDevice, device, file))
                {
                    ++errors;
                    Debug.WriteLine("Download error");
                }
                else
                {
                    ++downloads;
                }

                await Task.Delay(UiResponseDelayTime_ms, token);
            }

            completed = true;
        }
        catch (TaskCanceledException tce)
        {
            Debug.WriteLine($" Task Canceled Exception : {tce.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($" Error while monitoring devices: {ex.Message}");
            new DeviceStatusMessage(IsConnected: false, foundDevice).Publish();
        }
        finally
        {
            new DeviceDownloadCompleteMessage(foundDevice, completed, selectedFiles.Count, downloads, errors).Publish();
        }
    }

    private bool DownloadFile(FoundDevice foundDevice, MediaDevice device, string file)
    {
        try
        {
            MemoryStream memoryStream = new();
            device.DownloadFile(file, memoryStream);
            string[] fileTokens = file.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            string fileName = fileTokens[^1];
            string targetPath = Path.Combine(this.downloadFolderPath, fileName);
            byte[] bytes = memoryStream.ToArray();
            int length = bytes.Length;
            File.WriteAllBytes(targetPath, bytes);
            if (File.Exists(targetPath))
            {
                FileInfo fi = new(targetPath);
                if (fi.Length != length)
                {
                    new DeviceFileDownloadedMessage(
                        IsSuccess: false, IsDownloaded: false, foundDevice, file, "No exception").Publish();
                    return false;
                }
            }

            LoadedImage loadedImage = ImageLoader.PreLoadImage(targetPath);
            if (loadedImage.IsSuccess && loadedImage.IsPreLoaded)
            {
                // ! Verified by loadedImage.IsPreLoaded
                loadedImage.Metadata!.CameraFullPath = file;
                string trimmed = Path.GetFileNameWithoutExtension(fileName);
                string thumbnailName = trimmed + "_THUMB.jpg";
                string thumbnailPath = Path.Combine(this.downloadFolderPath, thumbnailName);

                // ! Verified by loadedImage.IsPreLoaded
                byte[] thumbnailBytes = loadedImage.JpgThumbnail!;
                File.WriteAllBytes(thumbnailPath, thumbnailBytes);
                new DeviceFileDownloadedMessage(
                    IsSuccess: true, IsDownloaded: true,
                    foundDevice, file, targetPath,
                    loadedImage.Metadata,
                    thumbnailBytes, thumbnailPath).Publish();
                return true;
            }

            new DeviceFileDownloadedMessage(
                IsSuccess: false, IsDownloaded: true, 
                foundDevice, file, targetPath).Publish();
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Exception thrown: " + ex);
            new DeviceFileDownloadedMessage(
                IsSuccess: false, IsDownloaded: false, foundDevice, file, "Exception thrown: " + ex).Publish();
            return false;
        }
    }

    #endregion Downloading Files 

    [Conditional("DEBUG")]
    private static void DebugPrintDeviceInfo(MediaDevice device)
    {
        Debug.WriteLine(new string('=', 60));

        static void PrintLabelValue(string label, string value)
        {
            Debug.Write(label.PadRight(22));
            Debug.WriteLine(value);
        }

        PrintLabelValue("Id:", device.DeviceId);
        PrintLabelValue("Friendly Name:", device.FriendlyName);
        PrintLabelValue("Manufacturer:", device.Manufacturer);
        PrintLabelValue("Model:", device.Model);
        PrintLabelValue("Serial Number:", string.IsNullOrEmpty(device.SerialNumber) ? "(none)" : device.SerialNumber);
        PrintLabelValue("Firmware:", string.IsNullOrEmpty(device.FirmwareVersion) ? "(unknown)" : device.FirmwareVersion);
        PrintLabelValue("Type:", device.DeviceType.ToString());
        PrintLabelValue("Protocol:", device.Protocol);
        PrintLabelValue("Transport:", device.Transport.ToString());
        PrintLabelValue("Power:", $"{device.PowerLevel} ({device.PowerSource})");

        Debug.WriteLine("");
    }
}
