namespace Lyt.PhotoPostPro.Model.LibraryModels;

// To avoid namespace conflicts for 'Path' 
using System.IO;


/// <summary> Creates folders and names from metadata dates.</summary>
public sealed class MetadataFolders
{
    private const string YearPrefix = "Year_";

    // Will not localize to ensure portability across sysems 
    private static readonly string[] MonthPostfixes =
    [
        "_Jan",
        "_Feb",
        "_Mar",
        "_Apr",
        "_May",
        "_Jun",
        "_Jul",
        "_Aug",
        "_Sep",
        "_Oct",
        "_Nov",
        "_Dec",
    ];

    // Will not localize to ensure portability across sysems 
    private static readonly string[] DayPostfixes =
    [
        // Order MUST match the DayOfWeek enumeration, so Sunday comes first 
        "_Sun",
        "_Mon",
        "_Tue",
        "_Wed",
        "_Thu",
        "_Fri",
        "_Sat",
    ];

    public MetadataFolders(Metadata metadata)
    {
        DateTime dateTime =
            metadata.HasExifMetadata && metadata.Captured != DateTime.MinValue ?
                metadata.Captured :
                metadata.FileDateUTC.ToLocalTime();
        this.Year = YearPrefix + dateTime.Year.ToString("D4");
        int month = dateTime.Month;
        this.Month = month.ToString("D2") + MonthPostfixes[month];
        int day = dateTime.Day;
        int weekDay = (int)dateTime.DayOfWeek;
        this.Day = day.ToString("D2") + DayPostfixes[weekDay];
    }

    public string Year { get; private set; }

    public string Month { get; private set; }

    public string Day { get; private set; }

    public string CreateDirectoryPathIfNeeded(string rootPath)
    {
        try
        {
            if (!Directory.Exists(rootPath))
            {
                throw new InvalidOperationException("No such directory path: " + rootPath);
            }

            string yearFolderPath = Path.Combine(rootPath, this.Year);
            if (!Directory.Exists(yearFolderPath))
            {
                Directory.CreateDirectory(yearFolderPath);
            }

            string monthFolderPath = Path.Combine(rootPath, this.Year, this.Month);
            if (!Directory.Exists(monthFolderPath))
            {
                Directory.CreateDirectory(monthFolderPath);
            }

            string dayFolderPath = Path.Combine(rootPath, this.Year, this.Month, this.Day);
            if (!Directory.Exists(dayFolderPath))
            {
                Directory.CreateDirectory(dayFolderPath);
            }

            return dayFolderPath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return rootPath;
        }
    }

    public static bool IsYearFolder(string path, out int year)
    {
        year = 0;
        if (!Directory.Exists(path))
        {
            return false;
        }

        // Extract the directory name 
        // Trim trailing slashes so Path.GetFileName knows it is the end of the name
        string cleanPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string? name = Path.GetFileName(cleanPath); 
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        if (!name.StartsWith(YearPrefix))
        {
            return false;
        }

        name = name.Substring(YearPrefix.Length);
        if (!int.TryParse(name, out year))
        {
            return false;
        }

        if (year < 1900 || year > 2100)
        {
            return false;
        }

        return true;
    }

    public static bool IsMonthFolder(string path, out int month)
    {
        month = 0;
        if (!Directory.Exists(path))
        {
            return false;
        }

        // Extract the directory name 
        // Trim trailing slashes so Path.GetFileName knows it is the end of the name
        string cleanPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string? name = Path.GetFileName(cleanPath);
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        if (name.Length != 2 + 4)
        {
            return false;
        }

        int index = -1;
        for (int i = 0; i < MonthPostfixes.Length; ++i)
        {
            string monthPostfix = MonthPostfixes[i];
            if (name.EndsWith(monthPostfix, StringComparison.InvariantCultureIgnoreCase))
            {
                index = i;
                break;
            }
        }

        if (index == -1)
        {
            return false;
        }

        month = index;
        name = name.Substring(0, 2);
        if (!int.TryParse(name, out month))
        {
            return false;
        }

        if (month < 1 || month > 12)
        {
            return false;
        }

        return true;
    }

    public static bool IsDayFolder(string path, out int day, out int dayOfWeek)
    {
        day = 0;
        dayOfWeek = 0;
        if (!Directory.Exists(path))
        {
            return false;
        }

        // Extract the directory name 
        // Trim trailing slashes so Path.GetFileName knows it is the end of the name
        string cleanPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string? name = Path.GetFileName(cleanPath);
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        if (name.Length != 2 + 4)
        {
            return false;
        }

        int index = -1;
        for (int i = 0; i < DayPostfixes.Length; ++i)
        {
            string dayPostfix = DayPostfixes[i];
            if (name.EndsWith(dayPostfix, StringComparison.InvariantCultureIgnoreCase))
            {
                index = i;                
                break;
            }
        }

        if (index == -1)
        {
            return false;
        }

        dayOfWeek = index;
        name = name.Substring(0, 2);
        if (!int.TryParse(name, out day))
        {
            return false;
        }

        if (day < 1 || day > 31)
        {
            return false;
        }

        return true;
    }
}
