namespace Lyt.PhotoPostPro.Model.PostProcessors;

public class FiltersStep(PostProcessWorkflow postProcessWorkflow) :
    PostProcessStep(postProcessWorkflow, PostProcessStep.FiltersStepName)
{
    public enum Filter
    {
        None,
        Grayscale,
        Sepia,
        Vignette,
        BlackWhite,
        Kodachrome,
        Lomograph,
        Polaroid,
    }

    public Filter SelectedFilter { get; set; }

    // All filter have a single amount parameter, we can use a single property for all of them.
    public float Amount { get; set; }

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

        // Image is unchanged if the amount is 0.0, for Grayscale and Sepia
        // Other filters are always applied without parameter 
        var clone = this.SourceImage.Clone();
        switch (this.SelectedFilter)
        {
            default:
            case Filter.None:
                break; 

            case Filter.Grayscale:
                clone.Grayscale(this.Amount);
                break;

            case Filter.Sepia:
                clone.Sepia(this.Amount);
                break;

            case Filter.Vignette:
                clone.Vignette();
                break;

            case Filter.BlackWhite:
                clone.BlackWhite();
                break;

            case Filter.Kodachrome:
                clone.Kodachrome();
                break;

            case Filter.Lomograph:
                clone.Lomograph();
                break;

            case Filter.Polaroid:
                clone.Polaroid();
                break;
        }

        PostProcessStep.RecalculateHistograms(clone);

        this.ResultImage = clone;
        return withFrame ? clone.ToFrame() : null;
    }

    internal Frame? Grayscale(float grayscaleAmount)
    {
        this.SelectedFilter = Filter.Grayscale;
        this.Amount = grayscaleAmount;
        return this.Transform(withFrame: true);
    }

    internal Frame? Sepia(float sepiaAmount)
    {
        this.SelectedFilter = Filter.Sepia;
        this.Amount = sepiaAmount;
        return this.Transform(withFrame: true);
    }

    internal Frame? Vignette()
    {
        this.SelectedFilter = Filter.Vignette;
        this.Amount = 0.0f;
        return this.Transform(withFrame: true);
    }

    internal Frame? BlackWhite()
    {
        this.SelectedFilter = Filter.BlackWhite;
        this.Amount = 0.0f;
        return this.Transform(withFrame: true);
    }

    internal Frame? Kodachrome()
    {
        this.SelectedFilter = Filter.Kodachrome;
        this.Amount = 0.0f;
        return this.Transform(withFrame: true);
    }

    internal Frame? Lomograph()
    {
        this.SelectedFilter = Filter.Lomograph;
        this.Amount = 0.0f;
        return this.Transform(withFrame: true);
    }

    internal Frame? Polaroid()
    {
        this.SelectedFilter = Filter.Polaroid;
        this.Amount = 0.0f;
        return this.Transform(withFrame: true);
    }

    private void Clear()
    {
        this.SelectedFilter = Filter.Grayscale;
        this.Amount = 0.0f;
    }
}