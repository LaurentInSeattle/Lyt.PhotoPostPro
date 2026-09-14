namespace Lyt.PhotoPostPro.Workflow.Process.Compose.CompositionGuides;

public partial class PyramidCG : UserControl
{
    private const double defaultThickness = 5.5;

    public PyramidCG() => this.InitializeComponent();

    /// <summary> ThicknessFactor Styled Property </summary>
    public static readonly StyledProperty<double> ThicknessFactorProperty =
        AvaloniaProperty.Register<PyramidCG, double>(
            nameof(ThicknessFactor),
            defaultValue: 0.7,
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
        if (sender is PyramidCG diagonal)
        {
            diagonal.PyramidUp.StrokeThickness = defaultThickness * value;
            diagonal.PyramidDown.StrokeThickness = defaultThickness * value;
        }

        return value;
    }

    /// <summary> Brush Styled Property </summary>
    public static readonly StyledProperty<SolidColorBrush> BrushProperty =
        AvaloniaProperty.Register<PyramidCG, SolidColorBrush>(
            nameof(Brush),
            defaultValue: new SolidColorBrush(Colors.AntiqueWhite),
            inherits: false,
            defaultBindingMode: BindingMode.OneWay,
            validate: null,
            coerce: CoerceBrush,
            enableDataValidation: false);

    /// <summary> Gets or sets the Brush property.</summary>
    public SolidColorBrush Brush
    {
        get => this.GetValue(BrushProperty);
        set => this.SetValue(BrushProperty, value);
    }

    private static SolidColorBrush CoerceBrush(AvaloniaObject sender, SolidColorBrush value)
    {
        if (sender is PyramidCG diagonal)
        {
            diagonal.PyramidUp.Stroke = value;
            diagonal.PyramidDown.Stroke = value;
        }

        return value;
    }
}