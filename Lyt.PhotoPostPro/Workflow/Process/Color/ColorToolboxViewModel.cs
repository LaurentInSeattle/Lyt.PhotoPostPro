namespace Lyt.PhotoPostPro.Workflow.Process.Color;

public sealed partial class ColorToolboxViewModel :
    ToolboxViewModel<ColorToolboxView, ColorStep>, IDropPathHandler
{
    private bool doNotUpdateModel;

    private ColorStep.ColorAlgorithm algorithm;
    private float saturation;
    private float red;
    private float green;
    private float blue;
    private LutMetadata lutMetadata = LutMetadata.Empty;

    public ColorToolboxViewModel()
    {
        this.DropViewModel = new DropViewModel(this)
        {
            IsVisible = true
        };
    }

    protected override string Title => this.Localize("Workflow.Color.Title");

    [ObservableProperty]
    public partial DropViewModel DropViewModel { get; set; }

    [ObservableProperty]
    public partial string SaturationString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double SaturationSliderValue { get; set; }

    [ObservableProperty]
    public partial string RedString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double RedSliderValue { get; set; }

    [ObservableProperty]
    public partial string GreenString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double GreenSliderValue { get; set; }

    [ObservableProperty]
    public partial string BlueString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double BlueSliderValue { get; set; }

    [ObservableProperty]
    public partial List<string> AvailableLutNames { get; set; } = [];

    [ObservableProperty]
    public partial int SelectedIndex { get; set; }

    public List<LutMetadata> AvailableLuts { get; set; } = [];

    public void OnDropPath(string path, bool isDirectory)
    {
        if ( isDirectory)
        {
            return; 
        }

        this.lutMetadata = new ("New Lut", path, LutFormat.Unknown , IsEmbedded: false) ;
        this.algorithm = ColorStep.ColorAlgorithm.Lut; 
        this.UpdateModel();
    }

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();
        this.saturation = 1.0f;
        this.red = 0.0f;
        this.green = 0.0f;
        this.blue = 0.0f;

        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Sliders initial positions and string values
            this.SaturationSliderValue = this.saturation;

            // Enforce property changed
            this.RedSliderValue = 0.1;
            this.GreenSliderValue = 0.1;
            this.BlueSliderValue = 0.1;

            this.RedSliderValue = this.red;
            this.GreenSliderValue = this.green;
            this.BlueSliderValue = this.blue;

            var metaLuts = LutsManager.BuiltInLuts();
            List<string> list = new(1 + metaLuts.Count)
            { 
               this.Localize("Workflow.Color.None"),
            };

            this.AvailableLuts.Add(LutMetadata.Empty);

            foreach (var metaLut in metaLuts)
            {
                this.AvailableLuts.Add(metaLut);
                list.Add(metaLut.FriendlyName);
            }

            this.AvailableLutNames = list;

            // Enforce property changed 
            this.SelectedIndex = 1;
            this.SelectedIndex = 0;
        });
    }

    public override void OnModelStepUpdated(ColorStep step) => this.UpdateSliders(step);

    private void UpdateSliders(ColorStep step)
    {
        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Here we need to undo the operations done reading the sliders 
            // No transform for the staturation threshold 
            this.SaturationSliderValue = step.SaturationAmount;

            this.RedSliderValue = step.RedAmount;
            this.GreenSliderValue = step.GreenAmount;
            this.BlueSliderValue = step.BlueAmount;
        });
    }

    partial void OnSaturationSliderValueChanged(double value)
    {
        // Slider sends 1.0 to +2.5, fine for the model  
        this.algorithm = ColorStep.ColorAlgorithm.Saturation;
        this.saturation = (float)value;
        this.SaturationString = value.ToString("+0.00;-0.00;0.00");
        this.UpdateModel();
    }

    partial void OnRedSliderValueChanged(double value)
    {
        // Slider sends -1.0 to +1.0, fine for the model  
        this.algorithm = ColorStep.ColorAlgorithm.Vibrance;
        this.red = (float)value;
        this.RedString = value.ToString("+0.00;-0.00;0.00");
        this.UpdateModel();
    }

    partial void OnGreenSliderValueChanged(double value)
    {
        // Slider sends -1.0 to +1.0, fine for the model  
        this.algorithm = ColorStep.ColorAlgorithm.Vibrance;
        this.green = (float)value;
        this.GreenString = value.ToString("+0.00;-0.00;0.00");
        this.UpdateModel();
    }

    partial void OnBlueSliderValueChanged(double value)
    {
        // Slider sends -1.0 to +1.0, fine for the model  
        this.algorithm = ColorStep.ColorAlgorithm.Vibrance;
        this.blue = (float)value;
        this.BlueString = value.ToString("+0.00;-0.00;0.00");
        this.UpdateModel();
    }

    partial void OnSelectedIndexChanged(int value)
    {
        if (value >= 0 && value < this.AvailableLutNames.Count)
        {
            // Wait one frame before launching the image color lookup process
            // If we dont, the app randomly freezes, with no exceptions thrown.
            // Possible Avalonia Bug ? Need to test with latest 12.0.5
            // Debug Output shows:
            // [Control] PlatformImpl is null, couldn't handle input. (PresentationSource #<some number>>)
            Schedule.OnUiThread(66, () =>
                {
                    this.algorithm = ColorStep.ColorAlgorithm.Lut;
                    this.lutMetadata = this.AvailableLuts[value];
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

        switch (this.algorithm)
        {
            case ColorStep.ColorAlgorithm.Saturation:
                this.model.GlobalSaturation(this.saturation);
                break;

            case ColorStep.ColorAlgorithm.Vibrance:
                this.model.Vibrance(this.red, this.green, this.blue);
                break;

            case ColorStep.ColorAlgorithm.Lut:
                if (this.lutMetadata.LutFormat == LutFormat.None)
                {
                    this.model.Workflow.Reset(); 
                }
                else
                {
                    this.model.Lut(this.lutMetadata);
                }

                break;

            default:
                break;
        }

    }
}
