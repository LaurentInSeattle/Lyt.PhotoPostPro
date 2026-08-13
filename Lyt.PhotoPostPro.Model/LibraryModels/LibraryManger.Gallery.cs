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
            if (!file.IsReadable())
            {
                continue; 
            } 

            FileInfo fileInfo = new(file);
            if (fileInfo.Length <= GalleryFileMinimumLength)
            {
                // Ignore small files
                continue;
            }

            this.GalleryContent.Add(file);
        }

        this.dispatcher.OnIdle(this.LoadGalleryFiles); 
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

    private byte[]? LoadGalleryFile(string imagePath)
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

    private void LoadGalleryFiles()
    {
        // TODO 
    } 
}
