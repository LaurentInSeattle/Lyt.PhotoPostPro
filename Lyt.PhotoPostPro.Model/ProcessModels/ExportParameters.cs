namespace Lyt.PhotoPostPro.Model.ProcessModels;

public sealed class ExportParameters
{
    public List<ExportImageParameters> ExportImageParameters { get; set; } = [];

    public ExportParameters()
    {
        // Adds the default export
        this.ExportImageParameters.Add(new ExportImageParameters());
    }

}
