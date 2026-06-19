namespace Lyt.PhotoPostPro.Workflow.Process.Compose;

public sealed partial class CropGridViewModel(
    ComposeViewModel composeViewModel, ComposeToolboxViewModel composeToolboxViewModel) : ViewModel<CropGridView>
{
    public static readonly SolidColorBrush[] Brushes =
        [
            new SolidColorBrush(Colors.DarkBlue),
            new SolidColorBrush(Colors.Firebrick),
            new SolidColorBrush(Colors.LightSeaGreen),
            new SolidColorBrush(Colors.Goldenrod),
            new SolidColorBrush(Colors.WhiteSmoke),
        ];

    private readonly ComposeViewModel composeViewModel = composeViewModel;
    private readonly ComposeToolboxViewModel composeToolboxViewModel = composeToolboxViewModel;

    private bool preview;
    private SolidColorBrush lastBrush = Brushes[3]; 

    [ObservableProperty]
    public partial bool ShowSplitters { get; set; } = true;

    [ObservableProperty]
    public partial double CropOpacity { get; set; } = 0.6;

    [ObservableProperty]
    public partial double CompositionOpacity { get; set; } = 0.7;

    [ObservableProperty]
    public partial double CompositionSize { get; set; } = 7.0;

    [ObservableProperty]
    public partial SolidColorBrush LineColor { get; set; } = Brushes[3];

    public void OnActivate() => this.View.Activate();

    public void OnDeactivate() => this.View.Deactivate();

    public void Colorize(int brushIndex)
    {
        if ((brushIndex < 0) || (brushIndex >= Brushes.Length))
        {
            return;
        }

        this.LineColor = Brushes[brushIndex];
    }

    public void TogglePreview ()
    {
        this.preview = !this.preview; 
        this.ShowSplitters = !this.preview ;
        this.CropOpacity = this.preview ? 1.0 : 0.6;
        this.CompositionOpacity = this.preview ? 0.0 : 0.4;
        this.LineColor = this.preview ? new SolidColorBrush(Colors.Black) : this.lastBrush;
        this.View.InvalidateVisual(); 
    }

    internal void ClearCrop() => this.View.ClearCrop();

    internal void SetCropRectangle(int x, int y, int dx, int dy) => this.View.SetCrop(x, y, dx, dy);

    internal void OnCropRectangleChanged(double x, double y, double dx, double dy)
    {
        int ix = (int)Math.Round(x);
        int iy = (int)Math.Round(y);
        int idx = (int)Math.Round(dx);
        int idy = (int)Math.Round(dy);
        this.composeToolboxViewModel.OnCropRectangleChanged(ix, iy, idx, idy);
    }

    internal void OnPlusTop() => this.View.Top(1);
    internal void OnMinusTop() => this.View.Top(-1);
    internal void OnPlusBottom() => this.View.Bottom(1);
    internal void OnMinusBottom() => this.View.Bottom(-1);
    internal void OnPlusLeft() => this.View.Left(1);
    internal void OnMinusLeft() => this.View.Left(-1);
    internal void OnPlusRight() => this.View.Right(1);
    internal void OnMinusRight() => this.View.Right(-1);

    internal void SelectCg(int cgIndex)
    {
        this.View.ThirdsCG.IsVisible = false;
        this.View.FrameCG.IsVisible = false;
        this.View.GoldenUpCG.IsVisible = false;
        this.View.GoldenDownCG.IsVisible = false;
        this.View.HalvesCG.IsVisible = false;
        this.View.DiagonalsCG.IsVisible = false;
        
        Control control = cgIndex switch
        {
            0 => this.View.ZeroCG,
            1 => this.View.ThirdsCG,
            2 => this.View.FrameCG,
            3 => this.View.HalvesCG,
            4 => this.View.DiagonalsCG,
            5 => this.View.GoldenUpCG,
            6 => this.View.GoldenDownCG,
            _ => this.View.ZeroCG,
        };

        control.IsVisible = true;
    }
}
