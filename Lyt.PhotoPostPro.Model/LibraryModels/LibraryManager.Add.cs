namespace Lyt.PhotoPostPro.Model.LibraryModels;

// To avoid namespace conflicts for 'Path' 
using System.IO;

public sealed partial class LibraryManager
{
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

        // Notify UI 
        // Send only one message should be enough - See how the message is processed in the library view
        new FolderTreeUpdatedMessage(FolderTreeKind.Added).Publish();

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
                LoadedThumbnail loadedThumbnail = new(metadata, thumbnailImageBytes);
                this.LoadedThumbnails.Add(targetPathMetadata, loadedThumbnail);

                // Update folder trees 
                this.CapturedFolderTree.UpdateOnFileAdded(DateKind.Captured, metadata, targetPathMetadata, doSort: false);
                this.AddedFolderTree.UpdateOnFileAdded(DateKind.Added, metadata, targetPathMetadata, doSort: false);
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

            // Notify UI 
            new FolderTreeUpdatedMessage(FolderTreeKind.Edited, dayFolder).Publish();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
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