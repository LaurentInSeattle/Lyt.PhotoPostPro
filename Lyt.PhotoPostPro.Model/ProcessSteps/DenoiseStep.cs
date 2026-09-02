namespace Lyt.PhotoPostPro.Model.ProcessSteps;

public sealed class DenoiseStep(ProcessWorkflow processWorkflow) :
    ProcessStep(processWorkflow, ProcessStep.DenoiseStepName)
{
    [JsonConverter(typeof(JsonStringEnumConverter<DenoiseAlgorithm>))]
    public enum DenoiseAlgorithm
    {
        None,
        IsoGrain,
    }

    //float gaussianSharpen, // 0.8f
    //    int medianBlur, // 1
    //    float gaussianBlur, // 0.75f
    //    float blendFactor) // 0.4f

    public float GaussianSharpen { get; set; }

    public int MedianBlur { get; set; }

    public float GaussianBlur { get; set; }

    public float BlendFactor { get; set; }

    public DenoiseAlgorithm Algorithm { get; set; }

    public override void Initialize(Image<RgbaHalf> _) => this.Clear();

    protected override void SetIdentity()
    {
        base.IsIdentity = this.Algorithm == DenoiseAlgorithm.None; 
    }

    public override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    public override void PerformStep(ProcessParameters ppp)
    {
        switch (ppp.DenoiseAlgorithm)
        {
            default:
            case DenoiseAlgorithm.None:
                break;

            case DenoiseAlgorithm.IsoGrain:
                this.IsoGrainDenoise(
                    gaussianSharpen: ppp.IsoGrainDenoiseGaussianSharpen,
                    medianBlur: ppp.IsoGrainDenoiseMedianBlur,
                    gaussianBlur: ppp.IsoGrainDenoiseGaussianBlur,
                    blendFactor: ppp.IsoGrainDenoiseBlendFactor,
                    withFrame: false
                );
                break;
        }
    }

    internal override Frame? Transform(bool withFrame = true)
        => base.DoTransform((clone) =>
            {
                switch (this.Algorithm)
                {
                    case DenoiseAlgorithm.None:
                        break;

                    case DenoiseAlgorithm.IsoGrain:
                        clone.IsoGrainDenoise(
                            this.GaussianSharpen, this.MedianBlur, this.GaussianBlur, this.BlendFactor );
                        break;

                    default:
                        throw new NotImplementedException("No such Denoise algorithm");
                }
            }, withFrame);


    internal Frame? IsoGrainDenoise(
        float gaussianSharpen, // 0.8f
        int medianBlur, // 1
        float gaussianBlur, // 0.75f
        float blendFactor, // 0.4f
        bool withFrame = true)
    {
        this.Algorithm = DenoiseAlgorithm.IsoGrain;
        this.GaussianSharpen = gaussianSharpen;
        this.MedianBlur = medianBlur;
        this.GaussianBlur = gaussianBlur;
        this.BlendFactor = blendFactor;
        this.SetIdentity();
        return this.Transform(withFrame);
    }

    private void Clear()
    {
        this.Algorithm = DenoiseAlgorithm.None;

        // Use default values for all properties so that the UI sliders are also reset to zero on Reset 
        this.GaussianSharpen = 0.8f;
        this.MedianBlur = 1;
        this.GaussianBlur = 0.75f;
        this.BlendFactor = 0.4f;
        this.SetIdentity();
    }
}