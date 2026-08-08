namespace Lyt.PhotoPostPro.MultiPlatformAbstractions.Mtp;

public interface IMtpService
{
    List<IMtpDevice> GetDevices();
}
