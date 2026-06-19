namespace Lyt.PhotoPostPro.Workflow.Process.Compose.CompositionGuides;

public partial class GoldenUpCG : UserControl
{
    private const double defaultPathThickness = 5.5;
    private const double defaultRectangleThickness = 2.5;

    public GoldenUpCG() => this.InitializeComponent();

    /// <summary> ThicknessFactor Styled Property </summary>
    public static readonly StyledProperty<double> ThicknessFactorProperty =
        AvaloniaProperty.Register<GoldenUpCG, double>(
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
        if (sender is GoldenUpCG golden)
        {
            golden.Path_1_S7.StrokeThickness = defaultPathThickness * value;
            golden.Path_2_S7.StrokeThickness = defaultPathThickness * value;
            golden.Rectangle_H2.Height = defaultRectangleThickness * value;
            golden.Rectangle_W2.Width = defaultRectangleThickness * value;
        }

        return value;
    }
}