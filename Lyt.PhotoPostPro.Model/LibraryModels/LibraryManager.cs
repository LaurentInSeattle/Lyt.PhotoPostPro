namespace Lyt.PhotoPostPro.Model.LibraryModels;

// To avoid namespace conflicts for 'Path' 
using System.IO;

public sealed class LibraryManager
{
    public const string LibraryFolderName = "Library";
    public const string ExportsFolderName = "Exports";

    public const int CachedHdImageCount = 300;

    private readonly Lock lockObject;

    private readonly string libraryFolderPath;
    private readonly string exportsFolderPath;

    private PhotoPostProModel? model;
    private FileManagerModel? fileManager;

    private int imageLoadedCount = 0;
    private int errorLoadingCount = 0;

    public LibraryManager()
    {
        this.lockObject = new Lock();
        string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        this.libraryFolderPath = Path.Combine(pictures, PhotoPostProModel.PhotoPostProAppName, LibraryFolderName);
        if (!Directory.Exists(this.libraryFolderPath))
        {
            Directory.CreateDirectory(this.libraryFolderPath);
        }

        this.exportsFolderPath = Path.Combine(pictures, PhotoPostProModel.PhotoPostProAppName, ExportsFolderName);
        if (!Directory.Exists(this.exportsFolderPath))
        {
            Directory.CreateDirectory(this.exportsFolderPath);
        }

        // Adjust capacity as needed
        this.LoadedHdImages = new(CachedHdImageCount);
        this.LoadedThumbnails = [];
    }

    public string LibraryFolderPath => this.libraryFolderPath;

    public string ExportsFolderPath => this.exportsFolderPath;

    // This dictionary is indexed by the path of the source image 
    public LruDictionary<string, LoadedImage> LoadedHdImages { get; private set; }

    // This dictionary is indexed by the path of the metatdata file for the image 
    public Dictionary<string, LoadedThumbnail> LoadedThumbnails { get; private set; }

    public FolderTree? CapturedFolderTree { get; private set; }

    public FolderTree? AddedFolderTree { get; private set; }

    public FolderTree? EditedFolderTree { get; private set; }

    public bool IsLoading { get; private set; }

    public void Initialize(PhotoPostProModel model, FileManagerModel fileManagerModel)
    {
        this.model = model;
        this.fileManager = fileManagerModel;
        this.IsLoading = true;
        this.InitialLibraryLoad();
    }

