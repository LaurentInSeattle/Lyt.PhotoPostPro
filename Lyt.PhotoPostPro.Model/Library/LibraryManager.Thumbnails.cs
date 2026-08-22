namespace Lyt.PhotoPostPro.Model.Library;

using System.IO ;

public sealed partial class LibraryManager
{
    public static void StaticLoadThumbnails(object? data)
    {
        if (data is not LibraryManager libraryManager)
        {
            return;
        }

        libraryManager.LoadThumbnails();
    }

    public void GenerateThumbnailCache()
    {
        // Explicit background low priority background thread
        var start = new ParameterizedThreadStart(StaticLoadThumbnails);
        Thread lowPriorityThread = new(start)
        {
            Priority = ThreadPriority.Lowest,
            IsBackground = true
        };
        lowPriorityThread.Start(this);
    }

    public void LoadThumbnails()
    {
        this.imageLoadedCount = 0;
        this.errorLoadingCount = 0;

        if (this.CapturedFolderTree is null)
        {
            return;
        }

        // ! We MUST have a model 
        var profiler = this.model!.Profiler;
        profiler.StartTiming();

        List<string> paths = new(1024);
        foreach (var year in this.CapturedFolderTree.YearFolders)
        {
            foreach (var month in year.MonthFolders)
            {
                foreach (var day in month.DayFolders)
                {
                    foreach (string path in day.MetadataFiles)
                    {
                        paths.Add(path);
                    }
                }
            }
        }

        Parallel.For(0, paths.Count, index =>
        {
            string path = paths[index];
            LoadedThumbnail? thumbnail = this.LoadThumbnail(path);
            if (thumbnail is not null)
            {
                lock (this.LoadedThumbnails)
                {
                    // Debug.WriteLine(" Loaded Thumbnail: " + path);
                    this.LoadedThumbnails.Add(path, thumbnail);
                }
            }
        });

        profiler.EndTiming(" Loaded Thumbnails: " + this.LoadedThumbnails.Count);

        this.AddedFolderTree = FolderTree.GenerateFromDate(this.LoadedThumbnails, forDateAdded: true);
        this.EditedFolderTree = FolderTree.GenerateFromDate(this.LoadedThumbnails, forDateAdded: false);

        new LibraryLoadedMessage(ImageCount: this.imageLoadedCount, ErrorCount: this.errorLoadingCount).Publish();
        this.IsLoading = false;
    }

    public void UpdateThumbnailCache(Metadata metadata, string pathThumbnail)
    {
        try
        {
            byte[] imageBytes = File.ReadAllBytes(pathThumbnail);
            LoadedThumbnail loadedThumbnail = new(metadata, imageBytes);

            // Kinda hackish !
            string endsWith = "_THUMB_EDIT.jpg";
            if (!pathThumbnail.EndsWith(endsWith))
            {
                if (Debugger.IsAttached) { Debugger.Break(); }
                return;
            }

            string path = pathThumbnail.Replace(endsWith, "_META.json");

#if DEBUG
            if (!this.LoadedThumbnails.ContainsKey(path))
            {
                if (Debugger.IsAttached) { Debugger.Break(); }
                throw new Exception("No folder key");
            }
#endif 
            this.LoadedThumbnails[path] = loadedThumbnail;
            new ThumbnailUpdatedMessage(path).Publish();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private LoadedThumbnail? LoadThumbnail(string metadataFilePath)
    {
        try
        {
            string serialized = File.ReadAllText(metadataFilePath);
            var jsonTypeInfo = AppJsonContext.Default.Metadata;

            // ! Checked before calling 
            Metadata? maybe = this.fileManager!.Deserialize(serialized, jsonTypeInfo);
            if (maybe is not Metadata metadata)
            {
                throw new Exception("Failed to load metadata: " + metadataFilePath);
            }

            string? folderPath = Path.GetDirectoryName(metadataFilePath);
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                throw new Exception("Inavlid path: " + folderPath);
            }

            string filenameThumbnailEdit = metadata.Filename + "_THUMB_EDIT.jpg";
            string pathThumbnail = Path.Combine(folderPath, filenameThumbnailEdit);
            if (!File.Exists(pathThumbnail))
            {
                string filenameThumbnail = metadata.Filename + "_THUMB.jpg";
                pathThumbnail = Path.Combine(folderPath, filenameThumbnail);
            }

            if (File.Exists(pathThumbnail))
            {
                byte[] imageBytes = File.ReadAllBytes(pathThumbnail);
                ++this.imageLoadedCount;
                return new LoadedThumbnail(metadata, imageBytes);
            }
            else
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                throw new Exception("Inavlid path: " + pathThumbnail);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            ++this.errorLoadingCount;
            return null;
        }
    }
}
