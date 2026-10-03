namespace Lyt.PhotoPostPro.Workflow.Process.Desaturation;

public sealed partial class DesaturationToolboxViewModel :
    ToolboxViewModel<DesaturationToolboxView, DesaturationStep>,
    IRecipient<ImageClickedMessage>
{
    private bool doNotUpdateModel;

    private float targetHue;
    private float tolerance;
    private float feather;
    private float saturationBase;
    private float saturationBoost; 

    private global::Avalonia.Media.Color huePatch;

    public DesaturationToolboxViewModel()
    {
        this.huePatch = Colors.LightGray;
        this.PatchColor = new SolidColorBrush(Colors.LightGray);
        this.PatchColorState = new SolidColorBrush(Colors.Firebrick);
        this.RunDesaturationIsDisabled = true;
        this.Subscribe<ImageClickedMessage>();
    }

    [ObservableProperty]
    public partial string ToleranceString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double ToleranceSliderValue { get; set; }

    [ObservableProperty]
    public partial string FeatherString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double FeatherSliderValue { get; set; }

    [ObservableProperty]
    public partial string SaturationBaseString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double SaturationBaseSliderValue { get; set; }

    [ObservableProperty]
    public partial string SaturationBoostString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double SaturationBoostSliderValue { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush PatchColor { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush PatchColorState { get; set; }

    [ObservableProperty]
    public partial bool RunDesaturationIsDisabled { get; set; }

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();

        if (!this.isFirstLoad)
        {
            return;
        }

        With.Flag(ref this.doNotUpdateModel, () =>
        {
            this.targetHue = 10.0f;
            this.tolerance = 10.0f;
            this.feather = 5.0f;
            this.saturationBase = 0.1f;
            this.saturationBoost = 1.2f;

            // Sliders initial positions and string values
            //this.TemperatureSliderValue = 0.01; // Force property changed 
            //this.TemperatureSliderValue = this.temperature;
            //this.SaturationSliderValue = this.saturationThreshold;

            this.RunDesaturationIsDisabled = true;
            this.PatchColorState = new SolidColorBrush(Colors.Firebrick);
        });

        this.isFirstLoad = false;
    }

    public override void OnModelStepUpdated(DesaturationStep step) => this.UpdateSliders(step);

    public void Receive(ImageClickedMessage message)
    {
        // Calculate white patch color by averaging colors on a 3 by 3 area on the image
        global::Avalonia.Media.Color patchColor =
            message.WriteableBitmap.GetColorAroundPixel(message.PixelX, message.PixelY);
        this.SetTargetHue(patchColor);
    }

    private void SetTargetHue(global::Avalonia.Media.Color patchColor)
    {
        this.huePatch = patchColor;
        this.PatchColor = new SolidColorBrush(patchColor);
        ColorUtilities.RgbToHsl(
            patchColor.R / 255.0f, patchColor.G / 255.0f, patchColor.B / 255.0f, 
            out float hue, out float sat, out float lit);
        this.targetHue = hue * 360.0f; 
        this.RunDesaturationIsDisabled = false;
        this.PatchColorState = new SolidColorBrush(Colors.LightGreen);
    }

    [RelayCommand]
    public void OnWhitePatch()
    {
        this.UpdateModel(fromButton: true);
    }

    private void UpdateSliders(DesaturationStep step)
    {
        With.Flag(ref this.doNotUpdateModel, () =>
        {

            // Here we need to undo the operations done reading the sliders 
            // No transform for the saturation threshold 
            //this.SaturationSliderValue = step.SaturationThreshold;
            //this.TemperatureSliderValue = step.Temperature;

            //byte r = (byte)MathF.Floor(255.0f * step.Red);
            //byte g = (byte)MathF.Floor(255.0f * step.Green);
            //byte b = (byte)MathF.Floor(255.0f * step.Blue);
            //var color = new global::Avalonia.Media.Color(255, r, g, b);
            //this.SetTargetHue(color);
        });
    }


    //partial void OnTemperatureSliderValueChanged(double value)
    //{
    //    // Slider sends -100.0 to +100.0, fine for the model  
    //    this.temperature = (float)value;
    //    this.TemperatureString = value.ToString("+0.0;-0.0;0.0");
    //    this.UpdateModel();
    //}

    //partial void OnSaturationSliderValueChanged(double value)
    //{
    //    // Slider sends 0.0 to +1.0, fine for the model  
    //    this.saturationThreshold = (float)value;
    //    this.SaturationString = value.ToString("+0.00;-0.00;0.00");
    //    this.UpdateModel();
    //}

    private void UpdateModel(bool fromButton = false)
    {
        if (this.doNotUpdateModel)
        {
            return;
        }

        if ( this.RunDesaturationIsDisabled)
        {
            return; 
        }

        if (fromButton)
        {
            this.model.SelectiveDesaturation(
                this.targetHue, this.tolerance, this.feather, this.saturationBase, this.saturationBoost);
        }
        else
        {
            this.ThrottleModelUpdate(() =>
            {
                this.model.SelectiveDesaturation(
                    this.targetHue, this.tolerance, this.feather, this.saturationBase, this.saturationBoost);
            });
        }
    }
}
