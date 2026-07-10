namespace Lyt.PhotoPostPro.Model.Messaging;

public sealed record class DevicesConnectedMessage(List<ConnectedDevice> Devices);

public sealed record class DeviceFileListMessage(HashSet<string> Files);
