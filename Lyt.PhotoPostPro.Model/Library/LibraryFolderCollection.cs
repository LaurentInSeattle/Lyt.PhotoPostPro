namespace Lyt.PhotoPostPro.Model.Library;

using System.IO;

using Lyt.FileSystem; 

public sealed class LibraryFolderCollection
{
    public LibraryFolderCollection() { /* Required for JSON serialization */ }

    public List<LibraryFolder> AvailableLibraryFolders { get; set; } = [];

    public string SelectedLibraryFolderName { get; set; } = string.Empty;

    public string SelectedLibraryFolderPath
    {
        get
        {
            if (!this.IsValid(this.SelectedLibraryFolderName))
            {
                return LibraryFolder.Default.FullPath;
            }

            LibraryFolder? libraryFolder = this.FromFriendlyName(this.SelectedLibraryFolderName);

            // ! Checked by IsValid above
            return libraryFolder!.FullPath;
        }
    }

    public LibraryFolder? FromFriendlyName(string friendlyName)
        => this.AvailableLibraryFolders.FirstOrDefault(
            s => s.FriendlyName.Equals(friendlyName, StringComparison.InvariantCultureIgnoreCase));

    public bool IsAvailable(string friendlyName)
        => this.FromFriendlyName(friendlyName) is not null;

    // Check if the library folder with the specified friendly name still exists.
    public bool IsValid(string friendlyName)
    {
        var libraryFolder = this.FromFriendlyName(friendlyName);
        if (libraryFolder is null)
        {
            return false;
        }

        // Has been deleted ? or invalid path ?
        if (!Directory.Exists(libraryFolder.FullPath))
        {
            return false;
        }

        // Has been renamed ? 
        // Make sure the library folder contains our application specific subfolders
        if ((!libraryFolder.FullPath.Contains(PhotoPostProModel.PhotoPostProAppName)) ||
            (!libraryFolder.FullPath.Contains(LibraryManager.LibraryFolderName)))
        {
            return false;
        }

        // Has been 'protected' ? 
        if (!libraryFolder.FullPath.IsFolderFullControl())
        {
            return false;
        }

        return true;
    }

    // Create a new library folder entry with the specified friendly name and path.
    // Returns true if the library folder was created successfully, false otherwise.
    // Actual final path will be: {path}\PhotoPostPro\Library
    public bool AddLibraryFolder(string friendlyName, string path)
    {
        try
        {
            if (this.IsAvailable(friendlyName))
            {
                return false;
            }

            if (!Directory.Exists(path))
            {
                return false;
            }

            // We can create a new library folder entry from an existing folder 
            // but if not we also need to ensure that the actual directory structure exists.
            bool wasCreated = false;
            string libraryFolderPath =
                Path.Combine(path, PhotoPostProModel.PhotoPostProAppName, LibraryManager.LibraryFolderName);
            if (!Directory.Exists(libraryFolderPath))
            {
                Directory.CreateDirectory(libraryFolderPath);
                wasCreated = true;
            }

            // Has been 'protected' ? 
            if (!libraryFolderPath.IsFolderFullControl())
            {
                if (wasCreated)
                {
                    // If we created the directory, delete it since we cannot write to it.
                    Directory.Delete(libraryFolderPath);
                }

                return false;
            }

            this.AvailableLibraryFolders.Add(
                new LibraryFolder()
                {
                    FriendlyName = friendlyName,
                    FullPath = libraryFolderPath,
                });

            return true;
        }
        catch (IOException ioex)
        {
            // Thrown if the file is locked by another process or a disk error occurs
            Debug.WriteLine(ioex.ToString());
            return false;
        }
        catch (UnauthorizedAccessException uaex)
        {
            // Thrown if the application lacks OS permissions to read or write a file
            Debug.WriteLine(uaex.ToString());
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Exception in AddLibraryFolder: " + ex.ToString());
            return false;
        }
    }

    public bool DeleteLibraryFolder(string friendlyName)
    {
        try
        {
            if (!this.IsAvailable(friendlyName))
            {
                // Cannot delete non existing
                return false;
            }

            var libraryFolder = this.FromFriendlyName(friendlyName);
            if (libraryFolder is null)
            {
                // Cannot delete non existing
                return false;
            }

            if (libraryFolder.FriendlyName.Equals(LibraryFolder.DefaultLibraryFolderName, StringComparison.InvariantCultureIgnoreCase))
            {
                // Cannot delete the default library folder
                return false;
            }

            // If the library folder being deleted is currently selected ?  
            if (libraryFolder.FriendlyName == this.SelectedLibraryFolderName)
            {
                // Cannot delete the currently selected library folder
                return false;
            }

            this.AvailableLibraryFolders.Remove(libraryFolder);
            return true;
        }
        catch (IOException ioex)
        {
            // Thrown if the file is locked by another process or a disk error occurs
            Debug.WriteLine(ioex.ToString());
            return false;
        }
        catch (UnauthorizedAccessException uaex)
        {
            // Thrown if the application lacks OS permissions to read or write a file
            Debug.WriteLine(uaex.ToString());
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Exception in DeleteLibraryFolder: " + ex.ToString());
            // ("Exception in DeleteLibraryFolder: " + ex.ToString());
            return false;
        }
    }

    public bool SelectLibraryFolder(string friendlyName)
    {
        try
        {
            if (!this.IsAvailable(friendlyName))
            {
                // Cannot select non existing
                return false;
            }

            var libraryFolder = this.FromFriendlyName(friendlyName);
            if (libraryFolder is null)
            {
                // Cannot select non existing
                return false;
            }

            // If the library folder being selected is already currently selected ?  
            string friendlyNameToSelect = libraryFolder.FriendlyName;
            if (friendlyNameToSelect == this.SelectedLibraryFolderName)
            {
                // Cannot select the currently selected library folder
                return false;
            }

            this.SelectedLibraryFolderName = friendlyNameToSelect;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Exception in SelectLibraryFolder: " + ex.ToString());
            // ("Exception in SelectLibraryFolder: " + ex.ToString());
            return false;
        }
    }
}