namespace Lyt.PhotoPostPro.Model.ProcessModels;

public sealed class ProcessMetadata
{
    private const float Megabytes = 1024.0f * 1024.0f;

    public ProcessMetadata(
        string fullPath,
        int width,
        int height,
        IReadOnlyList<MetadataExtractor.Directory>? directories)
    {
        this.FullPath = fullPath;
        this.Width = width;
        this.Height = height;

        FileInfo? fileInfo = new(this.FullPath);
        if (fileInfo is not null)
        {
            if (!fileInfo.Exists)
            {
                // Should never happen 
                if (Debugger.IsAttached) { Debugger.Break(); }
                throw new Exception("File does not exist.");
            }

            string extension = fileInfo.Extension;
            this.Filename = fileInfo.Name.Replace(extension, "");
            this.Extension = extension.Replace(".", "").ToUpperInvariant();
            float length = (float)fileInfo.Length / Megabytes;
            this.SizeMB = string.Format("{0:F1} MB", length);
            this.FileDateUTC = fileInfo.CreationTimeUtc;
        }
        else
        {
            // Should never happen 
            if (Debugger.IsAttached) { Debugger.Break(); }
            throw new Exception("File does not exist.");
        }

        if (directories is not null && directories.Count > 0)
        {
            this.ExifDrectories = directories;
            this.PopulateExifMetadata(directories);

            if (this.HasLocationMetadata)
            {
                Debug.WriteLine("  Location:  " + this.Latitude.ToString("F6") + "," + this.Longitude.ToString("F6"));
            }

            if (!string.IsNullOrWhiteSpace(this.Make) && !string.IsNullOrWhiteSpace(this.Model))
            {
                if (this.Model.Contains(this.Make))
                {
                    this.Model = this.Model.Replace(this.Make, string.Empty);
                    this.Model = this.Model.Trim();
                }
            }
        }
    }

    // Basic properties that should always be present 

    public string FullPath { get; private set; }

    public string Filename { get; private set; }

    public string Extension { get; private set; }

    public string SizeMB { get; private set; }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public string Dimensions => string.Format("{0:D} x {1:D}", this.Width, this.Height);

    public DateTime FileDateUTC { get; private set; }

    // Image properties that are possibly present when HasExifMetadata is true 

    public bool HasExifMetadata { get; private set; }

    public IReadOnlyList<MetadataExtractor.Directory>? ExifDrectories { get; private set; }

    public string Make { get; private set; } = string.Empty;

    public string Model { get; private set; } = string.Empty;

    public DateTime Captured { get; private set; }

    public string Aperture { get; private set; } = string.Empty;

    public string Exposure { get; private set; } = string.Empty;

    public string ExposureBias { get; private set; } = string.Empty;

    public string IsoSpeed { get; private set; } = string.Empty;

    public string FocalLength { get; private set; } = string.Empty;

    public bool WithFlash { get; private set; }

    public bool HasLocationMetadata => double.IsNormal(this.Latitude) && double.IsNormal(this.Longitude);

    public double Latitude { get; private set; } = double.NaN;

    public double Longitude { get; private set; } = double.NaN;

    public string LatitudeString { get; private set; } = string.Empty;

    public string LongitudeString { get; private set; } = string.Empty;

    private static readonly List<Tuple<string, string>> ExifToCode = new()
    {
        new( "Make" , "Make" ),
        new( "Model" , "Model" ),
        new( "Date/Time Original" , "Captured" ),

        new( "F-Number" , "Aperture" ),
        new( "ISO Speed Ratings" , "IsoSpeed" ),
        new( "Exposure Time" , "Exposure" ),
        new( "Exposure Bias Value" , "ExposureBias" ),
        new( "Focal Length" , "FocalLength" ),
        new( "Flash" , "WithFlash" ),

        new( "GPS Latitude" , "Latitude" ),
        new( "GPS Longitude" , "Longitude" ),
        new( "GPS Latitude" , "LatitudeString" ),
        new( "GPS Longitude" , "LongitudeString" ),
    };

