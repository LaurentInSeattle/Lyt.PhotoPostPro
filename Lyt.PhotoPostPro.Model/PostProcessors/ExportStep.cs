namespace Lyt.PhotoPostPro.Model.PostProcessors;

public class ExportStep(PostProcessWorkflow postProcessWorkflow) : 
    PostProcessStep(postProcessWorkflow, PostProcessStep.ExportStepName)
{
    public sealed class  ExportParameters
    {
    }

    public override void Initialize(Image<Rgb48> originalImage)
    {
    }

    public override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    internal Frame? Export(ExportParameters exportParameters)
    {
        return this.Transform();
    }

    public override Frame? Transform(bool withFrame = true)
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        try
        {
            var clone = this.SourceImage.Clone();
            var encoder = new JpegEncoder() { Quality = 95 };
            string sourcePath = this.PostProcessWorkflow.PostProcess.SourceFilePath; 
            FileInfo fi = new (sourcePath);
            string extension = ".jpg";
            string filename = Path.GetFileNameWithoutExtension(fi.Name);
            string newPath = fi.DirectoryName!;
            string exportPath = 
                Path.Combine(newPath, filename + "_" + FileManagerModel.TimestampString() + extension);
            clone.Save(exportPath, encoder);
            this.ResultImage = clone;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            if (Debugger.IsAttached) Debugger.Break();
            return null;
        } 

        return withFrame ? this.ResultImage.ToFrame() : null;
    }

    public override void Activate(WorkflowUpdateKind workflowUpdateKind)
    {
    }

    private void Clear()
    {
    }
}
