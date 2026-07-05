namespace Lyt.PhotoPostPro.Model.PostProcessors;

public class FiltersStep(PostProcessWorkflow postProcessWorkflow) : 
    PostProcessStep(postProcessWorkflow, PostProcessStep.FiltersStepName)
{
    public enum Filter
    {
        Grayscale,
        Sepia,
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

        // Image is unchanged if the amount is 1.0, for all filters.
        bool isChanged = Math.Abs(1.0 - this.Amount) > 0.001 ;
        var clone = this.SourceImage.Clone();
        if (isChanged)
        {
            switch (this.SelectedFilter)
            {
                default:
                case Filter.Grayscale:
                    clone.Grayscale(this.Amount);
                    break;

                case Filter.Sepia:
                    clone.Sepia(this.Amount);
                    break;
            }

            PostProcessStep.RecalculateHistograms(clone);
        }

        this.ResultImage = isChanged ? clone : this.SourceImage;
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

    private void Clear()
    {
        this.SelectedFilter = Filter.Grayscale;
        this.Amount = 0.0f;
    }
}