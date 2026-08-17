namespace Lyt.PhotoPostPro;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Lyt.PhotoPostPro.Implementations.Windows.Mtp;
using Lyt.PhotoPostPro.MultiPlatformAbstractions.Mtp;

using Lyt.PhotoPostPro.Implementations.Windows.Wallpaper;
using Lyt.PhotoPostPro.MultiPlatformAbstractions.Wallpaper;

public static class ImplementationsProvider
{
    public static IMtpService MtpService()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            {
                // OSPlatform.Linux is NOT supported, at least for now, no way to test it here
                throw new ArgumentException("Unsupported platform: " + RuntimeInformation.OSDescription);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new MtpService();
            }

            throw new ArgumentException("Unknown platform: No OSDescription" );
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            throw;
        }
    }

    public static IWallpaperService WallpaperService()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            {
                // OSPlatform.Linux is NOT supported, at least for now, no way to test it here
                throw new ArgumentException("Unsupported platform: " + RuntimeInformation.OSDescription);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WallpaperService();
            }

            throw new ArgumentException("Unknown platform: No OSDescription");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            throw;
        }
    }
}
