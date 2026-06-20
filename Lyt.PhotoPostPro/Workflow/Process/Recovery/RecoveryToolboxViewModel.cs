namespace Lyt.PhotoPostPro.Workflow.Process.Recovery;

public sealed partial class RecoveryToolboxViewModel :
    ToolboxViewModel<RecoveryToolboxView, RecoveryStep>
{
    private bool doNotUpdateModel;
    private float highlights;
    private float shadows;

    protected override string Title => this.Localize("Workflow.Recovery.Title");

    [ObservableProperty]
    public partial string HighlightsString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double HighlightsSliderValue { get; set; }

    [ObservableProperty]
    public partial string ShadowsString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double ShadowsSliderValue { get; set; }

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();

        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Sliders initial positions and string values
            // Enforce property changed 
            this.HighlightsSliderValue = this.highlights + 0.01;
            this.ShadowsSliderValue = this.shadows + 0.01;
            this.HighlightsSliderValue = this.highlights;
            this.ShadowsSliderValue = this.shadows;
        });
    }

    public override void OnModelStepUpdated(RecoveryStep step) => this.UpdateSliders(step);

    private void UpdateSliders(RecoveryStep step)
    {
        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Here we need to undo the operations done reading the sliders 
            // No transforms for highlights and shadows amounts 
            this.HighlightsSliderValue = step.HighlightAmount;
            this.ShadowsSliderValue = step.ShadowAmount;
        });
    }

    partial void OnHighlightsSliderValueChanged(double value)
    {
        // Slider sends -0.7 to +0.7 
        this.highlights = (float)value;
        this.HighlightsString = value.ToString("+0.00;-0.00;0.00") + " %";
        this.UpdateModel();
    }

    partial void OnShadowsSliderValueChanged(double value)
    {
        // Slider sends -0.7 to +0.7 
        this.shadows = (float)value;
        this.ShadowsString = value.ToString("+0.00;-0.00;0.00") + " %";
        this.UpdateModel();
    }

    private void UpdateModel()
    {
        if (this.doNotUpdateModel)
        {
            return;
        }

        this.model.HighlightsShadows(this.highlights, this.shadows);
    }
}
