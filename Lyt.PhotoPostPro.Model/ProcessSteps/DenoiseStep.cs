namespace Lyt.PhotoPostPro.Model.ProcessSteps;

public sealed class DenoiseStep(ProcessWorkflow processWorkflow) :
    ProcessStep(processWorkflow, ProcessStep.DenoiseStepName)
{
    [JsonConverter(typeof(JsonStringEnumConverter<DenoiseAlgorithm>))]
    public enum DenoiseAlgorithm
    {
        None,
        IsoGrain,
        StrongIsoGrain,
    }

    public float GaussianSharpen { get; set; }

    public int MedianBlur { get; set; }

    public float GaussianBlur { get; set; }

    public float BlendFactor { get; set; }

    public DenoiseAlgorithm Algorithm { get; set; }

    internal override void Initialize(Image<RgbaHalf> _) => this.Clear();

    protected override void SetIdentity() => base.IsIdentity = this.Algorithm == DenoiseAlgorithm.None; 

    internal override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    internal override void PerformStep(ProcessParameters ppp)
    {
        switch (ppp.DenoiseAlgorithm)
        {
            default:
            case DenoiseAlgorithm.None:
                break;

            case DenoiseAlgorithm.StrongIsoGrain:
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
                    case DenoiseAlgorithm.StrongIsoGrain:
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
        return this.IsoDenoise(gaussianSharpen, medianBlur, gaussianBlur, blendFactor, withFrame);
    }

    internal Frame? StrongIsoGrainDenoise(
        float gaussianSharpen, // 0.8f
        int medianBlur, // 2   <==
        float gaussianBlur, // 0.75f
        float blendFactor, // 0.5f  <==
        bool withFrame = true)
    {
        this.Algorithm = DenoiseAlgorithm.StrongIsoGrain;
        return this.IsoDenoise(gaussianSharpen, medianBlur, gaussianBlur, blendFactor, withFrame); 
    } 

    private Frame? IsoDenoise(
        float gaussianSharpen,  int medianBlur,  float gaussianBlur,  float blendFactor,  bool withFrame = true)
    {
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

        // Unseen in the UI - Base ISO Grain settings 
        this.GaussianSharpen = 0.8f;
        this.MedianBlur = 1;
        this.GaussianBlur = 0.75f;
        this.BlendFactor = 0.4f;
        this.SetIdentity();
    }
}