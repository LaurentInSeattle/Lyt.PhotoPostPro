namespace Lyt.PhotoPostPro.Workflow.Camera;

public sealed partial class CameraViewModel :
    ViewModel<CameraView>, IRecipient<DevicesFoundMessage>
{
    private readonly PhotoPostProModel model;
    private readonly CameraManager cameraMgr;

    public CameraViewModel(PhotoPostProModel photoPostProModel)
    {
        this.model = photoPostProModel;
        this.cameraMgr = this.model.CameraManager;
        this.Subscribe<DevicesFoundMessage>();
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
        this.cameraMgr.BeginMonitoringCameraConnexion();
    }

    public override void Deactivate()
    {
        base.Deactivate();
        this.cameraMgr.EndMonitoringCameraConnexion();
    }

    public void Receive(DevicesFoundMessage message)
        => Dispatch.OnUiThread(() => { this.OnDevicesConnected(message); }, DispatcherPriority.Background);

    private void OnDevicesConnected(DevicesFoundMessage message)
    {
        var list = message.Devices; 
        if ( list.Count == 0)
        {
            Debug.WriteLine(" No Camera Connected: ");
        }
        else
        {
            foreach (var device in message.Devices)
            {
                Debug.WriteLine(" Connected: " + device.FriendlyName + "  " + device.Description);
            }
        }
    }
}
