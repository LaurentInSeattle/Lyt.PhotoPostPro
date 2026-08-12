namespace Lyt.PhotoPostPro.Model.PostProcessors;

public sealed class LutStep(PostProcessWorkflow postProcessWorkflow) :
    PostProcessStep(postProcessWorkflow, PostProcessStep.LutStepName)
{
    public LutMetadata LutMetadata { get; set; } = LutMetadata.Empty;

    private Image<RgbaVector>? thumbnail;
    private bool exploreCancelled;

    public override void Initialize(Image<RgbaVector> _) => this.Clear();

    public override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    public override void Activate(WorkflowUpdateKind workflowUpdateKind)
    {
        base.Activate(workflowUpdateKind);

        if (this.SourceImage is null)
        {
            return;
        }

        this.thumbnail = this.SourceImage.Clone();
        this.thumbnail.Mutate(x => x.Resize(1024, 0));
    }

    public override void PerformStep(PostProcessParameters ppp)
    {
        LutMetadata lutMetadataMaybe =
            new(ppp.LutFriendlyName, ppp.LutPath, LutFormat.Unknown, ppp.LutIsEmbedded);
        if (lutMetadataMaybe.IsEmpty)
        {
            return;
        }

        LutFormat lutFormat = LutFormat.Unknown;
        if (ppp.LutPath.EndsWith("cube", StringComparison.InvariantCultureIgnoreCase))
        {
            lutFormat = LutFormat.Cube;
        }
        else if (ppp.LutPath.EndsWith("3dl", StringComparison.InvariantCultureIgnoreCase))
        {
            lutFormat = LutFormat.ThreeDL;
        }

        LutMetadata lutMetadata =
            new(ppp.LutFriendlyName, ppp.LutPath, lutFormat, ppp.LutIsEmbedded);
        this.Lut(lutMetadata);
    }

    public override Frame? Transform(bool withFrame = true)
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        var clone = this.SourceImage.Clone();
        clone.Lut(this.LutMetadata);
        PostProcessStep.RecalculateHistograms(clone);
        this.ResultImage = clone;
        return withFrame ? clone.ToFrame() : null;
    }

    public void CancelExploreLuts() => this.exploreCancelled = true;

    public void LaunchExploreLuts()
    {
        Task.Run(() =>
        {
            Thread.CurrentThread.Name = "LutStep.ExploreLuts";
            this.exploreCancelled = false;
            this.ExploreLuts();
        });
    }

    private void ExploreLuts()
    {
        if (this.thumbnail is null)
        {
            return;
        }

        bool done = false;

        // Send the original as thumbnail before looping on the LUTs
        new ExploreLutImageGeneratedMessage(LutMetadata.Empty, this.thumbnail.ToFrame()).Publish();

        // Throttle to let the UI display this image
        Task.Delay(120).Wait();

        var luts = LutsManager.BuiltInLuts();
        int lutIndex = 0;
        while (!done && !this.exploreCancelled)
        {
            if (lutIndex >= luts.Count)
            {
                done = true;
                break;
            }

            // Do not try to paralelize for now, it should be fast enough 
            LutMetadata lutMetadata = luts[lutIndex];
            ++lutIndex;
            var clone = this.thumbnail.Clone();
            clone.Lut(lutMetadata);
            new ExploreLutImageGeneratedMessage(lutMetadata, clone.ToFrame()).Publish();

            // Check for bailing out before waiting 
            if (this.exploreCancelled)
            {
                break;
            }

            // Throttle to let the UI display the images 
            Task.Delay(120).Wait();
        }

        if (done)
        {
            Debug.WriteLine(" All LUT images generated.");
        }
        else
        {
            Debug.WriteLine(" LUT images generation aborted.");
        }
    }

    internal Frame? Lut(LutMetadata lutMetadata)
    {
        this.LutMetadata = lutMetadata;
        return this.Transform(withFrame: true);
    }

    private void Clear() => this.LutMetadata = LutMetadata.Empty;
}