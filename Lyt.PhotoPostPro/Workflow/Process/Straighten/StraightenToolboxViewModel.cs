namespace Lyt.PhotoPostPro.Workflow.Process.Straighten;

public sealed partial class StraightenToolboxViewModel : 
    ToolboxViewModel<StraightenToolboxView, StraightenStep>
{
    private readonly StraightenViewModel viewModel;

    public StraightenToolboxViewModel()
    {
        this.viewModel = App.GetRequiredService<StraightenViewModel>();
        this.Color_0 = GuidelineViewModel.Brushes[0];
        this.Color_1 = GuidelineViewModel.Brushes[1];
        this.Color_2 = GuidelineViewModel.Brushes[2];
        this.Color_3 = GuidelineViewModel.Brushes[3];
        this.Color_4 = GuidelineViewModel.Brushes[4];
    }

    protected override string Title => this.Localize("Workflow.Straighten.Title");

    [ObservableProperty]
    public partial SolidColorBrush Color_0 { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush Color_1 { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush Color_2 { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush Color_3 { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush Color_4 { get; set; }

    [ObservableProperty]
    public partial string RotationAngleString { get; set; } = string.Empty;

    [RelayCommand]
    public void OnToggleVerticalGuideline() => this.viewModel.VerticalGuideLineViewModel.ToggleVisibility();

    [RelayCommand]
    public void OnToggleHorizontalGuideline() => this.viewModel.HorizontalGuideLineViewModel.ToggleVisibility();

    [RelayCommand]
    public void OnColorSelect(string parameter)
    {
        if (int.TryParse(parameter, out int colorIndex))
        {
            this.viewModel.VerticalGuideLineViewModel.Colorize(colorIndex);
            this.viewModel.HorizontalGuideLineViewModel.Colorize(colorIndex);
        }
    }

    public override void OnModelStepUpdated(StraightenStep step) => this.UpdateRotationString(step); 

    // public override void Activate(object? _) => this.UpdateRotationString();

    [RelayCommand]
    public void OnRotateClockwiseLarge() => base.model.Rotate(isClockwise: true, 1.0f);

    [RelayCommand]
    public void OnRotateCounterClockwiseLarge() =>  base.model.Rotate(isClockwise: false, 1.0f);

    [RelayCommand]
    public void OnRotateClockwiseSmall() => base.model.Rotate(isClockwise: true, 0.1f);

    [RelayCommand]
    public void OnRotateCounterClockwiseSmall() => base.model.Rotate(isClockwise: false, 0.1f);

    private void UpdateRotationString(StraightenStep step)
    {
        float value = step.RotationAngle;
        string stringValue = value.ToString("+0.0;-0.0;0.0");
        this.RotationAngleString = stringValue + " \u00B0"; // Unicode for degree symbol 
    }
}