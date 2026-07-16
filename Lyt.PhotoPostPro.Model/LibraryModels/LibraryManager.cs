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

    public string LibraryFolderPath => this.libraryFolderPath;

    public string ExportsFolderPath => this.exportsFolderPath;

    public void Initialize(FileManagerModel fileManagerModel)
    {
        this.fileManager = fileManagerModel;
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

        bool AddDownloadedFile(Metadata file)
        {
            try
            {
                if (!File.Exists(file.FullPath))
                {
                    throw new Exception("No such file: " + file.FullPath);
                }

                // Create target folder if needed 
                MetadataFolders metadataFolders = new(file);
                string targetFolder = metadataFolders.CreateDirectoryPathIfNeeded(this.libraryFolderPath);

                // Move main file 
                string targetFilename = Path.GetFileName(file.FullPath);
                string targetPath = Path.Combine(targetFolder, targetFilename);
                File.Move(file.FullPath, targetPath, overwrite: true);

                // Move thumbnail file 
                string? sourceFolder = Path.GetDirectoryName(file.FullPath);
                if (sourceFolder is null)
                {
                    throw new Exception("No source folder for: " + file.FullPath);
                }

                string filenameThumbnail = file.Filename + "_THUMB.jpg";
                string targetPathThumbnail = Path.Combine(targetFolder, filenameThumbnail);
                string sourcePathThumbnail = Path.Combine(sourceFolder, filenameThumbnail);
                File.Move(sourcePathThumbnail, targetPathThumbnail, overwrite: true);

                // Verify main file 
                FileInfo fileInfo = new(targetPath);
                if (!fileInfo.Exists)
                {
                    throw new Exception("Failed to move file" + file.FullPath);
                }

                if (fileInfo.Length != file.Length)
                {
                    throw new Exception("Failed to verify file move" + file.FullPath);
                }

                // update metadata 
                file.HasMovedTo(targetPath);

                // Finally serialize and save metadata 
                string filenameMetadata = file.Filename + "_META.json";
                string targetPathMetadata = Path.Combine(targetFolder, filenameMetadata);
                string serialized = this.fileManager.Serialize<Metadata>(file);
                File.WriteAllText(targetPathMetadata, serialized);

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

            string filenameThumbnail = metadata.Filename + "_THUMB.jpg";
            string pathThumbnail = Path.Combine(folderPath, filenameThumbnail);
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
        Thread lowPriorityThread = new(start);
        lowPriorityThread.Priority = ThreadPriority.Lowest;
        lowPriorityThread.IsBackground = true;
        lowPriorityThread.Start(this);
    }
}
