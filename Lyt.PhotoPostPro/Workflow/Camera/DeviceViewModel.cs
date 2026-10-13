namespace Lyt.PhotoPostPro.Workflow.Camera;

public sealed partial class DeviceViewModel(CameraViewModel parent, FoundDevice device) : ViewModel<DeviceView>
{
    private readonly CameraViewModel parent = parent;
    private readonly FoundDevice device = device;

    [ObservableProperty]
    public partial string FriendlyName { get; set; } = device.FriendlyName;

    [ObservableProperty]
    public partial string Manufacturer { get; set; } = device.Manufacturer;

    [ObservableProperty]
    public partial bool IsDeviceSelected { get; set; } = false;

    public FoundDevice Device => this.device;

    partial void OnIsDeviceSelectedChanged(bool value)
        => this.parent.OnDeviceSelectionChanged(this, selected: value);
}
