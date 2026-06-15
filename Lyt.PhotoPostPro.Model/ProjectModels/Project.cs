namespace Lyt.PhotoPostPro.Model.ProjectModels;

public sealed class Project
{
    public Project() { /* Required for serialization */ }

    public ProjectMetadata Metadata { get; set; } = new();

    public List<PostProcess> PostProcesses { get; set; } = new(8);
}
