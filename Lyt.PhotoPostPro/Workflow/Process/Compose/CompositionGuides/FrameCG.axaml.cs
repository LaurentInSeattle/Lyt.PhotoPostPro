namespace Lyt.PhotoPostPro.Workflow.Process.Compose.CompositionGuides;

public partial class FrameCG : UserControl
{
    private const double defaultThickness = 2.3;

    public FrameCG()
    {
        this.InitializeComponent();
        this.LayoutUpdated += this.OnLayoutUpdated;
    }

    private void OnLayoutUpdated(object? sender, EventArgs e) 
        => this.AdjustLayout(defaultThickness * this.ThicknessFactor); 

    /// <summary> ThicknessFactor Styled Property </summary>
    public static readonly StyledProperty<double> ThicknessFactorProperty =
        AvaloniaProperty.Register<FrameCG, double>(
            nameof(ThicknessFactor),
            defaultValue: defaultThickness,
            inherits: false,
            defaultBindingMode: BindingMode.OneWay,
            validate: null,
            coerce: CoerceThicknessFactor,
            enableDataValidation: false);

    /// <summary> Gets or sets the ThicknessFactor property.</summary>
    public double ThicknessFactor
    {
        get => this.GetValue(ThicknessFactorProperty);
        set => this.SetValue(ThicknessFactorProperty, value);
    }

    private static double CoerceThicknessFactor(AvaloniaObject sender, double value)
    {
        if (sender is FrameCG frame)
        {
            frame.AdjustLayout(defaultThickness * value );
        }

        return value;
    }

    private void AdjustLayout(double value)
    {
        var bounds = this.MainGrid.Bounds;
        double length = 0.12 * Math.Min(bounds.Width, bounds.Height);
        var gridLength = new GridLength(length, GridUnitType.Pixel);
        var cols = this.MainGrid.ColumnDefinitions;
        var rows = this.MainGrid.RowDefinitions;
        cols[0].Width = gridLength;
        cols[4].Width = gridLength;
        rows[0].Height = gridLength;
        rows[4].Height = gridLength;

        double thickness = defaultThickness * value;
        var gridLengthFrame = new GridLength(thickness, GridUnitType.Pixel);
        cols[1].Width = gridLengthFrame;
        cols[3].Width = gridLengthFrame;
        rows[1].Height = gridLengthFrame;
        rows[3].Height = gridLengthFrame;
    }
}