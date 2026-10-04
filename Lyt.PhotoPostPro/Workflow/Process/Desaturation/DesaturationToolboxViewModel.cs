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
            this.ToleranceSliderValue = this.tolerance;
            this.FeatherSliderValue = this.feather;
            this.SaturationBaseSliderValue = this.saturationBase;
            this.SaturationBoostSliderValue = this.saturationBoost;

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
            this.ToleranceSliderValue = step.Tolerance;
            this.FeatherSliderValue = step.Feather;
            this.SaturationBaseSliderValue = step.SaturationBase;
            this.SaturationBoostSliderValue = step.SaturationBoost;

            float hue = step.TargetHue;
            ColorUtilities.HslToRgb(hue / 360.0f, 0.7f, 0.6f, out float r, out float g, out float b);
            var color = new global::Avalonia.Media.Color(255, (byte)(r * 255.0f), (byte)(g * 255.0f), (byte)(b * 255.0f));
            this.SetTargetHue(color);
        });
    }


    partial void OnToleranceSliderValueChanged(double value)
    {
        // Slider sends value fine for the model  
        this.tolerance = (float)value;
        this.ToleranceString = value.ToString("+0.0;-0.0;0.0");
        this.UpdateModel();
    }

    partial void OnFeatherSliderValueChanged(double value)
    {
        // Slider sends value fine for the model  
        this.feather = (float)value;
        this.FeatherString = value.ToString("+0.0;-0.0;0.0");
        this.UpdateModel();
    }

    partial void OnSaturationBaseSliderValueChanged(double value)
    {
        // Slider sends value fine for the model  
        this.saturationBase = (float)value;
        this.SaturationBaseString = value.ToString("+0.00;-0.00;0.00");
        this.UpdateModel();
    }

    partial void OnSaturationBoostSliderValueChanged(double value)
    {
        // Slider sends value fine for the model  
        this.saturationBoost = (float)value;
        this.SaturationBoostString = value.ToString("+0.00;-0.00;0.00");
        this.UpdateModel();
    }

    private void UpdateModel(bool fromButton = false)
    {
        if (this.doNotUpdateModel)
        {
            return;
        }

        if (this.RunDesaturationIsDisabled)
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
