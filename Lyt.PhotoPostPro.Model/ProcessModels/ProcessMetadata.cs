namespace Lyt.PhotoPostPro.Model.ProcessModels;

public sealed class ProcessMetadata
{
    public ProcessMetadata(
        string fullPath,
        int width, 
        int height,
        IReadOnlyList<MetadataExtractor.Directory>? directories ) 
    {
        this.FullPath = fullPath;
        this.Width = width;
        this.Height = height;

        this.ExifMetadata = new();
        if ( directories is not null && directories.Count > 0)
        {
            this.PopulateExifMetadata(directories);
        }
    }

    public string FullPath { get; private set;  }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public bool HasExifMetadata { get; private set; }

    public NestedDictionary<string, string, string> ExifMetadata { get; private set; }

    private void PopulateExifMetadata(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        this.ExifMetadata.Clear();

        // TODO : Populate 

        this.HasExifMetadata = true; 
    } 
}
