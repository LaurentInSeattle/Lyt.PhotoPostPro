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

        tree.Sort(); 
        return tree;
    }

    public int FileCount()
    {
        int fileCount = 0;
        foreach(YearFolder year in this.YearFolders)
        {
            foreach (MonthFolder month in year.MonthFolders)
            {
                foreach(DayFolder day in month.DayFolders)
                {
                    fileCount += day.MetadataFiles.Count;  
                }
            }
        }

        return fileCount;
    }

    public void Sort()
    {
        var sortedYears = 
            (from  year in this.YearFolders orderby year.Year ascending select year).ToList();
        this.YearFolders = sortedYears;
        foreach (YearFolder year in this.YearFolders)
        {
            var sortedMonths = 
                (from month in year.MonthFolders orderby month.Month select month ).ToList();
            year.MonthFolders = sortedMonths;
            foreach (MonthFolder month in year.MonthFolders)
            {
                var sortedDays = 
                    ( from day in month.DayFolders orderby day.Day  select day ).ToList();
                month.DayFolders = sortedDays;
            }
        } 
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
