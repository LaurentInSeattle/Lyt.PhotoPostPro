namespace Lyt.TestCamera;

using System.Diagnostics;
using WPDlight;

internal class Program
{
    static void Main(string[] args)
    {
        try
        {
            string[] deviceIds = PortableDeviceManager.EnumerateDevices(true);
            if (deviceIds.Length == 0)
            {
                Console.WriteLine("No devices found");
            }
            else
            {
                foreach (string deviceId in deviceIds)
                {
                    // Basic device info from manager (not strictly required here but kept for potential use)
                    string deviceFriendlyName = PortableDeviceManager.GetDeviceFriendlyName(deviceId);
                    string deviceManufacturer = PortableDeviceManager.GetDeviceManufacturer(deviceId);
                    string deviceDescription = PortableDeviceManager.GetDeviceDescription(deviceId);

                    using (PortableDevice device = PortableDevice.Open(deviceId))
                    {
                        try
                        {
                            PrintDeviceInfo(device);
                            PrintTopLevelEntries(device);
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Error while inspecting device {deviceId}: {ex.Message}");
                            Console.ResetColor();
                        }
                    }
                }
            } 
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error while reading device data: {ex.Message}");
            Console.ResetColor();
        }


        Console.Write("Press Any Key To Exit...");
        Console.ReadKey();
    }

    private static void PrintDeviceInfo(PortableDevice device)
    {
        Console.WriteLine(new string('=', 60));

        void PrintLabelValue(string label, string value)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(label.PadRight(22));
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(value);
            Console.ResetColor();
        }

        PrintLabelValue("Name:", device.FriendlyName);
        PrintLabelValue("Id:", device.Id);
        PrintLabelValue("Manufacturer:", device.Manufacturer);
        PrintLabelValue("Model:", device.Model);
        PrintLabelValue("Serial Number:", string.IsNullOrEmpty(device.SerialNumber) ? "(none)" : device.SerialNumber);
        PrintLabelValue("Firmware:", string.IsNullOrEmpty(device.FirmwareVersion) ? "(unknown)" : device.FirmwareVersion);
        PrintLabelValue("Type:", device.Type.ToString());
        PrintLabelValue("Protocol:", device.Protocol);
        PrintLabelValue("Transport:", device.Transport.ToString());
        PrintLabelValue("Power:", $"{device.PowerLevel} ({device.PowerSource})");
        PrintLabelValue("Non-consumable:", device.SupportsNonConsumable.HasValue ? device.SupportsNonConsumable.Value.ToString() : "Unknown");

        Console.WriteLine();
    }

    private static void PrintTopLevelEntries(PortableDevice device)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Top-level entries:");
        Console.ResetColor();

        foreach (string entry in device.EnumerateFileSystemEntries(Path.DirectorySeparatorChar.ToString(), "*", System.IO.SearchOption.TopDirectoryOnly))
        {
            Debug.Assert(device.DirectoryExists(entry));

            Console.WriteLine("  " + entry);
        }

        Console.WriteLine();
    }
}
