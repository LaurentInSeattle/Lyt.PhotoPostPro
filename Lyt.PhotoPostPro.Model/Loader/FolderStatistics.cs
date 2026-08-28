namespace Lyt.PhotoPostPro.Model.Loader;

public sealed class FolderStatistics(string path)
{
    public string Path { get; private set; } = path;

    public bool Success { get; private set; } = true;

    public string Message { get; private set; } = string.Empty;

    public int TotalFileCount { get; private set; } // = 0;

    public int ImageFileCount { get; private set; } // = 0;

    public List<ImageStatistics> ImageStatistics { get; private set; } =
        // In the order of the enum 
        //      Jpeg, 
        //      Heic, 
        //      Raw,
        //      OtherImages,
        //      Movie,
        //      Unrecognized,
        [
            new ImageStatistics ( ImageKind.Jpeg),
            new ImageStatistics ( ImageKind.Heic),
            new ImageStatistics ( ImageKind.Raw),
            new ImageStatistics ( ImageKind.OtherImages),
            new ImageStatistics ( ImageKind.Movie),
            new ImageStatistics ( ImageKind.Unrecognized),
        ];

    public void Fail(string message)
    {
        this.Success = false;
        this.Message = message;
    }

    public void Add(ImageKind kind, string path, float sizeOnDiskMB)
    {
        var stats = this.ImageStatistics[(int)kind];
        stats.Update(path, sizeOnDiskMB);
    }

    public void Pack()
    {
        this.ImageStatistics =
             (from stats in this.ImageStatistics where stats.FileCount > 0 select stats).ToList();
        this.TotalFileCount =
             (from stats in this.ImageStatistics where stats.FileCount > 0 select stats.FileCount).Sum();
        this.ImageFileCount =
             (from stats in this.ImageStatistics 
              where stats.FileCount > 0 && (stats.Kind != ImageKind.Movie ) && (stats.Kind != ImageKind.Unrecognized)
              select stats.FileCount).Sum();
    }
}
