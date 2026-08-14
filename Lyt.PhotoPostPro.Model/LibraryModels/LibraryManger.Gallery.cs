namespace Lyt.PhotoPostPro.Model.LibraryModels;

using System.IO;

public sealed partial class LibraryManager
{
    public const int GalleryFileMinimumLength = 4 * 1024 * 1024;

    public List<string> GalleryContent { get; private set; } = [];

    public void InitializeGallery()
    {
        if (this.dispatcher is null)
        {
            throw new Exception("Library not properly initialized.");
        }

        // Enumerate files in gallery folder and filter them 
        var files =
            Directory.EnumerateFiles(this.galleryFolderPath, "*.jpg", new EnumerationOptions()
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true,
                MaxRecursionDepth = 2,
            });
        foreach (string file in files)
        {
            if (this.CanAddFile(file))
            {
                this.GalleryContent.Add(file);
            }
        }

        // Preload first two images if any present 
        if (this.GalleryContent.Count > 0)
        {
            this.dispatcher.OnIdle(this.LoadFirstGalleryFile);
        }
    }

    public bool CanAddFile(string filePath)
    {
        if (!filePath.IsReadable())
        {
            return false;
        }

        FileInfo fileInfo = new(filePath);
        if (fileInfo.Length <= GalleryFileMinimumLength)
        {
            // Ignore small files
            return false;
        }

        return true;
    }

    public byte[]? GetGalleryImage(string nowShowing)
    {
        try
        {
            if (this.GalleryImages.TryGetValue(nowShowing, out byte[]? imageBytes))
            {
                return imageBytes;
            }

            return this.LoadGalleryFile(nowShowing);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return null;
        }
    }

    public byte[]? LoadGalleryFile(string imagePath)
    {
        try
        {
            byte[] imageBytes = File.ReadAllBytes(imagePath);
            this.GalleryImages.Add(imagePath, imageBytes);
            return imageBytes;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return null;
        }
    }

    private void LoadFirstGalleryFile()
    {
        if (this.dispatcher is null)
        {
            throw new Exception("Library not properly initialized.");
        }

        this.LoadGalleryFile(this.GalleryContent[0]);
        if (this.GalleryContent.Count > 1)
        {
            this.dispatcher.OnIdle(this.LoadNextGalleryFile);
        }
    }

    private void LoadNextGalleryFile() => this.LoadGalleryFile(this.GalleryContent[1]);
}
