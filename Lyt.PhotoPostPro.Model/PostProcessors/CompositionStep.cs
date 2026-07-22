namespace Lyt.PhotoPostPro.Model.PostProcessors;

public class CompositionStep(PostProcessWorkflow postProcessWorkflow) : 
    PostProcessStep(postProcessWorkflow, PostProcessStep.CompositionStepName)
{
    public int X { get; set; }

    public int Y { get; set; }

    public int Dx { get; set; }

    public int Dy { get; set; }

    public int OriginalDx { get; set; }

    public int OriginalDy { get; set; }

    public override void Initialize(Image<HalfVector4> originalImage)
    {
        this.X = 0;
        this.Y = 0;
        this.Dx = originalImage.Width;
        this.Dy = originalImage.Height;
        this.OriginalDx = originalImage.Width;
        this.OriginalDy = originalImage.Height;
    }

    public override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    internal Frame? Crop(int x, int y, int dx, int dy)
    {
        this.X = x;
        this.Y = y;
        this.Dx = dx;
        this.Dy = dy;
        return this.Transform();
    }

    public override Frame? Transform(bool withFrame = true)
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        var cropRectangle = new Rectangle(this.X, this.Y, this.Dx, this.Dy);
        bool isChanged =
            (this.Dx != 0 && this.Dy != 0) &&
            (cropRectangle.Height != this.SourceImage.Height || cropRectangle.Width != this.SourceImage.Width);
        if (isChanged)
        {
            try
            {
                var clone = this.SourceImage.Clone();
                clone.Mutate(x => x.Crop(cropRectangle));
                this.ResultImage = clone;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                if (Debugger.IsAttached) Debugger.Break();
                return null;
            } 
        }
        else
        {
            this.ResultImage = this.SourceImage;
        }

        return withFrame ? this.ResultImage.ToFrame() : null;
    }

    public override void Activate(WorkflowUpdateKind workflowUpdateKind)
    {
        base.Activate(workflowUpdateKind);

        if (workflowUpdateKind == WorkflowUpdateKind.Back)
        {
            this.ResultImage = this.SourceImage;
        }
    }

    private void Clear()
    {
        this.X = 0;
        this.Y = 0;
        this.Dx = this.OriginalDx;
        this.Dy = this.OriginalDy;
    }
}
