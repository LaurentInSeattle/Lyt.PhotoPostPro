namespace Lyt.PhotoPostPro.Model.LibraryModels;

// To avoid namespace conflicts for 'Path' 
using System.IO;


/// <summary> Creates folders and names from metadata dates.</summary>
public sealed class MetadataFolders
{
    private const string YearPrefix = "Year_";

    // Will not localize to ensure portability across sysems 
    private readonly string[] MonthPostfixes =
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
    private readonly string[] DayPostfixes =
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
                metadata.FileDateUTC.ToLocalTime() ;
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

    public string CreateDirectoryPathIfNeeded (string rootPath)
    {
        try
        {
            if (!Directory.Exists(rootPath))
            {
                throw new InvalidOperationException( "No such directory path: " + rootPath );
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
}
