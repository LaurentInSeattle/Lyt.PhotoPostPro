namespace Lyt.PhotoPostPro.Model.ProjectModels;

public sealed class ProjectMetadata
{
    public ProjectMetadata() { /* Required for serialization */ }

    public string Id { get; set; } = FilenamesMgr.NewShortId();

    public string Name { get; set; } = string.Empty;

    public DateTime Created { get; set; } = DateTime.Now;

    public DateTime LastUpdated { get; set; } = DateTime.Now;

    public string SourceFolderPath { get; set; } = string.Empty;

    public int ImageCount { get; set; } = 0;

    public bool IsSingleImage { get; set; } = false;

    public string ThumbnailPath { get; set; } = string.Empty;

    public bool IsInvalid
        =>
            string.IsNullOrWhiteSpace(this.Name) ||
            string.IsNullOrWhiteSpace(this.SourceFolderPath);


    public bool Validate(out string errorMessageKey)
    {
        errorMessageKey = string.Empty;
        if (string.IsNullOrWhiteSpace(this.Name))
        {
            errorMessageKey = "Model.Project.Name";
            return false;
        }

        if (string.IsNullOrWhiteSpace(this.SourceFolderPath))
        {
            errorMessageKey = "Model.Project.FolderPath";
            return false;
        }

        DirectoryInfo directoryInfo = new(this.SourceFolderPath);
        if (!directoryInfo.Exists)
        {
            errorMessageKey = "Model.Project.FolderPath";
        }

        return true;
    }
}
