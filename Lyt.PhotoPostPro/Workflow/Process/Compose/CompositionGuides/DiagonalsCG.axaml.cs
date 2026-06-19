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
}