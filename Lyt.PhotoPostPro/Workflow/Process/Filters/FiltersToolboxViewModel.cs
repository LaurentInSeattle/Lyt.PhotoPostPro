namespace Lyt.PhotoPostPro.Workflow.Process.Filters;

public sealed partial class FiltersToolboxViewModel :
    ToolboxViewModel<FiltersToolboxView, FiltersStep>,
    IRecipient<LanguageChangedMessage>
{
    private bool doNotUpdateModel;
    private FiltersStep.Filter selectedFilter; 

    private float amount ;

    public FiltersToolboxViewModel()
    {
        this.selectedFilter = FiltersStep.Filter.Grayscale;
        this.amount = 0.0f;
        this.Subscribe<LanguageChangedMessage>();
    }

    protected override string Title => this.Localize("Workflow.Filters.Title");

    private static readonly List<string> supportedFiltersKeys =
        [
            "Workflow.Filters.None" ,
            "Workflow.Filters.Grayscale" ,
            "Workflow.Filters.Sepia"  ,
            "Workflow.Filters.Vignette"  ,
            "Workflow.Filters.BlackWhite"  , 

            // Image Sharp randomly crashes with these 
            //
            //"Workflow.Filters.Kodachrome"  ,
            //"Workflow.Filters.Polaroid"  , 
            //"Workflow.Filters.Lomograph"  , 
        ];

    private static readonly List<FiltersStep.Filter> filters =
        [
            FiltersStep.Filter.None,
            FiltersStep.Filter.Grayscale,
            FiltersStep.Filter.Sepia,
            FiltersStep.Filter.Vignette,
            FiltersStep.Filter.BlackWhite,
            //FiltersStep.Filter.Kodachrome,
            //FiltersStep.Filter.Polaroid,
            //FiltersStep.Filter.Lomograph,
        ];

    [ObservableProperty]
    public partial List<string> SupportedFilters { get; set; } =
        // Needs localization : these are default
        [
            "None" ,
            "Grayscale" ,
            "Sepia"  ,
            "Vignette"  ,
            "Black and White"  ,
            //"Kodachrome"  ,
            //"Polaroid"  ,
            //"Lomograph"  ,
        ];

    [ObservableProperty]
    public partial int SelectedIndex { get; set; }

    [ObservableProperty]
    public partial string AmountString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double AmountSliderValue { get; set; }

    public void Receive(LanguageChangedMessage _) => this.Localize();

    private void Localize()
    {
        List<string> localized = new(supportedFiltersKeys.Count);
        for (int i = 0; i < supportedFiltersKeys.Count; ++i)
        {
            localized.Add(this.Localize(supportedFiltersKeys[i]));
        }

        // Enforce property changed by providing a new list instance
        this.SupportedFilters = localized;
    }

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

    partial void OnSelectedIndexChanged(int value)
    {
        if (value >= 0 && value < this.SupportedFilters.Count)
        {
            // Wait one frame before launching the image color lookup process
            // If we dont, the app randomly freezes, with no exceptions thrown.
            // Possible Avalonia Bug ? Need to test with latest 12.0.5
            // Debug Output shows:
            // [Control] PlatformImpl is null, couldn't handle input. (PresentationSource #<some number>>)
            Schedule.OnUiThread(66, () =>
            {
                this.selectedFilter = filters[value];
                this.UpdateModel();
            }, DispatcherPriority.Background);
        }
    } 

    private void UpdateModel()
    {
        if (this.doNotUpdateModel)
        {
            return;
        }

        switch (this.selectedFilter)
        {
            default:
            case FiltersStep.Filter.None:
                this.model.Workflow.Reset();
                break;

            case FiltersStep.Filter.Grayscale:
                this.model.Grayscale(this.amount);
                break;

            case FiltersStep.Filter.Sepia:
                this.model.Sepia(this.amount);
                break;

            case FiltersStep.Filter.Vignette:
                this.model.Vignette();
                break;

            case FiltersStep.Filter.BlackWhite:
                this.model.BlackWhite();
                break;

            case FiltersStep.Filter.Kodachrome:
                this.model.Kodachrome();
                break;

            case FiltersStep.Filter.Lomograph:
                this.model.Lomograph();
                break;

            case FiltersStep.Filter.Polaroid:
                this.model.Polaroid();
                break;
        }
    }
}
