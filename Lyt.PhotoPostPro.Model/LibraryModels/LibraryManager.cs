namespace Lyt.PhotoPostPro.Model.LibraryModels;

// To avoid namespace conflicts for 'Path' 
using System.IO;

public sealed class LibraryManager
{
    public const string LibraryFolderName = "Library";
    public const string ExportsFolderName = "Exports";

    private readonly string libraryFolderPath;
    private readonly string exportsFolderPath;

    private FileManagerModel? fileManager;

    public LibraryManager()
    {
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

        this.LoadedThumbnails = [];
    }

    public Dictionary<string, LoadedThumbnail> LoadedThumbnails { get; private set; }

    public FolderTree? FolderTree { get; private set; }

    public bool IsLoading { get; private set; }

    public string LibraryFolderPath => this.libraryFolderPath;

    public string ExportsFolderPath => this.exportsFolderPath;

    public void Initialize(FileManagerModel fileManagerModel)
    {
        this.fileManager = fileManagerModel;
        this.IsLoading = true;
        this.GenerateInitialFolderTree();
    }

    public bool AddDownloadedFiles(List<Metadata> files)
    {
        int errors = 0;
        List<Exception> exceptions = [];
        if (this.fileManager is null)
        {
            throw new Exception("Library Manager is not initialized.");
        }

        bool AddDownloadedFile(Metadata metadata)
        {
            try
            {
                if (!File.Exists(metadata.FullPath))
                {
                    throw new Exception("No such file: " + metadata.FullPath);
                }

                // Create target folder if needed 
                MetadataFolders metadataFolders = new(metadata);
                string targetFolder = metadataFolders.CreateDirectoryPathIfNeeded(this.libraryFolderPath);

                // Move main file 
                string targetFilename = Path.GetFileName(metadata.FullPath);
                string targetPath = Path.Combine(targetFolder, targetFilename);
                File.Move(metadata.FullPath, targetPath, overwrite: true);

                // Move thumbnail file 
                string? sourceFolder = Path.GetDirectoryName(metadata.FullPath);
                if (sourceFolder is null)
                {
                    throw new Exception("No source folder for: " + metadata.FullPath);
                }

                string filenameThumbnail = metadata.Filename + "_THUMB.jpg";
                string targetPathThumbnail = Path.Combine(targetFolder, filenameThumbnail);
                string sourcePathThumbnail = Path.Combine(sourceFolder, filenameThumbnail);
                File.Move(sourcePathThumbnail, targetPathThumbnail, overwrite: true);

                // Verify main file 
                FileInfo fileInfo = new(targetPath);
                if (!fileInfo.Exists)
                {
                    throw new Exception("Failed to move file" + metadata.FullPath);
                }

                if (fileInfo.Length != metadata.Length)
                {
                    throw new Exception("Failed to verify file move" + metadata.FullPath);
                }

                // update metadata 
                metadata.HasMovedTo(targetPath);

                // Finally serialize and save metadata 
                string filenameMetadata = metadata.Filename + "_META.json";
                string targetPathMetadata = Path.Combine(targetFolder, filenameMetadata);
                string serialized = this.fileManager.Serialize<Metadata>(metadata);
                File.WriteAllText(targetPathMetadata, serialized);

                // Now update in memory data structures 
                // Add thumbnail to cache 
                byte[] thumbnail = File.ReadAllBytes(targetPathThumbnail);
                LoadedThumbnail loadedThumbnail = new(Metadata: metadata, ImageBytes: thumbnail);
                this.LoadedThumbnails.Add(targetPathMetadata, loadedThumbnail);

                // Update folder tree 
                this.FolderTree?.UpdateOnFileAdded(metadata, targetPathMetadata);

                return true;
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


        // TODO: Return more details 
        return errors == 0;
    }

    public bool AddDroppedFile(LoadedImage loadedImage)
    {
        if ((this.fileManager is null) || (this.FolderTree is null))
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
            byte[] thumbnail = loadedImage.JpgThumbnail!;

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
            File.WriteAllBytes(targetPathThumbnail, thumbnail);

            // Verify main file 
            FileInfo fileInfo = new(targetPath);
            if (!fileInfo.Exists)
            {
                throw new Exception("Failed to copy file" + metadata.FullPath);
            }

            if (fileInfo.Length != metadata.Length)
            {
                throw new Exception("Failed to verify file copy" + metadata.FullPath);
            }

            // update metadata 
            metadata.HasMovedTo(targetPath);

            // Finally serialize and save metadata 
            string filenameMetadata = metadata.Filename + "_META.json";
            string targetPathMetadata = Path.Combine(targetFolder, filenameMetadata);
            string serialized = this.fileManager.Serialize<Metadata>(metadata);
            File.WriteAllText(targetPathMetadata, serialized);

            // Now update in memory data structures 
            // Add thumbnail to cache 
            LoadedThumbnail loadedThumbnail = new(Metadata: metadata, ImageBytes: thumbnail);
            this.LoadedThumbnails.Add(targetPathMetadata, loadedThumbnail);

            // Update folder tree 
            this.FolderTree.UpdateOnFileAdded(metadata, targetPathMetadata);

            // All good 
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return false;
        }
    }

    public void LoadThumbnails()
    {
        if (this.FolderTree is null)
        {
            return;
        }

        foreach (var year in this.FolderTree.YearFolders)
        {
            foreach (var month in year.MonthFolders)
            {
                foreach (var day in month.DayFolders)
                {
                    foreach (string path in day.MetadataFiles)
                    {
                        var thumbnail = this.LoadThumbnail(path);
                        if (thumbnail is not null)
                        {
                            this.LoadedThumbnails.Add(path, thumbnail);
                            Debug.WriteLine(" Loaded Thumbnail: " + path);
                        }

                        // NEEDED ? 
                        //
                        // Throttle the process; Wait a bit
                        // Task.Delay(10).Wait();
                    }
                }
            }
        }

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

            string key = pathThumbnail.Replace(endsWith, "_META.json");

#if DEBUG
            if (!this.LoadedThumbnails.ContainsKey(key))
            {
                if (Debugger.IsAttached) { Debugger.Break(); }
                throw new Exception("No folder key");
            }
#endif 
            this.LoadedThumbnails[key] = loadedThumbnail;
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

            byte[] imageBytes = File.ReadAllBytes(pathThumbnail);
            return new LoadedThumbnail(metadata, imageBytes);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return null;
        }
    }

