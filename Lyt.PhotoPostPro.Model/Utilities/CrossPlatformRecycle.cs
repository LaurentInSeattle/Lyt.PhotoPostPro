namespace Lyt.PhotoPostPro.Model.Utilities;

using System.Runtime.InteropServices;
using System.IO; 

public static  class CrossPlatformRecycle
{
    public static void SendToRecycleBin(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Target file not found.", filePath);
        }

        string fullPath = Path.GetFullPath(filePath);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows native Recycle Bin handling
            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                fullPath,
                Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin
            );
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            TrashMacOs(fullPath);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            TrashLinux(fullPath);
        }
        else
        {
            // Fallback for unsupported platforms
            File.Delete(fullPath);
        }
    }

    private static void TrashMacOs(string filePath)
    {
        // Escapes the path for AppleScript
        string escapedPath = filePath.Replace("\\", "\\\\").Replace("\"", "\\\"");

        // Uses AppleScript via osascript to tell Finder to move the file to trash
        var psi = new ProcessStartInfo
        {
            FileName = "osascript",
            Arguments = $"-e \"tell application \\\"Finder\\\" to delete POSIX file \\\"{escapedPath}\\\"\"",
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        process?.WaitForExit();
    }

    private static void TrashLinux(string filePath)
    {
        // Try xdg-trash, the standard desktop tool
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "xdg-trash",
                Arguments = $"\"{filePath}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using var process = Process.Start(psi);
            process?.WaitForExit();

            if (process?.ExitCode == 0) return;
        }
        catch { /* Fallback to manual if xdg-trash is missing */ }

        // Try gio trash (Modern GNOME desktop default)
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "gio",
                Arguments = $"trash \"{filePath}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using var process = Process.Start(psi);
            process?.WaitForExit();

            if (process?.ExitCode == 0)
            {
                return;
            }
        }
        catch { /* Fallback to permanent delete if all tools fail */ }

        // Final fallback: Permanent deletion if no desktop environment is present
        File.Delete(filePath);
    }
}
