namespace Lyt.PhotoPostPro.Workflow.Process.Exposure;

public sealed partial class ExposureToolboxViewModel :
    ToolboxViewModel<ExposureToolboxView, ExposureStep>
{
    private bool doNotUpdateModel;
    private double gamma;
    private double gain;
    private int shift;

    public ExposureToolboxViewModel()
    {
        this.GammaCurveViewModel = new();
        this.gamma = 1.0;
        this.gain = 1.0;
        this.shift = 0;
    }

    [ObservableProperty]
    public partial GammaCurveViewModel GammaCurveViewModel { get; set; }

    [ObservableProperty]
    public partial string GammaString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double GammaSliderValue { get; set; }

    [ObservableProperty]
    public partial string GainString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double GainSliderValue { get; set; }

    [ObservableProperty]
    public partial string ShiftString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double ShiftSliderValue { get; set; }

    protected override string Title => this.Localize("Workflow.Exposure.Title");

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();

        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Sliders initial positions and string values
            this.GammaSliderValue = this.gamma;
            this.GainSliderValue = this.gain;

            // Enforce property changed 
            this.ShiftSliderValue = this.shift + 0.01;
            this.ShiftSliderValue = this.shift;
        });
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
    } 

    public override void OnModelStepUpdated(ExposureStep step) => this.UpdateSliders(step);

    // Interface inplementation has to be public
    private void UpdateSliders(ExposureStep step)
    {
        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Here we need to undo the operations done reading the sliders 
            this.GammaSliderValue = step.Gamma - 1.0;
            this.GainSliderValue = step.Gain - 1.0;
            this.ShiftSliderValue = step.Shift / 65535.0;
        });
    }

    partial void OnGammaSliderValueChanged(double value)
    {
        // Slider sends -0.5 to +0.5, add one 
        this.gamma = (float)(value + 1.0);
        this.GammaString = value.ToString("+0.00;-0.00;0.00");
        this.UpdateModel();
    }

    partial void OnGainSliderValueChanged(double value)
    {
        // Slider sends -0.5 to +0.5, add one 
        this.gain = (float)(value + 1.0);
        string stringValue = value.ToString("+0.00;-0.00;0.00");
        this.GainString = stringValue + " %";
        this.UpdateModel();
    }

    partial void OnShiftSliderValueChanged(double value)
    {
        // Slider sends -0.5 to +0.5, scale up 
        this.shift = (int)(value * 65535);
        string stringValue = value.ToString("+0.00;-0.00;0.00");
        this.ShiftString = stringValue + " %";
        this.UpdateModel();
    }

    private void UpdateModel()
    {
        if (this.doNotUpdateModel)
        {
            return;
        }

        this.model.AdjustExposure(this.gamma, this.gain, this.shift);
    }
}
