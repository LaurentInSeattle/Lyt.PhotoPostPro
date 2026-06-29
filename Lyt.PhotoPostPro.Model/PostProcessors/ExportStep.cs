namespace Lyt.PhotoPostPro.Model.PostProcessors;

public class ExportStep(PostProcessWorkflow postProcessWorkflow) : 
    PostProcessStep(postProcessWorkflow, PostProcessStep.ExportStepName)
{
    public override void Initialize(Image<Rgb48> originalImage) { }

    // DO NOT call the base class or else it will call Transform 
    public override void Activate(WorkflowUpdateKind workflowUpdateKind) => this.Reset(); 

    public override Frame? Reset()
    {
        this.Clear();

        if (this.SourceImage is null)
        {
            return null;
        }

        // Pick the possibly rotated or mirrored from the first step as 
        // the source image so that comparisons can be made 
        // The result image from the previous step is our current source
        // so we clone it and that becomes our result image... 
        var clone = this.SourceImage.Clone();
        this.SourceImage = this.PostProcessWorkflow.Steps[0].ResultImage;
        this.ResultImage = clone;
        return this.ResultImage.ToFrame() ;
    }

    internal Frame? Export(ExportParameters exportParameters) 
    {
        // TODO 
        if (this.ResultImage is null)
        {
            return null;
        }

        try
        {
            var encoder = new JpegEncoder() { Quality = 95 };
            string sourcePath = this.PostProcessWorkflow.PostProcess.SourceFilePath;
            FileInfo fi = new(sourcePath);
            string extension = ".jpg";
            string filename = Path.GetFileNameWithoutExtension(fi.Name);
            string newPath = fi.DirectoryName!;
            string exportPath =
                Path.Combine(newPath, filename + "_" + FileManagerModel.TimestampString() + extension);
            this.ResultImage.Save(exportPath, encoder);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            if (Debugger.IsAttached) Debugger.Break();
            return null;
        }

        return this.ResultImage.ToFrame();
    }

    public override Frame? Transform(bool withFrame = true) => this.Reset(); 

    private void Clear()
    {
    }
}
