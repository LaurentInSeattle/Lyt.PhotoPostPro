namespace Lyt.PhotoPostPro.Mtp.Windows;

public sealed class MtpDevice : IMtpDevice
{
    private readonly MediaDevice mediaDevice; 

    public MtpDevice(MediaDevice mediaDevice) 
    {
        this.mediaDevice = mediaDevice;
        this.Id = this.mediaDevice.DeviceId; 
        this.FriendlyName = this.mediaDevice.FriendlyName;
        this.Manufacturer = this.mediaDevice.Manufacturer;
        this.Description = this.mediaDevice.Description;
    }

    public string Id { get; private set; }

    public string FriendlyName { get; private set; }

    public string Manufacturer { get; private set; }

    public string Description { get; private set; }

    public void Update(string friendlyName, string manufacturer, string description)
    {
        this.FriendlyName = friendlyName;
        this.Manufacturer = manufacturer;
        this.Description = description;
    }

    public void Connect() => this.mediaDevice.Connect();

    public bool IsConnected => this.mediaDevice.IsConnected;

    public void Disconnect() => this.mediaDevice.Disconnect();

    public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
        => this.mediaDevice.GetFiles(path, searchPattern, searchOption);

    public void DeleteFile(string path) => this.mediaDevice.DeleteFile(path);

    public bool FileExists(string path) => !this.mediaDevice.FileExists(path);

    public void DownloadFile(string path, MemoryStream memoryStream) 
        => this.mediaDevice.DownloadFile(path, memoryStream);
}
