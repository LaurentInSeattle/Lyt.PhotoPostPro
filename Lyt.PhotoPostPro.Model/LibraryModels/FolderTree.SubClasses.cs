namespace Lyt.PhotoPostPro.Model.LibraryModels;

public sealed class YearFolder : FolderBase
{
    public int Year { get; set; }

    public List<MonthFolder> MonthFolders { get; set; } = [];

    public List<string> MetadataFiles()
    {
        List<string> files = [];
        foreach (var month in this.MonthFolders)
        {
            files.AddRange(month.MetadataFiles());
        }

        return files;
    }
}

public sealed class MonthFolder : FolderBase
{
    public int Year { get; set; }

    public int Month { get; set; }

    public List<DayFolder> DayFolders { get; set; } = [];

    public List<string> MetadataFiles()
    {
        List<string> files = [];  
        foreach (var day in this.DayFolders)
        {
            files.AddRange(day.MetadataFiles); 
        }

        return files;
    }
}

public sealed class DayFolder : FolderBase
{
    public int Year { get; set; }

    public int Month { get; set; }

    public int Day { get; set; }

    public int DayOfWeek { get; set; }

    public List<string> MetadataFiles { get; set; } = [];
}

public class FolderBase
{
    public string Path { get; set; } = string.Empty;
}
