namespace Lyt.PhotoPostPro.Workflow.Process.Contrast;

public sealed partial class ContrastToolboxViewModel :
    ToolboxViewModel<ContrastToolboxView, ContrastStep>
{
    private bool doNotUpdateModel;

    private ContrastStep.ContrastAlgorithm algorithm;
    private float contrast;
    private float blur;

    protected override string Title => this.Localize("Workflow.Contrast.Title");

    [ObservableProperty]
    public partial string ContrastString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double ContrastSliderValue { get; set; }

    [ObservableProperty]
    public partial string BlurString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double BlurSliderValue { get; set; }

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();
        this.contrast = 1.0f;
        this.blur = 0.0f;

        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Sliders initial positions and string values
            this.ContrastSliderValue = this.contrast;
            this.BlurSliderValue = this.blur;
        });
    }

    public override void OnModelStepUpdated(ContrastStep step) => this.UpdateSliders(step);

    private void UpdateSliders(ContrastStep step)
    {
        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Here we need to undo the operations done reading the sliders 
            // No transform for the staturation threshold 
            this.ContrastSliderValue = step.ContrastAmount;
            this.BlurSliderValue = step.BlurAmount;

            // More later here 
        });
    }

    partial void OnContrastSliderValueChanged(double value)
    {
        // Slider sends 1.0 to +2.5, fine for the model  
        this.algorithm = ContrastStep.ContrastAlgorithm.Global;
        this.contrast = (float)value;
        this.ContrastString = value.ToString("+0.00;-0.00;0.00");
        this.UpdateModel();
    }

    partial void OnBlurSliderValueChanged(double value)
    {
        // Slider sends 0.0 to +2.5, fine for the model  
        this.algorithm = ContrastStep.ContrastAlgorithm.Global;
        this.blur = (float)value;
        this.BlurString = value.ToString("+0.00;-0.00;0.00");
        this.UpdateModel();
    }

    private void UpdateModel()
    {
        if (this.doNotUpdateModel)
        {
            return;
        }

        switch (this.algorithm)
        {
            case ContrastStep.ContrastAlgorithm.Global:
                this.model.GlobalContrast(this.contrast, this.blur);
                break;

            case ContrastStep.ContrastAlgorithm.SCurves:
                // this.model.ColorMatrixWhiteBalance();
                break;

            default:
                break;
        }

    }
}
