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

    public override void OnModelStepUpdated(WhiteBalanceStep step) => this.UpdateSliders(step);

    private void UpdateSliders(WhiteBalanceStep step)
    {
        With.Flag(ref this.doNotUpdate, () =>
        {
            // Here we need to undo the operations done reading the sliders 
            // No transform for the staturation threshold 
            this.SaturationSliderValue = step.SaturationThreshold;

            // More later here 
        });

        this.UpdateModel();
    }

    partial void OnSaturationSliderValueChanged(double value)
    {
        // Slider sends 0.0 to +1.0, fine for the model  
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
