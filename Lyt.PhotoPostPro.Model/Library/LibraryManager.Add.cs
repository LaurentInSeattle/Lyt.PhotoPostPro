namespace Lyt.PhotoPostPro.Model.Library;

// To avoid namespace conflicts for 'Path' 
using System.IO;

public sealed partial class LibraryManager
{
    public bool IsAlreadyInLibrary(Metadata metadata)
    {
        // Check if the target library folder is existing 
        MetadataFolders metadataFolders = new(metadata);
        string targetFolder = metadataFolders.WouldBeDirectoryPath(this.libraryFolderPath);
        if (!Directory.Exists(targetFolder))
        {
            return false;
        }

        string sourceFilename = Path.GetFileName(metadata.FullPath);
        string targetPath = Path.Combine(targetFolder, sourceFilename);
        if (!File.Exists(targetPath))
        {
            return false;
        } 

        return true;
    }

    public bool AddDownloadedFiles(List<Metadata> files)
    {
        if ((this.CapturedFolderTree is null) ||
            (this.UnratedFolderTree is null))
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

                if (this.IsAlreadyInLibrary(metadata))
                {
                    throw new Exception("Already present in library: " + metadata.FullPath);
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
        this.UnratedFolderTree.Sort();

        // Notify UI 
        // Send only one message should be enough - See how the message is processed in the library view
        new FolderTreeUpdatedMessage(FolderTreeKind.Unrated).Publish();

        // TODO: Return more details 
        return errors == 0;
    }

    public bool AddDroppedFile(LoadedImage loadedImage)
    {
        if ((this.fileManager is null) ||
            (this.CapturedFolderTree is null) ||
            (this.UnratedFolderTree is null))
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

            if (this.IsAlreadyInLibrary(metadata))
            {
                throw new Exception("Already present in library: " + metadata.FullPath);
            }

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
                this.UnratedFolderTree.Sort();
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
        if ((this.CapturedFolderTree is null) ||
            (this.UnratedFolderTree is null))
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
            var jsonTypeInfo = AppJsonContext.Default.Metadata;
            string serialized = this.fileManager.Serialize(metadata, jsonTypeInfo);
            File.WriteAllText(targetPathMetadata, serialized);

            // Now update in memory data structures 
            // There can be multiple threads moving files so we need to lock them
            lock (this.lockObject)
            {
                // Add thumbnail to cache 
                LoadedThumbnail loadedThumbnail = new(metadata, thumbnailImageBytes);

                // Prevents trouble if we already have it, usually because of some previous aborted process
                if (!this.LoadedThumbnails.ContainsKey(targetPathMetadata))
                {
                    this.LoadedThumbnails.Add(targetPathMetadata, loadedThumbnail);
                }

                // Update folder trees 
                _ = this.CapturedFolderTree.UpdateOnFileAdded(DateKind.Captured, metadata, targetPathMetadata, doSort: false);
                _ = this.UnratedFolderTree.UpdateOnFileAdded(DateKind.Added, metadata, targetPathMetadata, doSort: false);
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
            var dayFolder = this.EditedFolderTree.UpdateOnFileAdded(DateKind.Edited, metadata, metadataFilePath);

            // Notify UI, if needed
            if (dayFolder is not null)
            {
                new FolderTreeUpdatedMessage(FolderTreeKind.Edited, dayFolder).Publish();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    public List<string> FindFilesUnratedOrEdited(
        DayFolder? selectedDay, MonthFolder? selectedMonth, YearFolder selectedYear,
        bool forUnrated, out int zeroStarCount)
    {
        zeroStarCount = 0;
        List<string> list = new(this.LoadedThumbnails.Count);

        bool checkDay = selectedDay is not null;
        // ! Checked by check day 
        int sDay = checkDay ? selectedDay!.Day : -1;

        bool checkMonth = selectedMonth is not null;
        // ! Checked by check month
        int sMonth = checkMonth ? selectedMonth!.Month : -1;
        int sYear = selectedYear.Year;

        List<LoadedThumbnail> loadedThumbnailsList = new(list.Count);
        foreach (var thumbnail in this.LoadedThumbnails)
        {
            Metadata metadata = thumbnail.Value.Metadata;
            DateTime date =
                forUnrated ?
                    metadata.AddedToLibraryUTC.ToLocalTime().Date :
                    metadata.LastEditedUTC.ToLocalTime().Date;
            if (date.Year != sYear)
            {
                continue;
            }

            if (checkMonth && date.Month != sMonth)
            {
                continue;
            }

            if (checkDay && date.Day != sDay)
            {
                continue;
            }

            if (forUnrated && metadata.Rating > 0)
            {
                continue;
            }

            if (metadata.Rating == 0)
            {
                ++zeroStarCount;
            }

            loadedThumbnailsList.Add(thumbnail.Value);
        }

        return (from thumb in loadedThumbnailsList
                orderby thumb.Metadata.Captured ascending
                select thumb.Metadata.MetadataFullPath()).ToList();
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