    public bool AddDownloadedFiles(List<Metadata> files)
    {
        if ((this.fileManager is null) ||
            (this.CapturedFolderTree is null) ||
            (this.AddedFolderTree is null))
        {
            throw new Exception("Library Manager is not initialized.");
        }

        int errors = 0;
        List<Exception> exceptions = [];

        bool AddDownloadedFile(Metadata metadata)
        {

            try
            {
                if (!File.Exists(metadata.FullPath))
                {
                    throw new Exception("No such file: " + metadata.FullPath);
                }

                // Create target library folder if needed 
                MetadataFolders metadataFolders = new(metadata);
                string targetFolder = metadataFolders.CreateDirectoryPathIfNeeded(this.libraryFolderPath);

                // Move main file 
                string targetFilename = Path.GetFileName(metadata.FullPath);
                string targetPath = Path.Combine(targetFolder, targetFilename);
                File.Move(metadata.FullPath, targetPath, overwrite: true);

                // Move thumbnail file 
                string? sourceFolder =
                    Path.GetDirectoryName(metadata.FullPath) ??
                    throw new Exception("No source folder for: " + metadata.FullPath);
                string filenameThumbnail = metadata.Filename + "_THUMB.jpg";
                string targetPathThumbnail = Path.Combine(targetFolder, filenameThumbnail);
                string sourcePathThumbnail = Path.Combine(sourceFolder, filenameThumbnail);
                File.Move(sourcePathThumbnail, targetPathThumbnail, overwrite: true);

                // Read back so that we can populate the cache
                byte[] thumbnailImageBytes = File.ReadAllBytes(targetPathThumbnail);

                return this.AddFileFinalSteps(metadata, targetPath, targetFolder, thumbnailImageBytes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                lock (exceptions)
                {
                    ++errors;
                    exceptions.Add(ex);
                }

                return false;
            }
        }

#if DEBUG 
        foreach (var file in files)
        {
            AddDownloadedFile(file);
        }
#else
        Parallel.For(0, files.Count, index =>
        {
            AddDownloadedFile(files[index]);
        });

#endif

        this.CapturedFolderTree.Sort();
        this.AddedFolderTree.Sort();

        // TODO: Return more details 
        return errors == 0;
    }

    public bool AddDroppedFile(LoadedImage loadedImage)
    {
        if ((this.fileManager is null) ||
            (this.CapturedFolderTree is null) ||
            (this.AddedFolderTree is null))            
        {
            throw new Exception("Library Manager is not initialized.");
        }

        try
        {
            if (!loadedImage.IsPreLoaded)
            {
                throw new Exception("Image is not preloaded.");
            }

            // ! Checked by loadedImage.IsPreLoaded
            Metadata metadata = loadedImage.Metadata!;

            // ! Checked by loadedImage.IsPreLoaded
            byte[] thumbnailImageBytes = loadedImage.JpgThumbnail!;

            if (!File.Exists(metadata.FullPath))
            {
                throw new Exception("No such file: " + metadata.FullPath);
            }

            // Create target folder if needed 
            MetadataFolders metadataFolders = new(metadata);
            string targetFolder = metadataFolders.CreateDirectoryPathIfNeeded(this.libraryFolderPath);

            // Copy main file - NOT Move 
            string targetFilename = Path.GetFileName(metadata.FullPath);
            string targetPath = Path.Combine(targetFolder, targetFilename);
            File.Copy(metadata.FullPath, targetPath, overwrite: true);

            // Create thumbnail file 
            string filenameThumbnail = metadata.Filename + "_THUMB.jpg";
            string targetPathThumbnail = Path.Combine(targetFolder, filenameThumbnail);
            File.WriteAllBytes(targetPathThumbnail, thumbnailImageBytes);

            bool success = this.AddFileFinalSteps(metadata, targetPath, targetFolder, thumbnailImageBytes);
            if (success)
            {
                this.CapturedFolderTree.Sort();
                this.AddedFolderTree.Sort();
            }

            return success; 
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return false;
        }
    }

    private bool AddFileFinalSteps(
        Metadata metadata, string imageFilePath, string imageFileFolder, byte[] thumbnailImageBytes)
    {
        if ((this.fileManager is null) ||
            (this.CapturedFolderTree is null) ||
            (this.AddedFolderTree is null) ||
            (this.EditedFolderTree is null))
        {
            throw new Exception("Library Manager is not initialized.");
        }

        try
        {
            // Verify main image file 
            FileInfo fileInfo = new(imageFilePath);
            if (!fileInfo.Exists)
            {
                throw new Exception("Failed to copy file" + metadata.FullPath);
            }

            if (fileInfo.Length != metadata.Length)
            {
                throw new Exception("Failed to verify file copy" + metadata.FullPath);
            }

            // update metadata 
            metadata.HasMovedTo(imageFilePath);
            metadata.AddedToLibraryUTC = DateTime.UtcNow;

            // Finally serialize and save metadata 
            string filenameMetadata = metadata.Filename + "_META.json";
            string targetPathMetadata = Path.Combine(imageFileFolder, filenameMetadata);
            string serialized = this.fileManager.Serialize<Metadata>(metadata);
            File.WriteAllText(targetPathMetadata, serialized);

            // Now update in memory data structures 
            // There can be multiple threads moving files so we need to lock them
            lock (this.lockObject)
            {
                // Add thumbnail to cache 
                LoadedThumbnail loadedThumbnail = new(Metadata: metadata, thumbnailImageBytes);
                this.LoadedThumbnails.Add(targetPathMetadata, loadedThumbnail);

                // Update folder trees 
                this.CapturedFolderTree.UpdateOnFileAdded(metadata, targetPathMetadata, doSort: false);
                this.AddedFolderTree.UpdateOnFileAdded(metadata, targetPathMetadata, doSort: false);
            }

            // All good 
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return false;
        }
    }

    public void UpdateEditedFile(Metadata metadata)
    {
        // Check if library manager is initialized
        if ((this.fileManager is null) || (this.EditedFolderTree is null))
        {
            throw new Exception("Library Manager is not initialized.");
        }

        try
        {
            // Update folder tree 
            string metadataFilePath = metadata.MetadataFullPath();
            this.EditedFolderTree.Remove(metadataFilePath);
            var dayFolder = this.EditedFolderTree.UpdateOnFileAdded(metadata, metadataFilePath);

            // Notify UI 
            new FolderTreeUpdatedMessage(FolderTreeKind.Edited, dayFolder).Publish();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
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
            // ! Checked before calling 
            Metadata? maybe = this.fileManager!.Deserialize<Metadata>(serialized);
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

    public void GenerateFolderTree()
    {
        try
        {
            var folderTree = FolderTree.GenerateFromFilesOnDisk(this.libraryFolderPath);
            this.CapturedFolderTree = folderTree;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            if (Debugger.IsAttached) { Debugger.Break(); }
        }
    }

    public void InitialLibraryLoad()
    {
        Task.Run(() =>
        {
            // wait a bit so that we dont delay app starting up 
            Task.Delay(1_000).Wait();
            this.GenerateFolderTree();
            Task.Delay(200).Wait();
            this.GenerateThumbnailCache();
        });
    }

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

    public bool Remove(Metadata metadata)
    {
        if (this.fileManager is null)
        {
            throw new Exception("Library Manager is not initialized.");
        }

        try
        {
            string? sourceFolder =
                Path.GetDirectoryName(metadata.FullPath) ??
                throw new Exception("No source folder for: " + metadata.FullPath);

            // TODO : Remove ToList()
            string searchPattern = string.Concat(metadata.Filename, "*.*");
            var files = Directory.EnumerateFiles(
                sourceFolder,
                searchPattern,
                new EnumerationOptions()
                {
                    IgnoreInaccessible = true,
                    MatchCasing = MatchCasing.PlatformDefault,
                }).ToList();
            foreach (string file in files)
            {
                CrossPlatformRecycle.SendToRecycleBin(file);
            }

            // Remove thumbnail from cache 
            string filename = string.Concat(metadata.Filename, "_META.json");
            string thumbnailKey = Path.Combine(sourceFolder, filename);
            if (this.LoadedThumbnails.ContainsKey(thumbnailKey))
            {
                this.LoadedThumbnails.Remove(thumbnailKey);
            }
            else
            {
                // No thumbnail ? 
                if (Debugger.IsAttached) { Debugger.Break(); }
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            if (Debugger.IsAttached) { Debugger.Break(); }
            return false;
        }
    }

    public bool SaveEdits(Metadata metadata, PostProcessWorkflow workflow)
    {
        // As of today, we can handle only one edit 
        if (this.fileManager is null || this.model is null)
        {
            throw new Exception("Library Manager is not initialized.");
        }

        try
        {
            // Create target folder if needed 
            MetadataFolders metadataFolders = new(metadata);
            string targetFolder = metadataFolders.CreateDirectoryPathIfNeeded(this.libraryFolderPath);
            string? sourceFolder =
                Path.GetDirectoryName(metadata.FullPath) ??
                throw new Exception("No source folder for: " + metadata.FullPath);
            string fileId = workflow.PostProcess.FileUidString;
            string filenameEdit = string.Concat(metadata.Filename, "_EDIT", fileId, ".json");
            string targetPathEdit = Path.Combine(targetFolder, filenameEdit);
            PostProcessParameters postProcessParameters;
            if (File.Exists(targetPathEdit))
            {
                string read = File.ReadAllText(targetPathEdit);
                postProcessParameters = this.fileManager.Deserialize<PostProcessParameters>(read);
                postProcessParameters.Update(workflow);
            }
            else
            {
                postProcessParameters = new PostProcessParameters(workflow);
            }

            string serialized = this.fileManager.Serialize<PostProcessParameters>(postProcessParameters);
            File.WriteAllText(targetPathEdit, serialized);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            if (Debugger.IsAttached) { Debugger.Break(); }
            return false;
        }
    }

    public List<ExistingPostProcessParameters> EnumerateExistingParameters(Metadata metadata)
    {
        if (this.fileManager is null || this.model is null)
        {
            throw new Exception("Library Manager is not initialized.");
        }

        List<ExistingPostProcessParameters> list = [];
        string? targetFolder = Path.GetDirectoryName(metadata.FullPath);
        if (targetFolder is null)
        {
            return list;
            // throw new Exception("No source folder for: " + metadata.FullPath);
        }

        string filenameEditPattern = string.Concat(metadata.Filename, "_EDIT", "*", ".json");
        var editFiles = Directory.EnumerateFiles(
            targetFolder,
            filenameEditPattern,
            new EnumerationOptions()
            {
                IgnoreInaccessible = true,
                MatchCasing = MatchCasing.PlatformDefault,
                RecurseSubdirectories = false
            });

        foreach (string editFile in editFiles)
        {
            try
            {
                string fileUid = editFile.Replace(metadata.Filename, string.Empty);
                fileUid = fileUid.Replace("_EDIT", string.Empty);
                fileUid = fileUid.Replace(".json", string.Empty);
                Debug.WriteLine(" " + editFile + " " + fileUid);
                string read = File.ReadAllText(editFile);
                var postProcessParameters = this.fileManager.Deserialize<PostProcessParameters>(read);
                ExistingPostProcessParameters existingPostProcessParameters = new(fileUid, postProcessParameters);
                list.Add(existingPostProcessParameters);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

                // Swallow : Do nothing 
            }
        }

        return list;
    }

    public void SaveMetadata(Metadata metadata)
    {
        if (this.fileManager is null || this.model is null)
        {
            throw new Exception("Library Manager is not initialized.");
        }

        try
        {
            MetadataFolders metadataFolders = new(metadata);
            string targetFolder = metadataFolders.CreateDirectoryPathIfNeeded(this.libraryFolderPath);
            string filenameMetadata = metadata.Filename + "_META.json";
            string targetPathMetadata = Path.Combine(targetFolder, filenameMetadata);
            string serialized = this.fileManager.Serialize<Metadata>(metadata);
            File.WriteAllText(targetPathMetadata, serialized);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            if (Debugger.IsAttached) { Debugger.Break(); }
        }
    }

    public void LoadHdImages(List<string> pathList)
    {
        Parallel.For(0, pathList.Count, index =>
        {
            string path = pathList[index];
            if (this.LoadedHdImages.ContainsKey(path))
            {
                return;
            }

            if (0 == index % 2)
            {
                // throttle 
                Task.Delay(40).Wait();
            }

            LoadedImage? loadedHdImage = ImageLoader.LoadHdImage(path);
            if (loadedHdImage is not null)
            {
                if (loadedHdImage.JpgThumbnail is byte[] imageBytes && loadedHdImage.Metadata is not null)
                {
                    lock (this.LoadedHdImages)
                    {
                        this.LoadedHdImages.Add(path, loadedHdImage);
                    }
                }

                // throttle 
                Task.Delay(40).Wait();
            }
        });
    }
}