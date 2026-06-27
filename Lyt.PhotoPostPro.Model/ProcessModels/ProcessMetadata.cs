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

        if (directories is not null && directories.Count > 0)
        {
            this.ExifDrectories = directories;
            this.PopulateExifMetadata(directories);
        }

        FileInfo? fileInfo = new(this.FullPath);
        if (fileInfo is not null)
        {
            if (!fileInfo.Exists)
            {
                // Should never happen 
                if ( Debugger.IsAttached )   { Debugger.Break(); }
                throw new Exception("File does not exist.");
            }

            this.Filename = fileInfo.Name;
            this.Extension = fileInfo.Extension.Replace(".", "").ToUpperInvariant();
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


    public bool HasExifMetadata { get; private set; }

    // Image properties that are possibly present when HasExifMetadata is true 

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

    public bool HasLocationMetadata
        =>  double.IsNormal(this.Latitude) && 
            double.IsNormal(this.Longitude) && 
            double.IsNormal(this.Elevation); 

    public double Latitude { get; private set; } = double.NaN; 

    public double Longitude { get; private set; } = double.NaN;

    public double Elevation { get; private set; } = double.NaN;

    private static readonly Dictionary<string, string> ExifToCode = new()
    {
        { "Make" , "Make" },
        { "Model" , "Model" },
        { "Date/Time Original" , "Captured" },

        { "F-Number" , "Aperture" },
        { "ISO Speed Ratings" , "IsoSpeed" },
        { "Exposure Time" , "Exposure" },
        { "Exposure Bias Value" , "ExposureBias" },
        { "Focal Length" , "FocalLength" },
        //{ "" , "" },
        //{ "" , "" },
        //{ "" , "" },
    };

    private void PopulateExifMetadata(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        this.HasExifMetadata = false;

        bool TryParseExifDate ( string stringValue, out DateTime dateTime )
        {
            dateTime = DateTime.MinValue; 
            string[] tokens = 
                stringValue.Split(
                    [' ', ':', '/'], 
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); 
            if ( tokens.Length != 6)
            {
                return false; 

            }
            
            if (int.TryParse(tokens[0] , out int year)) 
            {
                if (int.TryParse(tokens[1], out int month ))
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

        bool SetExifProperty(string exif, string stringValue)
        {
            try
            {
                if (ExifToCode.TryGetValue(exif, out string? code))
                {
                    var propertyInfo = this.GetType().GetProperty(code, BindingFlags.Instance | BindingFlags.Public);
                    if (propertyInfo is not null)
                    {
                        var methodInfo = propertyInfo.GetSetMethod(nonPublic: true);
                        if (methodInfo is not null)
                        {
                            if (propertyInfo.PropertyType == typeof(string))
                            {
                                methodInfo.Invoke(this, [stringValue]);
                                return true;
                            }
                            else if (propertyInfo.PropertyType == typeof(DateTime))
                            {
                                if (DateTime.TryParse(stringValue, out var regularDateTime))
                                {
                                    methodInfo.Invoke(this, [regularDateTime]);
                                    return true;
                                }
                                else
                                {
                                    if ( TryParseExifDate (stringValue, out DateTime exifDateTime))
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
                            else
                            {
                                Debug.WriteLine("No matching type for property " + exif);
                            }
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
                    // TODO: Add GPS section 
                    name.StartsWith("Exif IFD0") || name.StartsWith("Exif SubIFD");
                if ( ! isStandardExif)
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

                    if(SetExifProperty(tagName, description))
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
