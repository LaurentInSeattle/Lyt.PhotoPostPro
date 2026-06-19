namespace Lyt.PhotoPostPro.Workflow.Process.Compose.CompositionGuides;

public partial class DiagonalsCG : UserControl
{
    private const double defaultThickness = 5.5;

    public DiagonalsCG() => this.InitializeComponent();

    /// <summary> ThicknessFactor Styled Property </summary>
    public static readonly StyledProperty<double> ThicknessFactorProperty =
        AvaloniaProperty.Register<DiagonalsCG, double>(
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
        if (sender is DiagonalsCG diagonal)
        {
            diagonal.DiagonalUp.StrokeThickness = defaultThickness * value;
            diagonal.DiagonalDown.StrokeThickness = defaultThickness * value;
        }

        return value;
    }

    /// <summary> Brush Styled Property </summary>
    public static readonly StyledProperty<SolidColorBrush> BrushProperty =
        AvaloniaProperty.Register<DiagonalsCG, SolidColorBrush>(
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
        if (sender is DiagonalsCG diagonal)
        {
            diagonal.DiagonalUp.Stroke = value;
            diagonal.DiagonalDown.Stroke = value;
        }

        return value;
    }
}