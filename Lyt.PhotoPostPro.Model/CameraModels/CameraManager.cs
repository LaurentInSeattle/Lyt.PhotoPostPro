namespace Lyt.PhotoPostPro.Model.CameraModels;

#pragma warning disable CA1416 // Windows ONLY ! 

// Here to block both Path from Avalonia ans ImageSharp 
using System.IO;

public class CameraManager
{
    private const int FastCameraMonitoringTime_ms = 2_500;
    private const int SlowCameraMonitoringTime_ms = 5_000;

    private string downloadFolderPath;

    private CancellationTokenSource? cts;

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

    public void BeginMonitoringCameraConnexion()
    {
        this.cts = new CancellationTokenSource();
        Task.Run(async () => { this.MonitorCameraConnexion(this.cts.Token); });
    }

    public void EndMonitoringCameraConnexion()
    {
        this.cts?.Cancel();
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

    public void DownloadFiles(FoundDevice foundDevice, List<string> selectedFiles)
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

        foreach (string file in selectedFiles)
        {
            if (!DownloadFile(foundDevice, device, file))
            {
                new DeviceStatusMessage(IsConnected: false, foundDevice).Publish();
                break;
            }
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
                    new DeviceFileDownloadedMessage(foundDevice, file, targetPath).Publish();
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Exception thrown: " + ex);
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
