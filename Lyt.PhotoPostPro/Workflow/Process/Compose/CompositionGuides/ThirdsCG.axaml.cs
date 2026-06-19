namespace Lyt.PhotoPostPro.Workflow.Process.Compose.CompositionGuides;

public partial class ThirdsCG : UserControl
{
    private const double defaultThickness = 2.3;

    public ThirdsCG()
    {
        this.InitializeComponent();
        this.LayoutUpdated += this.OnLayoutUpdated;
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
        => this.AdjustLayout(defaultThickness * this.ThicknessFactor);

    /// <summary> ThicknessFactor Styled Property </summary>
    public static readonly StyledProperty<double> ThicknessFactorProperty =
        AvaloniaProperty.Register<ThirdsCG, double>(
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
        if (sender is ThirdsCG frame)
        {
            frame.AdjustLayout(defaultThickness * value);
        }

        return value;
    }

    private void AdjustLayout(double value)
    {
        var cols = this.MainGrid.ColumnDefinitions;
        var rows = this.MainGrid.RowDefinitions;
        double thickness = defaultThickness * value;
        var gridLengthFrame = new GridLength(thickness, GridUnitType.Pixel);
        cols[1].Width = gridLengthFrame;
        cols[3].Width = gridLengthFrame;
        rows[1].Height = gridLengthFrame;
        rows[3].Height = gridLengthFrame;
    }
}