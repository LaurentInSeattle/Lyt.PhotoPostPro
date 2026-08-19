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
    {
        bool isChanged =
            ppp.CompositionX != 0 ||
            ppp.CompositionY != 0 ||
            ppp.CompositionDx != ppp.CompositionOriginalDx ||
            ppp.CompositionDy != ppp.CompositionOriginalDy;
        if (isChanged)
        {
            _ = this.Crop(
                ppp.CompositionX, ppp.CompositionY,
                ppp.CompositionDx, ppp.CompositionDy);
        }
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
        this.SetIdentity();
        return this.Transform();
    }

    internal override Frame? Transform(bool withFrame = true)
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        if (this.IsIdentity)
        {
            this.ResultImage = this.SourceImage;
        }
        else
        {
            var clone = this.SourceImage.Clone();
            var cropRectangle = new Rectangle(this.X, this.Y, this.Dx, this.Dy);
            clone.Mutate(x => x.Crop(cropRectangle));
            this.ResultImage = clone;
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
        this.SetIdentity();
    }
}
