namespace Lyt.PhotoPostPro.Workflow.Process.Compose;

public sealed partial class ComposeToolboxViewModel : 
    ToolboxViewModel<ComposeToolboxView, CompositionStep>, 
    IToolboxViewModel
{
    private readonly ComposeViewModel viewModel;

    private int x;
    private int y;
    private int dx;
    private int dy;

    public ComposeToolboxViewModel() => this.viewModel = App.GetRequiredService<ComposeViewModel>();

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();

        // Enforce property changed 
        this.SelectedIndex = 0;
        this.SelectedIndex = 1;
    }

    protected override string Title => this.Localize("Workflow.Compose.Title");

    [ObservableProperty]
    public partial List<string> SupportedGuides{ get; set; } = 
        // TODO: Needs localization 
        [
            " -- None -- " , // => this.View.ZeroCG,
            " Rule of Thirds"  , // 1 => this.View.ThirdsCG,
            " Frame "  , // 2 => this.View.FrameCG,
            " Middle Lines "  , // 3 => this.View.HalvesCG,
            " Diagonals "  , // 4 => this.View.DiagonalsCG,
            " Golden Triangle Up "  , // 5 => this.View.GoldenUpCG,
            " Golden Triangle Down "  , // 6 => this.View.GoldenDownCG,
        ];

    [ObservableProperty]
    public partial int SelectedIndex { get; set; }

    [ObservableProperty]
    public partial string TopValueString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LeftValueString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BottomValueString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RightValueString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string WidthValueString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HeightValueString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AspectRatioValueString { get; set; } = string.Empty;

    [RelayCommand]
    public void OnTogglePreview() => this.viewModel.CropGridViewModel.TogglePreview();

    [RelayCommand]
    public void OnPlusTop()=> this.viewModel.CropGridViewModel.OnPlusTop();

    [RelayCommand]
    public void OnMinusTop() => this.viewModel.CropGridViewModel.OnMinusTop();

    [RelayCommand]
    public void OnPlusLeft() => this.viewModel.CropGridViewModel.OnPlusLeft();

    [RelayCommand]
    public void OnMinusLeft() => this.viewModel.CropGridViewModel.OnMinusLeft();

    [RelayCommand]
    public void OnPlusBottom() => this.viewModel.CropGridViewModel.OnPlusBottom();

    [RelayCommand]
    public void OnMinusBottom() => this.viewModel.CropGridViewModel.OnMinusBottom();

    [RelayCommand]
    public void OnPlusRight() => this.viewModel.CropGridViewModel.OnPlusRight();

    [RelayCommand]
    public void OnMinusRight() => this.viewModel.CropGridViewModel.OnMinusRight();

    internal void OnCropRectangleChanged(int ix, int iy, int idx, int idy)
    {
        this.x = ix;
        this.y = iy;
        this.dx = idx;
        this.dy = idy;
        this.DumpRectangle();

        this.LeftValueString = ix.ToString("D");
        this.TopValueString = iy.ToString("D");
        this.RightValueString = (ix + idx ).ToString("D");
        this.BottomValueString = (iy+ idy).ToString("D");
        this.WidthValueString = idx.ToString("D");
        this.HeightValueString = idy.ToString("D");
        float aspectRatio = idx / (float)idy; 
        this.AspectRatioValueString = aspectRatio.ToString("F2");
    }

    public override void OnAfterReset() => this.SetCropRectangle();

    public override void Activate(object? _) 
        => Dispatch.OnUiThread (()=> { this.SetCropRectangle(); }, DispatcherPriority.Background);

    private void SetCropRectangle()
    {
        var step = base.ModelStep<CompositionStep>();
        int ix = step.X;
        int iy = step.Y;
        int idx = step.Dx; 
        int idy = step.Dy;
        this.viewModel.CropGridViewModel.SetCropRectangle ( ix, iy, idx, idy);
    } 

    public override void OnBeforeNext() => this.model.Crop(this.x, this.y, this.dx, this.dy);

    partial void OnSelectedIndexChanged(int value) => this.viewModel.SelectCropGuidelines(value);
    
    [Conditional("DEBUG")]
    private void DumpRectangle() => Debug.WriteLine($" Crop: X:{x}  Y:{y}  W: {dx}  H: {dy}");
}
