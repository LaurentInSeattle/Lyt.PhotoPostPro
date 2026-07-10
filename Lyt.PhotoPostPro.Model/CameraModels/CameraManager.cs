namespace Lyt.PhotoPostPro.Model.CameraModels;

#pragma warning disable CA1416 // Windows ONLY ! 

// Here to block both Path from Avalonia ans ImageSharp 
using System.IO;

public class CameraManager
{
    public const int UiResponseDelayTime_ms = 180;
    public const int FastCameraMonitoringTime_ms = 2_500;
    public const int SlowCameraMonitoringTime_ms = 5_000;

    private readonly string downloadFolderPath;

    private CancellationTokenSource? ctsMonitoring;
    private CancellationTokenSource? ctsDownloading;

    public CameraManager()
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        this.downloadFolderPath = Path.Combine(desktop, "CameraDownloads");
        if (!Directory.Exists(downloadFolderPath))
        {
            Directory.CreateDirectory(downloadFolderPath);
        }
    }

    public bool IsMonitoring { get; private set; }

    public bool IsDownloading { get; private set; }

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
                Debug.WriteLine(" No devices found");
                // Publish an empty list 
                new DevicesFoundMessage([]).Publish();
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
                    IsMonitoring = false;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($" Error while reading device data: {ex.Message}");
        }
    }

    public void DisposeDevice (FoundDevice foundDevice)
    {
        var devices = MediaDevice.GetDevices().ToList();
        MediaDevice? device =
            (from dev in devices where dev.DeviceId == foundDevice.Id select dev)
            .FirstOrDefault();
        if (device is not null)
        {
            device.Dispose() ;
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

            this.IsDownloading= true;
            foreach (string file in selectedFiles) 
            {
                if (token.IsCancellationRequested || ! this.IsDownloading)
                {
                    break; 
                }

                if (!this.DownloadFile(foundDevice, device, file))
                {
                    Debug.WriteLine("Download error");
                }

                await Task.Delay(UiResponseDelayTime_ms, token);
            }
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
    }

    private void TryConnectTo(FoundDevice foundDevice, MediaDevice device)
    {
        try
        {
            device.Connect();
            if (device.IsConnected)
            {
                new DeviceStatusMessage(IsConnected: true, foundDevice).Publish();
                DebugPrintDeviceInfo(device);
                var files = AllFiles(device);
                Debug.WriteLine("Found files: " + files.Count);
                new DeviceFileListMessage(foundDevice, files).Publish();
                IsMonitoring = false;
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
                if (fi.Length == length)
                {
                    new DeviceFileDownloadedMessage(IsSuccess: true, foundDevice, file, targetPath).Publish();
                    return true;
                }
            }

            new DeviceFileDownloadedMessage(IsSuccess: false, foundDevice, file, "No exception").Publish();
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Exception thrown: " + ex);
            new DeviceFileDownloadedMessage(IsSuccess: false, foundDevice, file, "Exception thrown: " + ex).Publish();
            return false;
        }
    }

    private static List<string> AllFiles(MediaDevice device)
    {
        HashSet<string> allFiles = [];

        string[] files =
            device.GetFiles(Path.DirectorySeparatorChar.ToString(), "*", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            Debug.WriteLine("      File: " + file);
            allFiles.Add(file);
        }

        return allFiles.ToList();
    }

    [Conditional("DEBUG")]
    private static void DebugPrintDeviceInfo(MediaDevice device)
    {
        Debug.WriteLine(new string('=', 60));

        void PrintLabelValue(string label, string value)
        {
            Debug.Write(label.PadRight(22));
            Debug.WriteLine(value);
        }

        PrintLabelValue("Id:", device.DeviceId);
        PrintLabelValue("Name:", device.FriendlyName);
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
