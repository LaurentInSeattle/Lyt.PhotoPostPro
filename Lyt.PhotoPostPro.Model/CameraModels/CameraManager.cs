namespace Lyt.PhotoPostPro.Model.CameraModels;


#pragma warning disable CA1416

// Here to block both Path from Avalonia ans ImageSharp 
using System.IO; 

public class CameraManager
{
    private const int FastCameraMonitoringTime_ms = 2_500;
    private const int SlowCameraMonitoringTime_ms = 5_000;

    private CancellationTokenSource? cts;
    private bool isMonitoring;
    //private OpenedCamera? lastConnectedCamera;

    public bool IsCameraConnected { get; private set; }

    public void BeginMonitoringCameraConnexion()
    {
        this.cts = new CancellationTokenSource();
        Task.Run(async () => { this.MonitorCameraConnexion(this.cts.Token); });
    }

    public void EndMonitoringCameraConnexion()
    {
        this.cts?.Cancel();
        this.isMonitoring = false;
    }

    private async void MonitorCameraConnexion(CancellationToken token)
    {
        try
        {
            this.isMonitoring = true;
            while (!token.IsCancellationRequested && this.isMonitoring)
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
                new DevicesConnectedMessage([]).Publish();
            }
            else
            {
                List<ConnectedDevice> connectedDevices = new(deviceCount);
                foreach (MediaDevice device in devices)
                {
                    // Basic device info 
                    var connectedDevice =
                        new ConnectedDevice(device.DeviceId, device.FriendlyName, device.Manufacturer, device.Description);
                    connectedDevices.Add(connectedDevice);
                }

                new DevicesConnectedMessage(connectedDevices).Publish();

                if (deviceCount == 1)
                {
                    // Single device: try to connect to it  
                    MediaDevice device = devices[0];
                    try
                    {
                        Console.WriteLine("One device found: " + device.Description);
                        device.Connect();
                        if (device.IsConnected)
                        {
                            PrintDeviceInfo(device);
                            var files = AllFiles(device);
                            Debug.WriteLine("Found files: " + files.Count);
                        }

                        //foreach (string file in files)
                        //{
                        //    await DownloadFile(device, file);
                        //}
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($" Error while inspecting device {device.FriendlyName}: {ex.Message}");
                    }
                }
                else //  (deviceIds.Length > 1)
                {

                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($" Error while reading device data: {ex.Message}");
        }
    }

    private static void PrintDeviceInfo(MediaDevice device)
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

    private static bool DownloadFile(MediaDevice device, string file)
    {
        try
        {
            MemoryStream memoryStream = new();
            device.DownloadFile(file, memoryStream);
            string[] fileTokens = file.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            string fileName = fileTokens[^1];
            string target = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
            byte[] bytes = memoryStream.ToArray();
            int length = bytes.Length;
            File.WriteAllBytes(target, bytes);
            if (File.Exists(target))
            {
                FileInfo fi = new(target);
                if (fi.Length == length)
                {
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

    private static HashSet<string> AllFiles(MediaDevice device)
    {
        HashSet<string> allFiles = [];

        string[] files = 
            device.GetFiles(Path.DirectorySeparatorChar.ToString(), "*", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            Debug.WriteLine("      File: " + file);
            allFiles.Add(file);
        }

        return allFiles;
    }
}
