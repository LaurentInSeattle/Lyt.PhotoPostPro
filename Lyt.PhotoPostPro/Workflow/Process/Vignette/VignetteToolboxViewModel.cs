namespace Lyt.PhotoPostPro.Workflow.Process.Vignette;

public sealed partial class VignetteToolboxViewModel :
    ToolboxViewModel<VignetteToolboxView, VignetteStep>
{
    private bool doNotUpdateModel;
    private float top;
    private float bottom;
    private float left;
    private float right;
    private float lightness; 


    protected override string Title => this.Localize("Workflow.Vignette.Title");

    [ObservableProperty]
    public partial string TopString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double TopSliderValue { get; set; }

    [ObservableProperty]
    public partial string BottomString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double BottomSliderValue { get; set; }

    [ObservableProperty]
    public partial string LeftString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double LeftSliderValue { get; set; }

    [ObservableProperty]
    public partial string RightString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double RightSliderValue { get; set; }

    [ObservableProperty]
    public partial string LightnessString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double LightnessSliderValue { get; set; }

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();

        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Sliders initial positions and string values
            // Enforce property changed 
            this.TopSliderValue = this.top + 0.01;
            this.BottomSliderValue = this.bottom + 0.01;
            this.LeftSliderValue = this.left + 0.01;
            this.RightSliderValue = this.right + 0.01;
            this.LightnessSliderValue = this.lightness + 0.01;

            // proper values 
            this.TopSliderValue = this.top;
            this.BottomSliderValue = this.bottom;
            this.LeftSliderValue = this.left;
            this.RightSliderValue = this.right;
            this.LightnessSliderValue = this.lightness;
        });
    }

    public override void OnModelStepUpdated(VignetteStep step) => this.UpdateSliders(step);

    private void UpdateSliders(VignetteStep step)
    {
        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Here we need to undo the operations done reading the sliders 
            // No transforms for highlights and shadows amounts 
            this.TopSliderValue = step.Top;
            this.BottomSliderValue = step.Bottom;
            this.LeftSliderValue = step.Left;
            this.RightSliderValue = step.Right;
            this.LightnessSliderValue = step.Lightness;
        });
    }

    partial void OnTopSliderValueChanged(double value)
    {
        // Slider sends 0 to 0.45
        this.top = (float)value;
        int intValue = (int)(this.top * 100);
        this.TopString = intValue.ToString("D") + " %";
        this.UpdateModel();
    }

    partial void OnBottomSliderValueChanged(double value)
    {
        // Slider sends 0 to 0.45
        this.bottom = (float)value;
        int intValue = (int)(this.bottom * 100);
        this.BottomString = intValue.ToString("D") + " %";
        this.UpdateModel();
    }

    partial void OnLeftSliderValueChanged(double value)
    {
        // Slider sends 0 to 0.45
        this.left = (float)value;
        int intValue = (int)(this.left * 100);
        this.LeftString = intValue.ToString("D") + " %";
        this.UpdateModel();
    }

    partial void OnRightSliderValueChanged(double value)
    {
        // Slider sends 0 to 0.45
        this.right = (float)value;
        int intValue = (int)(this.right * 100);
        this.RightString = intValue.ToString("D") + " %";
        this.UpdateModel();
    }

    partial void OnLightnessSliderValueChanged(double value)
    {
        // Slider sends -0.75 to +0.75
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

        this.model.Vignette(this.top, this.bottom, this.left, this.right, this.lightness);
    }
}
