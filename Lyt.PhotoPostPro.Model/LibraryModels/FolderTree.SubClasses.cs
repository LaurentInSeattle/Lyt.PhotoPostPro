namespace Lyt.PhotoPostPro.Model.LibraryModels;

public sealed class YearFolder 
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

    public MonthFolder AddMonthIfNeeded(int month)
    {
        MonthFolder? monthFolder = this.MonthFolders.FirstOrDefault(f => f.Month == month);
        if (monthFolder is null)
        {
            var newFolder = new MonthFolder() { Month = month, Year = this.Year };
            this.MonthFolders.Add(newFolder);
            return newFolder;
        }

        return monthFolder;
    }
}

public sealed class MonthFolder 
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

    public DayFolder AddDayIfNeeded(int day, int dayOfWeek)
    {
        DayFolder? dayFolder = this.DayFolders.FirstOrDefault(f => f.Day == day);
        if (dayFolder is null)
        {
            var newFolder = new DayFolder() { Day = day, Month = this.Month, Year = this.Year, DayOfWeek = dayOfWeek };
            this.DayFolders.Add(newFolder);
            return newFolder;
        }

        return dayFolder;
    }
}

public sealed class DayFolder 
{
    public int Year { get; set; }

    public int Month { get; set; }

    public int Day { get; set; }

    public int DayOfWeek { get; set; }

    public List<string> MetadataFiles { get; set; } = [];
}
