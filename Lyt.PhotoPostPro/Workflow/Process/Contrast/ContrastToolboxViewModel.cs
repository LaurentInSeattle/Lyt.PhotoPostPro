namespace Lyt.PhotoPostPro.Workflow.Process.Contrast;

public sealed partial class ContrastToolboxViewModel :
    ToolboxViewModel<ContrastToolboxView, ContrastStep>
{
    private bool doNotUpdateModel;

    private ContrastStep.ContrastAlgorithm algorithm;
    private float contrast;
    private float blur;
    private float brightness;
    private float red;
    private float green;
    private float blue;

    protected override string Title => this.Localize("Workflow.Contrast.Title");

    [ObservableProperty]
    public partial string ContrastString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double ContrastSliderValue { get; set; }

    [ObservableProperty]
    public partial string BlurString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double BlurSliderValue { get; set; }

    [ObservableProperty]
    public partial string BrightnessString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double BrightnessSliderValue { get; set; }

    [ObservableProperty]
    public partial string RedString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double RedSliderValue { get; set; }

    [ObservableProperty]
    public partial string GreenString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double GreenSliderValue { get; set; }

    [ObservableProperty]
    public partial string BlueString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double BlueSliderValue { get; set; }

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();
        this.contrast = 1.0f;
        this.blur = 0.0f;
        this.brightness = 0.0f;

        this.red = 3.0f;
        this.green = 3.0f;
        this.blue = 3.0f;

        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Sliders initial positions and string values
            this.ContrastSliderValue = this.contrast;
            this.BlurSliderValue = 0.5; // Force Property changed
            this.BlurSliderValue = this.blur;
            this.BrightnessSliderValue = 0.5; // Force Property changed
            this.BrightnessSliderValue = this.brightness;

            // Enforce property changed
            this.RedSliderValue = 1.1;
            this.GreenSliderValue = 1.1;
            this.BlueSliderValue = 1.1;

            this.RedSliderValue = this.red;
            this.GreenSliderValue = this.green;
            this.BlueSliderValue = this.blue;
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
            this.BrightnessSliderValue = step.BrightnessAmount;

            this.RedSliderValue = step.RedAmount;
            this.GreenSliderValue = step.GreenAmount;
            this.BlueSliderValue = step.BlueAmount;
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

    partial void OnBrightnessSliderValueChanged(double value)
    {
        // Slider sends 0.0 to +0.5, fine for the model  
        this.algorithm = ContrastStep.ContrastAlgorithm.Global;
        this.brightness = (float)value;
        this.BrightnessString = value.ToString("+0.00;-0.00;0.00");
        this.UpdateModel();
    }

    partial void OnRedSliderValueChanged(double value)
    {
        // Slider sends +2.5 to +7.0, fine for the model  
        // Minimum = "2.5" Maximum = "7.0"
        this.algorithm = ContrastStep.ContrastAlgorithm.SCurves;
        this.red = (float)value;
        this.RedString = value.ToString("+0.00;-0.00;0.00");
        this.UpdateModel();
    }

    partial void OnGreenSliderValueChanged(double value)
    {
        // Slider sends +2.5 to +7.0, fine for the model  
        // Minimum = "2.5" Maximum = "7.0"
        this.algorithm = ContrastStep.ContrastAlgorithm.SCurves;
        this.green = (float)value;
        this.GreenString = value.ToString("+0.00;-0.00;0.00");
        this.UpdateModel();
    }

    partial void OnBlueSliderValueChanged(double value)
    {
        // Slider sends +2.5 to +7.0, fine for the model  
        // Minimum = "2.5" Maximum = "7.0"
        this.algorithm = ContrastStep.ContrastAlgorithm.SCurves;
        this.blue = (float)value;
        this.BlueString = value.ToString("+0.00;-0.00;0.00");
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
                this.model.GlobalContrast(this.contrast, this.blur, this.brightness);
                break;

            case ContrastStep.ContrastAlgorithm.SCurves:
                this.model.SCurvesContrast(this.red, this.green, this.blue);
                break;

            default:
                break;
        }
    }
}
