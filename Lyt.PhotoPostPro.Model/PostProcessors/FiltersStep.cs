namespace Lyt.PhotoPostPro.Model.PostProcessors;

using static Lyt.PhotoPostPro.Model.PostProcessors.SharpenStep;

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

    public override void Initialize(Image<RgbaHalf> _) => this.Clear();

    protected override void SetIdentity()
    {
        if (this.SelectedFilter == Filter.None)
        {
            base.IsIdentity = true;
        }
        else if (
            (this.SelectedFilter == Filter.Grayscale) ||
            (this.SelectedFilter == Filter.Sepia) ||
            (this.SelectedFilter == Filter.Vignette))
        {
            // Image is unchanged if the amount is 0.0, for Vignette, Grayscale and Sepia
            base.IsIdentity = MathF.Abs(this.Amount) < 0.001;
        }
        else
        {
            base.IsIdentity = false;
        }
    }

    public override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    public override void PerformStep(PostProcessParameters ppp)
    {
        switch (ppp.FilterSelectedFilter)
        {
            default:
                break;

            case Filter.Grayscale:
                if (ppp.FilterAmount > 0.001)
                {
                    this.Grayscale(ppp.FilterAmount);
                }
                break;

            case Filter.Sepia:
                if (ppp.FilterAmount > 0.001)
                {
                    this.Sepia(ppp.FilterAmount); ;
                }

                break;

            case Filter.Vignette:
                if (ppp.FilterAmount > 0.001)
                {
                    this.Vignette(ppp.FilterAmount);
                }

                break;

            case Filter.BlackWhite:
                this.BlackWhite();
                break;

            case Filter.Kodachrome:
                this.Kodachrome();
                break;

            case Filter.Lomograph:
                this.Lomograph();
                break;

            case Filter.Polaroid:
                this.Polaroid();
                break;
        }
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

                // All other filters are always applied without parameter 
                case Filter.Vignette:
                    clone.Vignette(this.Amount);
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

            this.ResultImage = clone;
        }

        PostProcessStep.RecalculateHistograms(this.ResultImage);
        return withFrame ? this.ResultImage.ToFrame() : null;
    }

    internal Frame? Grayscale(float grayscaleAmount)
    {
        this.SelectedFilter = Filter.Grayscale;
        this.Amount = grayscaleAmount;
        this.SetIdentity();
        return this.Transform(withFrame: true);
    }

    internal Frame? Sepia(float sepiaAmount)
    {
        this.SelectedFilter = Filter.Sepia;
        this.Amount = sepiaAmount;
        this.SetIdentity();
        return this.Transform(withFrame: true);
    }

    internal Frame? Vignette(float vignetteAmount)
    {
        this.SelectedFilter = Filter.Vignette;
        this.Amount = vignetteAmount;
        this.SetIdentity();
        return this.Transform(withFrame: true);
    }

    internal Frame? BlackWhite()
    {
        this.SelectedFilter = Filter.BlackWhite;
        this.Amount = 0.0f;
        this.SetIdentity();
        return this.Transform(withFrame: true);
    }

    internal Frame? Kodachrome()
    {
        this.SelectedFilter = Filter.Kodachrome;
        this.Amount = 0.0f;
        this.SetIdentity();
        return this.Transform(withFrame: true);
    }

    internal Frame? Lomograph()
    {
        this.SelectedFilter = Filter.Lomograph;
        this.Amount = 0.0f;
        this.SetIdentity();
        return this.Transform(withFrame: true);
    }

    internal Frame? Polaroid()
    {
        this.SelectedFilter = Filter.Polaroid;
        this.Amount = 0.0f;
        this.SetIdentity();
        return this.Transform(withFrame: true);
    }

    private void Clear()
    {
        this.SelectedFilter = Filter.Grayscale;
        this.Amount = 0.0f;
        this.SetIdentity();
    }
}