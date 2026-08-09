namespace Lyt.PhotoPostPro.MultiPlatformAbstractions.Mtp;

public interface IMtpService
{
    void Initialize(); 

    List<IMtpDevice> GetDevices();
}
