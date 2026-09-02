namespace Lyt.PhotoPostPro.Workflow.Process.Denoise;

public sealed partial class DenoiseToolboxViewModel :
    ToolboxViewModel<DenoiseToolboxView, DenoiseStep>,
    IRecipient<LanguageChangedMessage>
{
    private bool doNotUpdateModel;
    private DenoiseStep.DenoiseAlgorithm selectedFilter;

    private float amount;

    public DenoiseToolboxViewModel()
    {
        this.selectedFilter = DenoiseStep.DenoiseAlgorithm.None ;
        this.amount = 0.0f;
        this.Subscribe<LanguageChangedMessage>();
    }

    protected override string Title => this.Localize("Workflow.Denoise.Title");

    private static readonly List<string> supportedFiltersKeys =
        [
            // Must match the order of the Filter enum defined in FilterStep
            "Workflow.Denoise.None" ,
            "Workflow.Denoise.IsoGrain" ,
        ];

    [ObservableProperty]
    public partial List<string> SupportedFilters { get; set; } =
        // Needs localization : these are default
        // Must match the order of the Filter enum defined in FilterStep
        [
            "None" ,
            "IsoGrain" ,
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
        this.Localize(); 

        if (!this.isFirstLoad)
        {
            return;
        }

        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Enforce property changed 
            this.SelectedIndex = 1;
            this.SelectedIndex = 0;

            // Sliders initial positions and string values
            // Enforce property changed 
            this.AmountSliderValue = this.amount + 0.01;
            this.AmountSliderValue = this.amount;
        });

        this.isFirstLoad = false;
    }

    public override void OnModelStepUpdated(DenoiseStep step) => this.UpdateSliders(step);

    private void UpdateSliders(DenoiseStep step)
    {
        With.Flag(ref this.doNotUpdateModel, () =>
        {
            this.SelectedIndex = (int)step.Algorithm;

            // Here we need to undo the operations done reading the sliders 
            // No transforms for highlights and shadows amounts 
            // this.AmountSliderValue = step.Amount;
        });
    }

    partial void OnAmountSliderValueChanged(double value)
    {
        // Slider sends 0 to +1
        this.amount = (float)value;
        int intValue = (int)(value * 100.0 + 0.5);
        this.AmountString = intValue.ToString("D") + " %";
        this.UpdateModel();
    }

    partial void OnSelectedIndexChanged(int value)
    {
        if (!this.IsActivated)
        {
            return;
        }

        if (value >= 0 && value < this.SupportedFilters.Count)
        {
            // Wait one frame before launching the image color lookup process
            // If we dont, the app randomly freezes, with no exceptions thrown.
            // Possible Avalonia Bug ? Need to test with latest 12.0.5
            // Debug Output shows:
            // [Control] PlatformImpl is null, couldn't handle input. (PresentationSource #<some number>>)
            if (Enum.IsDefined(typeof(DenoiseStep.DenoiseAlgorithm), value))
            {
                Schedule.OnUiThread(150, () =>
                {
                    this.selectedFilter = (DenoiseStep.DenoiseAlgorithm)value;
                    this.UpdateModel();
                }, DispatcherPriority.Background);
            }
        }
    }

    private void UpdateModel()
    {
        if (this.doNotUpdateModel)
        {
            return;
        }

        if ((this.selectedFilter == DenoiseStep.DenoiseAlgorithm.IsoGrain))
        {
            this.ThrottleModelUpdate(() =>
            {
                // Throttle to avoid freezing the UI when moving the slider
                // Only for filters that require an amount value, like Grayscale and Sepia
                switch (this.selectedFilter)
                {
                    default:
                        break;

                    case DenoiseStep.DenoiseAlgorithm.IsoGrain:
                        // this.model.IsoGrain(this.amount);
                        break;
                }
            });
        }
        else
        {
            // All other filters do not require an amount value 
            // So we can update the model immediately
            switch (this.selectedFilter)
            {
                default:
                case DenoiseStep.DenoiseAlgorithm.None:
                    // Call on the model, and NOT the workflow 
                    this.model.Reset();
                    break;
            }
        }
    }
}
