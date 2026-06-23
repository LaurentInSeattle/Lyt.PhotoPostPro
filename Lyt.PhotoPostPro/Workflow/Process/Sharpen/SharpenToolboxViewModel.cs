namespace Lyt.PhotoPostPro.Workflow.Process.Sharpen;

public sealed partial class SharpenToolboxViewModel :
    ToolboxViewModel<SharpenToolboxView, SharpenStep>
{
    private bool doNotUpdateModel;

    private SharpenStep.SharpenAlgorithm  algorithm;
    private float blur;

    protected override string Title => this.Localize("Workflow.Sharpen.Title");

    [ObservableProperty]
    public partial string BlurString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double BlurSliderValue { get; set; }

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();
        this.blur = 0.0f;

        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Sliders initial positions and string values
            this.BlurSliderValue = this.blur;
        });
    }

    public override void OnModelStepUpdated(SharpenStep step) => this.UpdateSliders(step);

    private void UpdateSliders(SharpenStep step)
    {
        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Here we need to undo the operations done reading the sliders 
            // No transform for the staturation threshold 
            this.BlurSliderValue = step.SharpenAmount;

            // More later here 
        });
    }

    partial void OnBlurSliderValueChanged(double value)
    {
        // Slider sends 0.0 to +2.5, fine for the model  
        this.algorithm = SharpenStep.SharpenAlgorithm.Sharpen;
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
            case SharpenStep.SharpenAlgorithm.Sharpen:
                this.model.GlobalSharpen(this.blur);
                break;

            case SharpenStep.SharpenAlgorithm.EdgesMask:
                // this.model.ColorMatrixWhiteBalance();
                break;

            default:
                break;
        }
    }
}
