namespace Lyt.PhotoPostPro.Model.PostProcessors;

public sealed class LutStep(PostProcessWorkflow postProcessWorkflow) :
    PostProcessStep(postProcessWorkflow, PostProcessStep.LutStepName)
{
    public const int ThumbnailSize = 1024; 

    public LutMetadata LutMetadata { get; set; } = LutMetadata.Empty;

    private Image<RgbaHalf>? thumbnail;

    public override void Initialize(Image<RgbaHalf> _) => this.Clear();

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
        this.thumbnail.Mutate(x => x.Resize(ThumbnailSize, 0));
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

        if (this.LutMetadata == LutMetadata.Empty)
        {
            //if (Debugger.IsAttached) { Debugger.Break(); }
            // Does happen when going back after quickly moving forward in the workflow
            return null;
        }

        var model = this.PostProcessWorkflow.PostProcess.Model;
        if (!model.LutsManager.TryLoadLut(this.LutMetadata, out Lut? lut))
        {
            // Failed to load LUT ? 
            if (Debugger.IsAttached) { Debugger.Break(); }
            return null;
        }

        if (lut is null)
        {
            return null;
        }

        var clone = this.SourceImage.Clone();
        clone.Lut(lut);
        PostProcessStep.RecalculateHistograms(clone);
        this.ResultImage = clone;
        return withFrame ? clone.ToFrame() : null;
    }

    public void LaunchExploreLuts()
    {
        Task.Run(() =>
        {
            Thread.CurrentThread.Name = "LutStep.ExploreLuts";
            this.ExploreLuts();
        });
    }

    private void ExploreLuts()
    {
        if (this.thumbnail is null)
        {
            return;
        }

        // Send the original as thumbnail before looping on the LUTs
        new ExploreLutImageGeneratedMessage(LutMetadata.Empty, this.thumbnail.ToFrame()).Publish();

        // Throttle to let the UI display the initial image
        Task.Delay(30).Wait();

        var model = this.PostProcessWorkflow.PostProcess.Model;
        var luts = model.LutsManager.EnumerateBuiltInLuts();
        int lutDone = 0;
        Parallel.For(0, luts.Count, lutIndex =>
        {
            LutMetadata lutMetadata = luts[lutIndex];
            if (lutMetadata == LutMetadata.Empty)
            {
                if (Debugger.IsAttached) { Debugger.Break(); }
                return;
            }

            Lut? lut = null;
            lock (model.LutsManager)
            {
                if (!model.LutsManager.TryLoadLut(lutMetadata, out lut))
                {
                    // Failed to load LUT ? 
                    if (Debugger.IsAttached) { Debugger.Break(); }
                    return;
                }

                if (lut is null)
                {
                    return;
                }
            }

            var clone = this.thumbnail.Clone();
            clone.Lut(lut);
            new ExploreLutImageGeneratedMessage(lutMetadata, clone.ToFrame()).Publish();
            ++lutDone;
        });

        if (lutDone == luts.Count)
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