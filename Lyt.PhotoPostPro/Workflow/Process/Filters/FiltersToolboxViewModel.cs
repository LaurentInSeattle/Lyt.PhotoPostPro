namespace Lyt.PhotoPostPro.Workflow.Process.Filters;

public sealed partial class FiltersToolboxViewModel :
    ToolboxViewModel<FiltersToolboxView, FiltersStep>
{
    private bool doNotUpdateModel;
    private float grayscale ;
    private float sepia;

    public FiltersToolboxViewModel()
    {
        this.grayscale = 0.0f;
        this.sepia = 0.0f;
    }

    protected override string Title => this.Localize("Workflow.Filters.Title");

    [ObservableProperty]
    public partial string GrayscaleString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double GrayscaleSliderValue { get; set; }

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();

        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Sliders initial positions and string values
            // Enforce property changed 
            this.GrayscaleSliderValue = this.grayscale + 0.01;
            this.GrayscaleSliderValue = this.grayscale;
        });
    }

    public override void OnModelStepUpdated(FiltersStep step) => this.UpdateSliders(step);

    private void UpdateSliders(FiltersStep  step)
    {
        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Here we need to undo the operations done reading the sliders 
            // No transforms for highlights and shadows amounts 
            this.GrayscaleSliderValue = step.Amount;
        });
    }

    partial void OnGrayscaleSliderValueChanged  (double value)
    {
        // Slider sends 0 to +1
        this.grayscale = (float)value;
        int intValue = (int)(value * 100.0 + 0.5); 
        this.GrayscaleString = intValue.ToString("D") + " %";
        this.UpdateModel();
    }


    private void UpdateModel()
    {
        if (this.doNotUpdateModel)
        {
            return;
        }

        // this.model.Grayscale(this.grayscale);
        this.model.Sepia(this.grayscale);
    }
}