    private void PopulateExifMetadata(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        this.HasExifMetadata = false;

        bool TryParseExifDate(string stringValue, out DateTime dateTime)
        {
            dateTime = DateTime.MinValue;
            string[] tokens =
                stringValue.Split(
                    [' ', ':', '/'],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length != 6)
            {
                return false;

            }

            if (int.TryParse(tokens[0], out int year))
            {
                if (int.TryParse(tokens[1], out int month))
                {
                    if (int.TryParse(tokens[2], out int day))
                    {
                        if (int.TryParse(tokens[3], out int hour))
                        {
                            if (int.TryParse(tokens[4], out int minute))
                            {
                                if (int.TryParse(tokens[5], out int second))
                                {
                                    dateTime = new DateTime(year, month, day, hour, minute, second);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        bool TryParseExifGps(string stringValue, out double gpsLocation)
        {
            gpsLocation = double.NaN;
            string[] tokens =
                stringValue.Split(
                    [' ', '\'', '"', '°'],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length != 3)
            {
                return false;
            }

            if (int.TryParse(tokens[0], out int degrees))
            {
                if (int.TryParse(tokens[1], out int minutes))
                {
                    if (double.TryParse(tokens[2], out double seconds))
                    {
                        if (degrees > 0)
                        {
                            gpsLocation = degrees + minutes / 60.0 + seconds / 3600.0;
                        }
                        else
                        {
                            gpsLocation = degrees - minutes / 60.0 - seconds / 3600.0;
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        bool SetExifProperty(string exif, string code, string stringValue)
        {
            try
            {
                var propertyInfo = this.GetType().GetProperty(code, BindingFlags.Instance | BindingFlags.Public);
                if (propertyInfo is not null)
                {
                    var methodInfo = propertyInfo.GetSetMethod(nonPublic: true);
                    if (methodInfo is not null)
                    {
                        var propertyType = propertyInfo.PropertyType;
                        if (propertyType == typeof(string))
                        {
                            methodInfo.Invoke(this, [stringValue]);
                            return true;
                        }
                        else if (propertyType == typeof(DateTime))
                        {
                            if (DateTime.TryParse(stringValue, out var regularDateTime))
                            {
                                methodInfo.Invoke(this, [regularDateTime]);
                                return true;
                            }
                            else
                            {
                                if (TryParseExifDate(stringValue, out DateTime exifDateTime))
                                {
                                    methodInfo.Invoke(this, [exifDateTime]);
                                    return true;
                                }
                                else
                                {
                                    Debug.WriteLine("Failed to parse date/time for property " + exif + "  " + stringValue);
                                }
                            }
                        }
                        else if (propertyType == typeof(double))
                        {
                            // For now , only latitude and longitude 
                            if (code == "Latitude" || code == "Longitude")
                            {
                                if (TryParseExifGps(stringValue, out double exifDouble))
                                {
                                    methodInfo.Invoke(this, [exifDouble]);
                                    return true;
                                }
                                else
                                {
                                    Debug.WriteLine("Failed to parse latitude or longitude for property " + exif + "  " + stringValue);
                                }
                            }
                            // Else ignore 
                        }
                        else if (propertyType == typeof(bool))
                        {
                            // For now , only latitude and longitude 
                            if (code == "WithFlash")
                            {
                                if (stringValue.Contains("Flash fired", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    methodInfo.Invoke(this, [true]);
                                    return true;
                                }
                            }
                            // Else" ignore 
                        }
                        else
                        {
                            Debug.WriteLine("No matching type for property " + exif);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return false;
            }

            return false;
        }

        bool SetExifProperties(string exif, string stringValue)
        {
            var properties = (from tuple in ExifToCode where tuple.Item1 == exif select tuple).ToList();
            if (properties.Count == 0)
            {
                return false;
            }

            int count = 0;
            foreach (var property in properties)
            {
                if (SetExifProperty(property.Item1, property.Item2, stringValue))
                {
                    ++count;
                }
            }

            return count > 0;
        }

        try
        {
            foreach (var directory in directories)
            {
                if (directory.IsEmpty)
                {
                    continue;
                }

                string name = directory.Name;
                bool isStandardExif =
                    name.StartsWith("GPS") ||
                    name.StartsWith("Exif IFD0") ||
                    name.StartsWith("Exif SubIFD");
                if (!isStandardExif)
                {
                    // Ignore everything that is not standard 
                    continue;
                }

                foreach (var tag in directory.Tags)
                {
                    string? tagName = tag.Name;
                    if (string.IsNullOrWhiteSpace(tagName))
                    {
                        continue;
                    }

                    string? description = tag.Description;
                    if (string.IsNullOrWhiteSpace(description))
                    {
                        continue;
                    }

                    if (SetExifProperties(tagName, description))
                    {
                        this.HasExifMetadata = true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }
}
