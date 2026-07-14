namespace Lyt.PhotoPostPro.Model.LibraryModels;

using System.Diagnostics.Eventing.Reader;

// To avoid namespace conflicts for 'Path' 
using System.IO;

public sealed class LibraryManager
{
    private readonly string libraryFolderPath;
    private FileManagerModel? fileManager;

    public LibraryManager()
    {
        string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        this.libraryFolderPath = Path.Combine(pictures, PhotoPostProModel.PhotoPostProAppName, "Library");
        if (!Directory.Exists(this.libraryFolderPath))
        {
            Directory.CreateDirectory(this.libraryFolderPath);
        }
    }

    public void Initialize(FileManagerModel fileManagerModel) => this.fileManager = fileManagerModel;

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

    public bool AddDroppedFile(Metadata file)
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

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            //lock (exceptions)
            //{
            //    ++errors;
            //    exceptions.Add(ex);
            //}

            return false;
        }
    }
}
