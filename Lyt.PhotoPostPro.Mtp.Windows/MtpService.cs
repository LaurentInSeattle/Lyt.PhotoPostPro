namespace Lyt.PhotoPostPro.Mtp.Windows;

public sealed class MtpService : IMtpService
{
#pragma warning disable CA1822 // Mark members as static

    public List<IMtpDevice> GetDevices()
    {
        var devices = MediaDevice.GetDevices();
        var list = new List<IMtpDevice>(8);
        foreach (MediaDevice device in devices)
        {
            list.Add(new MtpDevice(device));
        }

        return list;
    }

#pragma warning restore CA1822
}
