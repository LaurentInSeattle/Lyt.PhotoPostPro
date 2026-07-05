namespace Lyt.PhotoPostPro.Workflow.Process.Filters;

public sealed partial class FiltersToolboxViewModel :
    ToolboxViewModel<FiltersToolboxView, FiltersStep>
{
    private bool doNotUpdateModel;
    private float amount ;

    protected override string Title => this.Localize("Workflow.Filters.Title");

    [ObservableProperty]
    public partial string AmountString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double AmountSliderValue { get; set; }

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();

        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Sliders initial positions and string values
            // Enforce property changed 
            this.AmountSliderValue = this.amount + 0.01;
            this.AmountSliderValue = this.amount;
        });
    }

    public override void OnModelStepUpdated(FiltersStep step) => this.UpdateSliders(step);

    private void UpdateSliders(FiltersStep  step)
    {
        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Here we need to undo the operations done reading the sliders 
            // No transforms for highlights and shadows amounts 
            this.AmountSliderValue = step.Amount;
        });
    }

    partial void OnAmountSliderValueChanged  (double value)
    {
        // Slider sends 0 to +1
        this.amount = (float)value;
        int intValue = (int)(value * 100.0 + 0.5); 
        this.AmountString = intValue.ToString("D") + " %";
        this.UpdateModel();
    }


    private void UpdateModel()
    {
        if (this.doNotUpdateModel)
        {
            return;
        }

        // this.model.Grayscale(this.amount);
        this.model.Sepia(this.amount);
    }
}
