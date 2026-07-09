namespace Lyt.TestCamera;

using System.Diagnostics;
using WPDlight;

// ==> WPD Light : https://github.com/MartinKuschnik/WPDlight
 
internal class Program
{
    static async Task Main(string[] args)
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
                            var files  = ShowAllFiles(device);
                            foreach (string file in files)
                            {
                                await DownloadFile(device, file);
                            }
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

    private async static Task DownloadFile (PortableDevice device, string file )
    {
        try
        {
            MemoryStream memoryStream = new();
            await device.DownloadFileAsync(file, memoryStream);
            string[] fileTokens = file.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            string fileName = fileTokens[^1];
            string target = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
            File.WriteAllBytes(target, memoryStream.ToArray());
        } 
        catch (Exception ex)
        {
            Console.Write("Exception thrown: " + ex);
        }
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
        // Not supported on iPhone 
        // PrintLabelValue("Power:", $"{device.PowerLevel} ({device.PowerSource})");
        // Not supported on iPhone 
        // PrintLabelValue("Non-consumable:", device.SupportsNonConsumable.HasValue ? device.SupportsNonConsumable.Value.ToString() : "Unknown");

        Console.WriteLine();
    }

    private static List<string> ShowAllFiles(PortableDevice device)
    {
        List<string> allFiles = [] ; 

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Top-level entries:");
        Console.ResetColor();

        void ShowFiles (string currentDir)
        {
            var files = device.EnumerateFiles(currentDir, "*", System.IO.SearchOption.TopDirectoryOnly).ToList();
            foreach (string file in files)
            {
                Console.WriteLine("  " + file);
                allFiles.Add(file);
            }
        }

        foreach (string entry in device.EnumerateFileSystemEntries(Path.DirectorySeparatorChar.ToString(), "*", System.IO.SearchOption.TopDirectoryOnly))
        {
            Debug.Assert(device.DirectoryExists(entry));

            Console.WriteLine("  " + entry);
            ShowFiles(entry);

            var directories = device.EnumerateDirectories(entry, "*", System.IO.SearchOption.AllDirectories).ToList();

            foreach (string directory in directories )
            {
                Console.WriteLine("  " + directory);
                ShowFiles(directory);
            }

        }

        Console.WriteLine();
        return allFiles; 
    }
}
