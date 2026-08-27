namespace Lyt.PhotoPostPro.Model.Loader;

public sealed  class ImageStatistics(ImageKind kind)
{
    public ImageKind Kind { get; private set; } = kind;

    public int FileCount { get; private set; } // = 0;

    public float SizeOnDiskMB { get; private set; } // = 0.0f;

    public List<string> Paths { get; private set; } = []; 

    internal void Update(string path, float sizeOnDiskMB)
    {
        ++ this.FileCount;
        this.SizeOnDiskMB += sizeOnDiskMB; 
        this.Paths.Add(path);
    }
}