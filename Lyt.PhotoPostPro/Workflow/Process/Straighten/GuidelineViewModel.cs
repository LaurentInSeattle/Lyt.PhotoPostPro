namespace Lyt.PhotoPostPro.Workflow.Process.Straighten;

public sealed partial class GuidelineViewModel(bool isVertical) : ViewModel<GuidelineView>
{
    public static readonly SolidColorBrush[] Brushes =
        [
            new SolidColorBrush(Colors.DarkBlue),
            new SolidColorBrush(Colors.Firebrick),
            new SolidColorBrush(Colors.LightSeaGreen),
            new SolidColorBrush(Colors.Goldenrod),
            new SolidColorBrush(Colors.WhiteSmoke),
        ];

    [ObservableProperty]
    public partial bool IsVertical { get; set; } = isVertical;

    [ObservableProperty]
    public partial SolidColorBrush LineColor { get; set; } = Brushes[3];

    [ObservableProperty]
    public partial bool IsVisible { get; set; } = true;

    public void ToggleVisibility() => this.IsVisible = !this.IsVisible;

    public void Colorize(int brushIndex)
    {
        if ((brushIndex < 0) || (brushIndex >= Brushes.Length))
        {
            return;
        }

        this.LineColor = Brushes[brushIndex];
    }
}
