namespace Lyt.PhotoPostPro.Model.CameraModels;

public sealed record class ConnectedDevice(
    string Id,
    string FriendlyName,
    string Manufacturer,
    string Description);

/*
        PrintLabelValue("Model:", device.Model);
        PrintLabelValue("Serial Number:", string.IsNullOrEmpty(device.SerialNumber) ? "(none)" : device.SerialNumber);
        PrintLabelValue("Firmware:", string.IsNullOrEmpty(device.FirmwareVersion) ? "(unknown)" : device.FirmwareVersion);
        PrintLabelValue("Type:", device.Type.ToString());
        PrintLabelValue("Protocol:", device.Protocol);
        PrintLabelValue("Transport:", device.Transport.ToString());
        // Not supported on iPhone 
        // PrintLabelValue("Power:", $"{device.PowerLevel} ({device.PowerSource})");
        // Not supported on iPhone 
        // PrintLabelValue("Non-consumable:", device.SupportsNonConsumable.HasValue ? device.SupportsNonConsumable.Value.ToString() : "Unknown");
 */