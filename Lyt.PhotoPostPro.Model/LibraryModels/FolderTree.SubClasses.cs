namespace Lyt.PhotoPostPro.Model.LibraryModels;

public sealed class YearFolder
{
    public int Year { get; set; }

    public string Path { get; set; } = string.Empty;

    public List<MonthFolder> MonthFolders { get; set; } = [];

}

public sealed class MonthFolder
{
    public int Year { get; set; }

    public int Month { get; set; }

    public string Path { get; set; } = string.Empty;

    public List<DayFolder> DayFolders { get; set; } = [];

}

public sealed class DayFolder
{
    public int Year { get; set; }

    public int Month { get; set; }

    public int Day { get; set; }

    public int DayOfWeek { get; set; }

    public string Path { get; set; } = string.Empty;

    public List<string> MetadataFiles { get; set; } = [];
}