    public void GenerateFolderTree()
    {
        try
        {
            var folderTree = FolderTree.Generate(this.libraryFolderPath);
            this.FolderTree = folderTree;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            if (Debugger.IsAttached) { Debugger.Break(); }
        }
    }

    public void GenerateInitialFolderTree()
    {
        Task.Run(() =>
        {
            // wait a bit so that we dont delay app starting up 
            Task.Delay(2_000).Wait();
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

    public bool SaveEdits(Metadata metadata, PostProcessWorkflow workflow)
    {
        if (this.fileManager is null)
        {
            throw new Exception("Library Manager is not initialized.");
        }

        try
        {
            // Create target folder if needed 
            MetadataFolders metadataFolders = new(metadata);
            string targetFolder = metadataFolders.CreateDirectoryPathIfNeeded(this.libraryFolderPath);
            string? sourceFolder = Path.GetDirectoryName(metadata.FullPath);
            if (sourceFolder is null)
            {
                throw new Exception("No source folder for: " + metadata.FullPath);
            }

            string filenameEdit = metadata.Filename + "_EDIT.json";
            string targetPathEdit = Path.Combine(targetFolder, filenameEdit);
            var stepsParameters = PostProcessParameters.FromPostProcessWorkflow(workflow);
            string serialized = this.fileManager.Serialize<PostProcessParameters>(stepsParameters);
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
}
