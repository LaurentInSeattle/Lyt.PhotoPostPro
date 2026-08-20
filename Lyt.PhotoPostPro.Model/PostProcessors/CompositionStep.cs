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

    public override void Initialize(Image<RgbaHalf> originalImage)
    {
        this.X = 0;
        this.Y = 0;
        this.Dx = originalImage.Width;
        this.Dy = originalImage.Height;
        this.OriginalDx = originalImage.Width;
        this.OriginalDy = originalImage.Height;
    }

    protected override void SetIdentity()
        => base.IsIdentity =
            this.X == 0 &&
            this.Y == 0 &&
            this.Dx == this.OriginalDx &&
            this.Dy == this.OriginalDy;

    public override void PerformStep(PostProcessParameters ppp)
        => this.Crop(ppp.CompositionX, ppp.CompositionY, ppp.CompositionDx, ppp.CompositionDy, withFrame: false);

    public override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    internal Frame? Crop(int x, int y, int dx, int dy, bool withFrame = true)
    {
        this.X = x;
        this.Y = y;
        this.Dx = dx;
        this.Dy = dy;
        this.SetIdentity();
        return this.Transform(withFrame);
    }

    internal override Frame? Transform(bool withFrame = true)
        => base.DoTransform((clone) =>
        {
            var cropRectangle = new Rectangle(this.X, this.Y, this.Dx, this.Dy);
            clone.Mutate(x => x.Crop(cropRectangle));
        }, recalculateHistograms: false, withFrame);

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
        this.SetIdentity();
    }
}
