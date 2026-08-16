namespace Lyt.PhotoPostPro.Model.Utilities;

using System.Globalization;

public static class WebUtilities
{
    public static bool OpenWebUrl(string webUrl, out string message)
    {
        message = string.Empty;
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var proc = new Process { StartInfo = { UseShellExecute = true, FileName = webUrl } };
                proc.Start();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("x-www-browser", webUrl);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", webUrl);
            }
            else
            {
                throw new ArgumentException("Unsupported platform: " + RuntimeInformation.OSDescription);
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e); 
            message = "Failed to open provided URL, exception thrown: " + e.Message;
            return false;
        }
    }

    public static bool OpenLocationUrl(GeoProtocol protocol, double latitude, double longitude)
    {
        string url = GeoLocationUrl(protocol, latitude, longitude);
        return OpenWebUrl(url, out string _);
    }

    /// <summary> The preferred protocol for encoding the location. </summary>
    public enum GeoProtocol
    {
        /// <summary> Convenience URL builders for Google Bing, US Weather. Maybe more later.</summary>
        GoogleMapsLink,
        BingMapsLink,
        UsWeatherLink,
    }

    public static string GeoLocationUrl(GeoProtocol protocol, double latitude, double longitude)
    {
        IFormatProvider formatProvider = CultureInfo.InvariantCulture; 
        string latString = latitude.ToString("F6", formatProvider);
        string longString = longitude.ToString("F6", formatProvider);
        return protocol switch
        {
            // See: https://developers.google.com/maps/documentation/urls/get-started
            GeoProtocol.GoogleMapsLink => $"https://www.google.com/maps/search/?api=1&query={latString}%2C{longString}",            
            GeoProtocol.BingMapsLink => $"https://www.bing.com/maps/search?style=r&cp={latString}%7E{longString}",
            GeoProtocol.UsWeatherLink => $"https://forecast.weather.gov/MapClick.php?lon={longString}&lat={latString}",
            _ => throw new NotImplementedException("Unsupported geo protocol"),
        };
    } 
}