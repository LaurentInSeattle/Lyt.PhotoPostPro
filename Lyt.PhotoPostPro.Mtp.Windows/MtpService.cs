namespace Lyt.PhotoPostPro.Mtp.Windows;

public sealed class MtpService : IMtpService
{
    public List<IMtpDevice> GetDevices()
    {
        // Media Devices API 2.0
        // var devices = MediaDeviceManager.Instance.GetDevices();
        var devices = MediaDevice.GetDevices();
        if ( devices is null || !devices.Any())
        {
            return []; 
        }

        var list = new List<IMtpDevice>(8);
        foreach (MediaDevice device in devices)
        {
            list.Add(new MtpDevice(device));
        }

        return list;
    }
}
