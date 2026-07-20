namespace Lyt.PhotoPostPro.Model.CameraModels;

public sealed class FoundDevice
{
    public FoundDevice( string id, string friendlyName, string manufacturer, string description)
    {
        this.Id = id;
        this.FriendlyName = friendlyName;
        this.Manufacturer = manufacturer;
        this.Description = description;
    }

    public void Update (string friendlyName, string manufacturer, string description)
    {
        this.FriendlyName = friendlyName;
        this.Manufacturer = manufacturer;
        this.Description = description;
    }

    public string Id { get; private set;  }

    public string FriendlyName { get; private set;  }

    public string Manufacturer { get; private set;  }

    public string Description { get; private set;  }
}
