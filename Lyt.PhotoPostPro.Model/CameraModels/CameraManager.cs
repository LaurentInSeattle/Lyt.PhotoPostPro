namespace Lyt.PhotoPostPro.Model.CameraModels;

public class CameraManager
{
    private const int FastCameraMonitoringTime_ms = 2_500;
    private const int SlowCameraMonitoringTime_ms = 5_000;

    private CancellationTokenSource? cts;
    private bool isMonitoring;
    private OpenedCamera? lastConnectedCamera;

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
            string[] deviceIds = PortableDeviceManager.EnumerateDevices(true);
            if (deviceIds.Length == 0)
            {
                Debug.WriteLine(" No devices found");
                // Publish an empty list 
                new DevicesConnectedMessage([]).Publish();
            }
            else
            {
                List<ConnectedDevice> connectedDevices = new(deviceIds.Length);
                foreach (string deviceId in deviceIds)
                {
                    // Basic device info from manager (not strictly required here but kept for potential use)
                    string deviceFriendlyName = PortableDeviceManager.GetDeviceFriendlyName(deviceId);
                    string deviceManufacturer = PortableDeviceManager.GetDeviceManufacturer(deviceId);
                    string deviceDescription = PortableDeviceManager.GetDeviceDescription(deviceId);
                    var connectedDevice =
                        new ConnectedDevice(deviceId, deviceFriendlyName, deviceManufacturer, deviceDescription);
                    connectedDevices.Add(connectedDevice);
                }

                new DevicesConnectedMessage(connectedDevices).Publish();

                if (deviceIds.Length == 1)
                {
                    // Single device: try to open it 
                    string deviceId = deviceIds[0];
                    try
                    {
                        using var device = PortableDevice.Open(deviceId);
                        //PrintDeviceInfo(device);
                        //var files = ShowAllFiles(device);
                        //foreach (string file in files)
                        //{
                        //    await DownloadFile(device, file);
                        //}
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($" Error while inspecting device {deviceId}: {ex.Message}");
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

    public bool OpenDevice(string deviceId)
    {
        return true;
    }

    public List<string> Files(string deviceId)
    {
        List<string> files = [];
        return files;
    }
}
