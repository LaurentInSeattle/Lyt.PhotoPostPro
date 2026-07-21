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
                YearFolder yearFolder = new() { Year = year };
                tree.YearFolders.Add(yearFolder);

                var directoryMonths = Directory.EnumerateDirectories(directoryYear);
                foreach (string directoryMonth in directoryMonths)
                {
                    if (MetadataFolders.IsMonthFolder(directoryMonth, out int month))
                    {
                        MonthFolder monthFolder = new() { Year = year, Month = month };
                        yearFolder.MonthFolders.Add(monthFolder);

                        var directoryDays = Directory.EnumerateDirectories(directoryMonth);
                        foreach (string directoryDay in directoryDays)
                        {
                            if (MetadataFolders.IsDayFolder(directoryDay, out int day, out int dayOfWeek))
                            {

                                // Enumerate metadata files 
                                // Bring the '_META' filter because we also have the _EDIT.json files containing edits 
                                var files = Directory.EnumerateFiles(directoryDay, "*_META.json");
                                if (!files.Any())
                                {
                                    // No files, possibly deleted, no need to delete the folder 
                                    continue;
                                }

                                // Create a DayFolder only if we have files 
                                DayFolder dayFolder = new()
                                {
                                    Year = year,
                                    Month = month,
                                    Day = day,
                                    DayOfWeek = dayOfWeek,
                                };

                                monthFolder.DayFolders.Add(dayFolder);

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
        foreach (YearFolder year in this.YearFolders)
        {
            foreach (MonthFolder month in year.MonthFolders)
            {
                foreach (DayFolder day in month.DayFolders)
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
            (from year in this.YearFolders orderby year.Year ascending select year).ToList();
        this.YearFolders = sortedYears;
        foreach (YearFolder year in this.YearFolders)
        {
            var sortedMonths =
                (from month in year.MonthFolders orderby month.Month select month).ToList();
            year.MonthFolders = sortedMonths;
            foreach (MonthFolder month in year.MonthFolders)
            {
                var sortedDays =
                    (from day in month.DayFolders orderby day.Day select day).ToList();
                month.DayFolders = sortedDays;
            }
        }
    }

    public void UpdateOnFileAdded(Metadata metadata, string metadataFilePath)
    {
        metadata.GetLibraryFolders(out int year, out int month, out int day, out int dayOfWeek);
        var yearFolder =
            (from folder in this.YearFolders where folder.Year == year select folder)
            .FirstOrDefault();
        if (yearFolder is null)
        {
            yearFolder = new YearFolder() { Year = year };
            this.YearFolders.Add(yearFolder);
        }

        var monthFolder =
            (from folder in yearFolder.MonthFolders where folder.Month == month select folder)
            .FirstOrDefault();
        if (monthFolder is null)
        {
            monthFolder = new MonthFolder() { Month = month, Year = year };
            yearFolder.MonthFolders.Add(monthFolder);
        }

        var dayFolder =
            (from folder in monthFolder.DayFolders where folder.Day == day select folder)
            .FirstOrDefault();
        if (dayFolder is null)
        {
            dayFolder = new DayFolder() { Day = day, DayOfWeek = dayOfWeek, Month = month, Year = year };
            monthFolder.DayFolders.Add(dayFolder);
        }

        dayFolder.MetadataFiles.Add(metadataFilePath); 

        this.Sort();
    }

    public void UpdateOnFileRemoved()
    {
    }
}
