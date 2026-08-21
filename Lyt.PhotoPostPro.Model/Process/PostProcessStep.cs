namespace Lyt.PhotoPostPro.Model.Process;

public abstract class PostProcessStep(PostProcessWorkflow postProcessWorkflow, string name)
{
    public const string OrientationStepName = "Orientation";
    public const string StraightenStepName = "Straighten";
    public const string CompositionStepName = "Composition";
    public const string ExposureStepName = "Exposure";
    public const string RecoveryStepName = "Recovery";
    public const string WhiteBalanceStepName = "WhiteBalance";
    public const string ContrastStepName = "Contrast";
    public const string LutStepName = "Lut";
    public const string ColorStepName = "Color";
    public const string SharpenStepName = "Sharpen";
    public const string VignetteStepName = "Vignette";
    public const string FiltersStepName = "Filters";
    public const string ExportStepName = "Export";

    public Dictionary<string, string> LocalizationStrings = new()
    {
        {   OrientationStepName  ,  "Workflow.Orient.Title"          },
        {   StraightenStepName   ,  "Workflow.Straighten.Title"      },
        {   CompositionStepName  ,  "Workflow.Compose.Title"         },
        {   ExposureStepName     ,  "Workflow.Exposure.Title"        },
        {   RecoveryStepName     ,  "Workflow.Recovery.Title"        },
        {   VignetteStepName     ,  "Workflow.Vignette.Title"        },
        {   WhiteBalanceStepName ,  "Workflow.WhiteBalance.Title"    },
        {   ContrastStepName     ,  "Workflow.Contrast.Title"        },
        {   LutStepName          ,  "Workflow.Lut.Title"             },
        {   ColorStepName        ,  "Workflow.Color.Title"           },
        {   SharpenStepName      ,  "Workflow.Sharpen.Title"         },
        {   FiltersStepName      ,  "Workflow.Filters.Title"         },
        {   ExportStepName       ,  "Workflow.Export.Title"          },
    };

    public string Name { get; set; } = name;

    public Image<RgbaHalf>? SourceImage { get; set; }

    public Image<RgbaHalf>? ResultImage { get; set; }

    internal PostProcessStep? PreviousStep { get; set; }

    internal PostProcessStep? NextStep { get; set; }

    internal PostProcessWorkflow PostProcessWorkflow { get; private set; } = postProcessWorkflow;

    internal bool InitialRunNeeded { get; set; }

    internal bool IsFirstRun { get; set; } = true;

    public bool IsIdentity { get; protected set; }

    public string LocalizationName => this.LocalizationStrings[this.Name];

    internal bool IsFirstStep => this.PreviousStep is null;

    internal bool IsLastStep => this.NextStep is null;

    // Performs actions provided in parameters 
    public abstract void PerformStep(PostProcessParameters postProcessParameters);

    protected abstract void SetIdentity();

    internal abstract Frame? Transform(bool withFrame = true);

    // Default implementation does nothing. Override in derived classes if needed.
    public virtual void Initialize(Image<RgbaHalf> originalImage) { }

    // Default implementation does nothing. Override in derived classes if needed.
    public virtual void Finish()
    {
        this.SourceImage?.Dispose();
        this.SourceImage = null;
        this.ResultImage?.Dispose();
        this.ResultImage = null;
    }

    // Default: Override in derived classes is needed.
    internal Frame? DoTransform(
        Action<Image<RgbaHalf>> transform,
        bool recalculateHistograms = true,
        bool withFrame = true)
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
            transform(clone);
            this.ResultImage = clone;
        }

        if (recalculateHistograms)
        {
            bool needForHistograms =
                !this.PostProcessWorkflow.PostProcess.IsReplayMode || this.NextStep is ExportStep;
            if (needForHistograms)
            {
                PostProcessStep.RecalculateHistograms(this.ResultImage);
            }
        }

        return withFrame ? this.ResultImage.ToFrame() : null;
    }

    // Default implementation restore original into result 
    public virtual Frame? Reset()
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        this.IsIdentity = true;
        this.ResultImage = this.SourceImage;
        return this.SourceImage.ToFrame();
    }

    // Override in derived classes if needed, overrides must call the base class .
    public virtual void Activate(WorkflowUpdateKind workflowUpdateKind)
    {
        Debug.WriteLine("Activating : " + this.Name + "  " + workflowUpdateKind);
        if (this.IsFirstRun)
        {
            Debug.WriteLine(this.Name + "  - First Run : " + workflowUpdateKind);
            this.IsFirstRun = false;
            this.Reset();
        }
        else
        {
            this.Transform(withFrame: false);
        }
    }

    // Default implementation does nothing. Override in derived classes if needed.
    internal virtual void Deactivate(WorkflowUpdateKind workflowUpdateKind) { }

    internal static void RecalculateHistograms(Image<RgbaHalf> image)
    {
        Task.Run(() =>
        {
            Histograms histograms = new(image);
            new HistogramsGeneratedMessage(histograms).Publish();
        });
    }
}
