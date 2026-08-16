namespace Lyt.PhotoPostPro.Model.Camera;

public sealed class FoundDevice(string id, string friendlyName, string manufacturer, string description)
{
    public void Update (string friendlyName, string manufacturer, string description)
    {
        this.FriendlyName = friendlyName;
        this.Manufacturer = manufacturer;
        this.Description = description;
    }

    public string Id { get; private set; } = id;

    public string FriendlyName { get; private set; } = friendlyName;

    public string Manufacturer { get; private set; } = manufacturer;

    public string Description { get; private set; } = description;
}
