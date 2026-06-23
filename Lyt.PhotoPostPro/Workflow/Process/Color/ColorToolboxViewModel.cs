namespace Lyt.PhotoPostPro.Workflow.Process.Color;

public sealed partial class ColorToolboxViewModel :
    ToolboxViewModel<ColorToolboxView, ColorStep>
{
    private bool doNotUpdateModel;

    private ColorStep.ColorAlgorithm algorithm;
    private float saturation;

    protected override string Title => this.Localize("Workflow.Color.Title");

    [ObservableProperty]
    public partial string SaturationString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double SaturationSliderValue { get; set; }

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();
        this.saturation = 1.0f;

        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Sliders initial positions and string values
            this.SaturationSliderValue = this.saturation;
        });
    }

    public override void OnModelStepUpdated(ColorStep step) => this.UpdateSliders(step);

    private void UpdateSliders(ColorStep step)
    {
        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Here we need to undo the operations done reading the sliders 
            // No transform for the staturation threshold 
            this.SaturationSliderValue = step.SaturationAmount;

            // More later here 
        });
    }

    partial void OnSaturationSliderValueChanged(double value)
    {
        // Slider sends 1.0 to +2.5, fine for the model  
        this.algorithm = ColorStep.ColorAlgorithm.Saturation;
        this.saturation = (float)value;
        this.SaturationString = value.ToString("+0.00;-0.00;0.00");
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
            case ColorStep.ColorAlgorithm.Saturation:
                this.model.GlobalSaturation(this.saturation);
                break;

            case ColorStep.ColorAlgorithm.Vibrance:
                // TODO
                // this.model.ColorMatrixWhiteBalance();
                break;

            default:
                break;
        }

    }
}
