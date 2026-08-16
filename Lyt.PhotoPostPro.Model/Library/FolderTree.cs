namespace Lyt.PhotoPostPro.Model.Library;

// Prevents conflicts with Six Labors, etc.
using System.IO;

public sealed class FolderTree
{
    public List<YearFolder> YearFolders { get; set; } = [];

    public static FolderTree GenerateFromFilesOnDisk(string rootPath)
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

        tree.Cleanup();
        tree.Sort();
        return tree;
    }

    public static FolderTree GenerateFromDate(Dictionary<string, LoadedThumbnail> metadataDictionary, bool forDateAdded)
    {
        FolderTree tree = new();
        foreach (var item in metadataDictionary)
        {
            string filePath = item.Key;
            Metadata metadata = item.Value.Metadata;
            var date = forDateAdded ? metadata.AddedToLibraryUTC.ToLocalTime() : metadata.LastEditedUTC.ToLocalTime();
            if (!forDateAdded && (date == DateTime.MinValue))
            {
                // Never edited, skip it
                continue;
            }

            if (forDateAdded && (date == DateTime.MinValue || date.Year < 1926))
            {
                // Corrupted date added, skip it 
                continue;

                // DON'T : This creates very confusing empty slots in the Library view
                // Use current date and time as a fallback for date added
                // date = DateTime.Now;
            }

            int year = date.Year;
            int month = date.Month;
            int day = date.Day;
            int dayOfWeek = (int)date.DayOfWeek;

            YearFolder yearFolder = tree.AddYearIfNeeded(year);
            MonthFolder monthFolder = yearFolder.AddMonthIfNeeded(month);
            DayFolder dayFolder = monthFolder.AddDayIfNeeded(day, dayOfWeek);
            dayFolder.MetadataFiles.Add(filePath);
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

    public void Cleanup()
    {
        // Remove years 
        var yearsToRemove = new List<YearFolder>();
        foreach (YearFolder year in this.YearFolders)
        {
            if (year.MetadataFiles().Count == 0)
            {
                yearsToRemove.Add(year);
            }
        }

        foreach (YearFolder year in yearsToRemove)
        {
            this.YearFolders.Remove(year);
        }

        // Remove months on all remaining years 
        foreach (YearFolder year in this.YearFolders)
        {
            var monthsToRemove = new List<MonthFolder>();
            foreach (MonthFolder month in year.MonthFolders)
            {
                if (month.MetadataFiles().Count == 0)
                {
                    monthsToRemove.Add(month);
                }
            }

            foreach (MonthFolder month in monthsToRemove)
            {
                year.MonthFolders.Remove(month);
            }
        }
    }

    public DayFolder UpdateOnFileAdded(DateKind dateKind, Metadata metadata, string metadataFilePath, bool doSort = true)
    {
        metadata.GetLibraryFolders(dateKind, out int year, out int month, out int day, out int dayOfWeek);
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

        if (doSort)
        {
            this.Sort();
        } 

        return dayFolder;
    }

    public void UpdateOnFileRemoved() => this.Cleanup();

    public YearFolder AddYearIfNeeded(int year)
    {
        YearFolder? yearFolder = this.YearFolders.FirstOrDefault(f => f.Year == year);
        if (yearFolder is null)
        {
            var newFolder = new YearFolder() { Year = year };
            this.YearFolders.Add(newFolder);
            return newFolder;
        }

        return yearFolder;
    }

    internal void Remove(string metadataFilePath)
    {
        foreach (var year in this.YearFolders)
        {
            foreach (var month in year.MonthFolders)
            {
                foreach (var day in month.DayFolders)
                {
                    if (day.MetadataFiles.Remove(metadataFilePath))
                    {
                        break;
                    }
                }
            }
        }
    }
}
