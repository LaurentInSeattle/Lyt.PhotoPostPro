namespace Lyt.PhotoPostPro.Workflow.Process.WhiteBalance;

public sealed partial class WhiteBalanceToolboxViewModel : 
    ToolboxViewModel<WhiteBalanceToolboxView, WhiteBalanceStep>
{
    private bool doNotUpdate; 
    private float saturationThreshold;

    protected override string Title => this.Localize("Workflow.WhiteBalance.Title");

    [ObservableProperty]
    public partial string SaturationString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double SaturationSliderValue { get; set; }

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();
        this.OnClearWhiteBalance();
    }

    [RelayCommand]
    public void OnClearWhiteBalance()
    {
        this.doNotUpdate = true;
        {
            this.SaturationSliderValue = 0.4;
        }
        this.doNotUpdate = false;

        this.UpdateModel();
    }

    partial void OnSaturationSliderValueChanged(double value)
    {
        // Slider sends 0.0 to +1.0 
        this.saturationThreshold = (float)value;
        this.SaturationString = value.ToString("+0.00;-0.00;0.00");
        this.UpdateModel();
    }

    private void UpdateModel()
    {
        if ( this.doNotUpdate)
        {
            return; 
        }

        this.model.FilteredGrayWorldAWB(this.saturationThreshold);
    } 
}
