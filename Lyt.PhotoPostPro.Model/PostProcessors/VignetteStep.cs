namespace Lyt.PhotoPostPro.Model.PostProcessors;

public class VignetteStep(PostProcessWorkflow postProcessWorkflow) : 
    PostProcessStep(postProcessWorkflow, PostProcessStep.VignetteStepName)
{
    public float Top { get; set; }

    public float Bottom { get; set; }

    public float Left { get ; set; }

    public float Right { get; set; }

    public float Lightness { get; set; }

    public override void Initialize(Image<Rgb48> _) => this.Clear();

    public override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    public override Frame? Transform(bool withFrame = true)
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        bool isChanged =
            Math.Abs(this.Top) > 0.001 ||
            Math.Abs(this.Bottom) > 0.001 ||
            Math.Abs(this.Left) > 0.001 ||
            Math.Abs(this.Right) > 0.001 ||
            Math.Abs(this.Lightness) > 0.001 ;
        var clone = this.SourceImage.Clone();
        if (isChanged)
        {
            clone.Vignette(this.Top, this.Bottom, this.Left, this.Right, this.Lightness);
            PostProcessStep.RecalculateHistograms(clone);
        }

        this.ResultImage = isChanged ? clone : this.SourceImage;
        return withFrame ? clone.ToFrame() : null;
    }

    internal Frame? Vignette(float top, float bottom, float left, float right, float lightness)
    {
        this.Top = top;
        this.Bottom = bottom;
        this.Left = left;
        this.Right = right;
        this.Lightness = lightness;
        return this.Transform(withFrame: true);
    }

    private void Clear()
    {
        this.Top = 0.0f;
        this.Bottom = 0.0f;
        this.Left = 0.0f;
        this.Right = 0.0f;
        this.Lightness = 0.0f;
    }
}