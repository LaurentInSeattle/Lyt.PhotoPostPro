namespace Lyt.PhotoPostPro.Controls;

public partial class GammaCurveImageControl : UserControl
{
    public enum BrushColor { Red, Green, Blue, Luminosity }

    private readonly SolidColorBrush brushRedDark = new(Colors.DarkRed);
    private readonly SolidColorBrush brushGreenDark = new(Colors.DarkGreen);
    private readonly SolidColorBrush brushBlueDark = new(Colors.DarkBlue);
    private readonly SolidColorBrush brushGrayDark = new(Colors.DarkGray);

    private readonly SolidColorBrush brushRed = new(Colors.Salmon);
    private readonly SolidColorBrush brushGreen = new(Colors.MediumSeaGreen);
    private readonly SolidColorBrush brushBlue = new(Colors.DodgerBlue);
    private readonly SolidColorBrush brushGray = new(Colors.OldLace);

    public GammaCurveImageControl() => this.InitializeComponent();

    public void Load(Curve curve)
    {
        float[] values = curve.Points;
        const double canvasHeight = 256.0;
        const double canvasWidth = 512.0;

        this.MainCanvas.Children.Clear();


        // Draw the diagonal 
        PathGeometry geometryDiagonal = new();
        using (var context = geometryDiagonal.Open())
        {
            // Geometry is not filled 
            Point start = new(0.0, canvasHeight);
            context.BeginFigure(start, isFilled: false);

            // Add last point at top right and do NOT close the figure 
            Point end = new(canvasWidth, 0.0);
            context.LineTo(end);

            // Figure is not closed
            context.EndFigure(isClosed: false);
        }

        var diagonalPath = new global::Avalonia.Controls.Shapes.Path
        {
            Stroke = brushRed,
            StrokeThickness = 2,
            Data = geometryDiagonal,
            Opacity = 0.7,
        };

        this.MainCanvas.Children.Add(diagonalPath);

        // Curve path to show the curve points : Geometry is not filled 
        PathGeometry geometryCurve = new();
        using (var context = geometryCurve.Open())
        {
            for (int i = 0; i < values.Length; i++)
            {
                // CONSIDER :
                // Compensate for the non-square frame so that we can see a significant change on the Y axis 
                double y = canvasHeight * (1.0 - values[i]);
                double x = i * canvasWidth / (values.Length - 1);
                Point point = new(x, y);

                if (i == 0)
                {
                    // Geometry is not filled 
                    context.BeginFigure(point, isFilled: false);
                }
                else if (i == values.Length - 1)
                {
                    // Add last point at bottom right and do NOT close the figure 
                    context.LineTo(point);

                    // Figure is not closed
                    context.EndFigure(isClosed: false);
                }
                else
                {
                    context.LineTo(point);
                }
            }
        }

        var curvePath = new global::Avalonia.Controls.Shapes.Path
        {
            Stroke = brushGrayDark,
            StrokeThickness = 4,
            Data = geometryCurve,
            Opacity = 1.0,
        };

        this.MainCanvas.Children.Add(curvePath);
    }
}