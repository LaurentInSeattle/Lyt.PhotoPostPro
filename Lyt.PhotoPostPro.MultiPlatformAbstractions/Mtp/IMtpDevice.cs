namespace Lyt.PhotoPostPro.MultiPlatformAbstractions.Mtp;

public interface IMtpDevice
{
    void Update(string friendlyName, string manufacturer, string description); 

    string Id { get;  } 

    string FriendlyName { get; }

    string Manufacturer { get; }

    string Description { get; }

    void Connect();

    bool IsConnected { get; }

    void Disconnect();

    string[] GetFiles(string path, string searchPattern, SearchOption searchOption);

    void DeleteFile(string path);

    bool FileExists(string path);

    void DownloadFile(string path, MemoryStream memoryStream);
}
