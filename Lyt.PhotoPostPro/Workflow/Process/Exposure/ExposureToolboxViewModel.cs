namespace Lyt.PhotoPostPro.Workflow.Process.Exposure;

public sealed partial class ExposureToolboxViewModel : 
    ToolboxViewModel<ExposureToolboxView, ExposureStep>
{
    private bool doNotUpdate; 
    private double gamma ;
    private double gain ;
    private int shift;

    public ExposureToolboxViewModel()
    {
        this.gamma = 1.0;
        this.gain = 1.0;
        this.shift = 0;
        this.GammaCurveViewModel = new();     
    }

    protected override string Title => this.Localize("Workflow.Exposure.Title");

    [ObservableProperty]
    public partial GammaCurveViewModel GammaCurveViewModel{ get; set; }

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

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();
        this.OnBeforeReset();
    }

    // Interface inplementation has to be public
    public override void OnBeforeReset()
    {
        this.doNotUpdate = true;
        {
            this.GammaSliderValue = 0.0;
            this.GainSliderValue = 0.0;
            this.ShiftSliderValue = 0.0;
        }
        this.doNotUpdate = false;
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
        this.shift = (int)( value * 65535) ;
        string stringValue = value.ToString("+0.00;-0.00;0.00");
        this.ShiftString = stringValue + " %";
        this.UpdateModel();
    }

    private void UpdateModel()
    {
        if ( this.doNotUpdate)
        {
            return; 
        }

        this.model.AdjustExposure(this.gamma, this.gain, this.shift);
    }
}
