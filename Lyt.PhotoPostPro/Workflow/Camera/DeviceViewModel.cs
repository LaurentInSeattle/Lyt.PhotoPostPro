namespace Lyt.PhotoPostPro.Workflow.Camera;

public sealed partial class DeviceViewModel : ViewModel<DeviceView>
{
    private readonly CameraViewModel parent;
    private readonly FoundDevice device;

    [ObservableProperty]
    public partial string FriendlyName { get; set; }

    [ObservableProperty]
    public partial string Manufacturer { get; set; } 

    [ObservableProperty]
    public partial bool IsDeviceSelected { get; set; } = false;

    public FoundDevice Device => this.device;

    public DeviceViewModel(CameraViewModel parent, FoundDevice device)
    {
        this.parent = parent;
        this.device = device;
        this.FriendlyName = device.FriendlyName;
        this.Manufacturer = device.Manufacturer;
    }

    partial void OnIsDeviceSelectedChanged(bool value)
        => this.parent.OnDeviceSelectionChanged(this, selected: value);
}
