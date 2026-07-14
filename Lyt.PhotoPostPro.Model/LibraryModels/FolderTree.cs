namespace Lyt.PhotoPostPro.Model.LibraryModels;

// Prevents conflicts with Six Labors, etc.
using System.IO;

public sealed class FolderTree
{
    public List<YearFolder> YearFolders { get; set; } = [];

    public static FolderTree Generate(string rootPath)
    {
        FolderTree tree = new();

        // CONSIDER: Use parallelization on top folders 
        var directories = Directory.EnumerateDirectories(rootPath);
        foreach (string directoryYear in directories)
        {
            if (MetadataFolders.IsYearFolder(directoryYear, out int year))
            {
                YearFolder yearFolder = new() { Year = year, Path = directoryYear };
                tree.YearFolders.Add(yearFolder);

                var directoryMonths = Directory.EnumerateDirectories(directoryYear);
                foreach (string directoryMonth in directoryMonths)
                {
                    if (MetadataFolders.IsMonthFolder(directoryMonth, out int month))
                    {
                        MonthFolder monthFolder = new() { Year = year, Month = month, Path = directoryMonth };
                        yearFolder.MonthFolders.Add(monthFolder);

                        var directoryDays = Directory.EnumerateDirectories(directoryMonth);
                        foreach (string directoryDay in directoryDays)
                        {
                            if (MetadataFolders.IsDayFolder(directoryDay, out int day, out int dayOfWeek))
                            {
                                DayFolder dayFolder = new()
                                {
                                    Year = year,
                                    Month = month,
                                    Day = day,
                                    DayOfWeek = dayOfWeek,
                                    Path = directoryDay
                                };

                                monthFolder.DayFolders.Add(dayFolder);

                                // Enumerate metadata files 
                                var files = Directory.EnumerateFiles(directoryDay, "*.json");
                                foreach (string file in files)
                                {
                                    dayFolder.MetadataFiles.Add(file);
                                }
                            }
                        }
                    }
                }
            }
        }

        return tree;
    }

    public void Sort()
    {
        // TODO
    }

    public void UpdateOnFileAdded() 
    {
        // TODO
        this.Sort();
    }

    public void UpdateOnFileRemoved()
    {
    }
}

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