namespace Lyt.PhotoPostPro.Workflow.Process.WhiteBalance;

public sealed partial class WhiteBalanceToolboxViewModel : 
    ToolboxViewModel<WhiteBalanceToolboxView, WhiteBalanceStep>, 
    IRecipient<ImageClickedMessage>
{
    private bool doNotUpdateModel;
    private WhiteBalanceStep.WhiteBalanceAlgorithm algorithm ;
    private float kelvin;
    private float temperature;
    private float saturationThreshold;
    private global::Avalonia.Media.Color whitePatch; 

    public WhiteBalanceToolboxViewModel()
    {
        this.whitePatch = Colors.LightGray;
        this.PatchColor = new SolidColorBrush(Colors.Black);
        this.PatchColorState = new SolidColorBrush(Colors.Firebrick);
        this.RunWhitePatchIsDisabled = true;
        this.Subscribe<ImageClickedMessage>();
    }

    protected override string Title => this.Localize("Workflow.WhiteBalance.Title");

    [ObservableProperty]
    public partial string SaturationString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double SaturationSliderValue { get; set; }

    [ObservableProperty]
    public partial string TemperatureString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double TemperatureSliderValue { get; set; }

    [ObservableProperty]
    public partial string KelvinString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double KelvinSliderValue { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush PatchColor { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush PatchColorState { get; set; }

    [ObservableProperty]
    public partial bool RunWhitePatchIsDisabled  { get; set; }

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();
        this.temperature = 0.0f;
        this.saturationThreshold = 0.4f; 

        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Sliders initial positions and string values
            this.TemperatureSliderValue = 0.01; // Force property changed 
            this.TemperatureSliderValue = this.temperature;
            this.SaturationSliderValue = this.saturationThreshold;

            this.RunWhitePatchIsDisabled = true;
            this.PatchColorState = new SolidColorBrush(Colors.Firebrick);
        });
    }

    public override void OnModelStepUpdated(WhiteBalanceStep step) => this.UpdateSliders(step);

    public void Receive(ImageClickedMessage message)
    {
        // Calculate white patch color by averaging colors on a 3 by 3 area on the image
        global::Avalonia.Media.Color patchColor =
            message.WriteableBitmap.GetColorAroundPixel(message.PixelX, message.PixelY);
        this.whitePatch = patchColor;
        this.PatchColor = new SolidColorBrush(patchColor);
        bool canRunWhitePatch = this.whitePatch.Luminance() >= 0.5;
        this.RunWhitePatchIsDisabled = !canRunWhitePatch;
        this.PatchColorState = new SolidColorBrush(canRunWhitePatch ? Colors.LightGreen: Colors.Firebrick);
    }

    [RelayCommand]
    public void OnWhitePatch()
    {
        this.algorithm = WhiteBalanceStep.WhiteBalanceAlgorithm.WhitePatch;
        this.UpdateModel() ;
    }

    private void UpdateSliders(WhiteBalanceStep step)
    {
        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Here we need to undo the operations done reading the sliders 
            // No transform for the staturation threshold 
            this.SaturationSliderValue = step.SaturationThreshold;

            // More later here 
        });
    }

    partial void OnKelvinSliderValueChanged(double value)
    {
        // Slider sends 1000.0 to +40000.0, fine for the model  
        this.algorithm = WhiteBalanceStep.WhiteBalanceAlgorithm.TannerHelland;
        this.kelvin = (float)value;
        int intValue = (int)value; 
        this.KelvinString = intValue.ToString("D");
        this.UpdateModel();
    }

    partial void OnTemperatureSliderValueChanged(double value)
    {
        // Slider sends -100.0 to +100.0, fine for the model  
        this.algorithm = WhiteBalanceStep.WhiteBalanceAlgorithm.ColorMatrix;
        this.temperature = (float)value;
        this.TemperatureString = value.ToString("+0.0;-0.0;0.0");
        this.UpdateModel();
    }

    partial void OnSaturationSliderValueChanged(double value)
    {
        // Slider sends 0.0 to +1.0, fine for the model  
        this.algorithm = WhiteBalanceStep.WhiteBalanceAlgorithm.FilteredGrayWorldAWB; 
        this.saturationThreshold = (float)value;
        this.SaturationString = value.ToString("+0.00;-0.00;0.00");
        this.UpdateModel();
    }

    private void UpdateModel()
    {
        if ( this.doNotUpdateModel)
        {
            return; 
        }

        switch (this.algorithm)
        {
            case WhiteBalanceStep.WhiteBalanceAlgorithm.FilteredGrayWorldAWB:
                this.model.FilteredGrayWorldAWB(this.saturationThreshold);
                break;

            case WhiteBalanceStep.WhiteBalanceAlgorithm.ColorMatrix:
                this.model.ColorMatrixWhiteBalance(this.temperature);
                break;

            case WhiteBalanceStep.WhiteBalanceAlgorithm.TannerHelland:
                this.model.TannerHellandWhiteBalance(this.kelvin);
                break;

            case WhiteBalanceStep.WhiteBalanceAlgorithm.WhitePatch:
                // Model uses normalized values 
                this.model.WhitePatchWhiteBalance(
                    this.whitePatch.R / 255.0f, 
                    this.whitePatch.G / 255.0f, 
                    this.whitePatch.B / 255.0f);
                break;

            default:
                break;
        }
    }
}
