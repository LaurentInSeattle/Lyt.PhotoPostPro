namespace Lyt.PhotoPostPro.Workflow.Process.Spotlight;

public sealed partial class SpotlightToolboxViewModel :
    ToolboxViewModel<SpotlightToolboxView, SpotlightStep>
{
    private bool doNotUpdateModel;
    private float centerX;
    private float centerY;
    private float axisX = 120.0f;
    private float axisY = 60.0f;
    private float rotation;
    private float lightness;

    [ObservableProperty]
    public partial string AxisXString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double AxisXSliderValue { get; set; }

    [ObservableProperty]
    public partial string AxisYString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double AxisYSliderValue { get; set; }

    [ObservableProperty]
    public partial string RotationString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double RotationSliderValue { get; set; }


    [ObservableProperty]
    public partial string LightnessString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double LightnessSliderValue { get; set; }

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();

        if (!this.isFirstLoad)
        {
            return;
        }

        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Sliders initial positions and string values
            // Enforce property changed 
            this.AxisXSliderValue = 0.0;
            this.AxisYSliderValue = 0.0;
            this.RotationSliderValue = 45.0;
            this.LightnessSliderValue = 0.05;

            // proper values 
            this.AxisXSliderValue = this.axisX;
            this.AxisYSliderValue = this.axisY;
            this.RotationSliderValue = this.rotation;
            this.LightnessSliderValue = this.lightness;
        });

        this.isFirstLoad = false;
    }

    public override void OnModelStepUpdated(SpotlightStep step) => this.UpdateSliders(step);

    private void UpdateSliders(SpotlightStep step)
    {
        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Here we need to undo the operations done reading the sliders 
            //this.TopSliderValue = step.Top;
            //this.BottomSliderValue = step.Bottom;
            //this.LeftSliderValue = step.Left;
            //this.RightSliderValue = step.Right;
            this.LightnessSliderValue = step.Lightness;
        });
    }

    partial void OnAxisXSliderValueChanged(double value)
    {
        // Slider sends 5 to 500
        this.axisX = (float)value;
        int intValue = (int)(this.axisX);
        this.AxisXString = intValue.ToString("D");
        this.UpdateModel();
    }

    partial void OnAxisYSliderValueChanged(double value)
    {
        // Slider sends 5 to 500
        this.axisY = (float)value;
        int intValue = (int)(this.axisY);
        this.AxisYString = intValue.ToString("D");
        this.UpdateModel();
    }

    partial void OnRotationSliderValueChanged(double value)
    {
        // Slider sends -45 to 45
        this.rotation = (float)value;
        this.RotationString = this.rotation.ToString("F1") + " \u00B0";
        this.UpdateModel();
    }

    partial void OnLightnessSliderValueChanged(double value)
    {
        // Slider sends -0.8 to +0.8
        this.lightness = (float)value;
        this.LightnessString = value.ToString("+0.00;-0.00;0.00") + " %"; 
        this.UpdateModel();
    }

    private void UpdateModel()
    {
        if (this.doNotUpdateModel)
        {
            return;
        }

        if (( this.centerX <= 0) || (this.centerY <= 0))
        {
            return; 
        }

        this.ThrottleModelUpdate(() =>
        {
            this.model.Spotlight(
                this.centerX, this.centerY,
                this.axisX, this.axisY, 
                this.rotation, this.lightness);
        });
    }
}
