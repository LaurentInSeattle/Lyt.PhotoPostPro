namespace Lyt.PhotoPostPro.Controls;

public partial class HistogramImageControl : UserControl
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

    public HistogramImageControl() => this.InitializeComponent();

    public void Load(Histogram histogram, BrushColor color)
    {
        float[] bins = histogram.Bins;
        const double canvasHeight = 340.0;
        const double canvasWidth = 1024.0;

        SolidColorBrush brush =
            color switch
            {
                BrushColor.Red => brushRed,
                BrushColor.Green => brushGreen,
                BrushColor.Blue => brushBlue,
                BrushColor.Luminosity => brushGray,
                _ => brushGray,
            };

        SolidColorBrush brushDark =
            color switch
            {
                BrushColor.Red => brushRedDark,
                BrushColor.Green => brushGreenDark,
                BrushColor.Blue => brushBlueDark,
                BrushColor.Luminosity => brushGrayDark,
                _ => brushGrayDark,
            };

        this.MainCanvas.Children.Clear();

        // First path to fill the graph area 
        // Start point at the bottom left, end point at bottom right 
        Point first = new(0, canvasHeight);
        Point last = new(canvasWidth, canvasHeight);
        PathGeometry geometryArea = new() { FillRule = FillRule.NonZero };
        using (var context = geometryArea.Open())
        {
            context.BeginFigure(first, isFilled: true);
            for (int i = 0; i < 256; i++)
            {
                double x = i * canvasWidth / 255.0;
                double y = canvasHeight * (1.0 - bins[i]);
                Point point = new(x, y);
                context.LineTo(point);
            }

            // Add last point at bottom right and close the figure 
            context.LineTo(last);
            context.EndFigure(isClosed: true);
        }

        var areaPath = new global::Avalonia.Controls.Shapes.Path
        {
            Stroke = brushDark,
            StrokeThickness = 1,
            Fill = brushDark,
            Data = geometryArea,
            Opacity = 0.8,
        };

        this.MainCanvas.Children.Add(areaPath);

        // Second path to hilight the curve points 
        // Geometry is not filled 
        PathGeometry geometryCurve = new();
        using (var context = geometryCurve.Open())
        {
            for (int i = 0; i < 256; i++)
            {
                double x = i * canvasWidth / 255.0;
                double y = canvasHeight * (1.0 - bins[i]);
                Point point = new(x, y);

                if (i == 0)
                {
                    // Geometry is not filled 
                    context.BeginFigure(first, isFilled: false);
                }
                else if (i == 255)
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
            Stroke = brush,
            StrokeThickness = 4,
            Data = geometryCurve,
            Opacity = 1.0,
        };

        this.MainCanvas.Children.Add(curvePath);

        // Add clipping warnings as needed 
        void AddClippingIcon(bool isAtLeft)
        {
            const double iconSize = 40;
            const double topPosition = 12.0;
            const double nudge = 0.6; 
            double leftPosition = isAtLeft ? nudge * iconSize : 1024.0 - (1.0 + nudge) * iconSize;
            var geometry = InformationLevel.Warning.ToIconGeometry();
            PathIcon icon = new()
            {
                Foreground = brush,
                Width = iconSize, Height = iconSize,
                Data = geometry,
            };

            icon.SetValue(Canvas.TopProperty, topPosition);
            icon.SetValue(Canvas.LeftProperty, leftPosition);
            this.MainCanvas.Children.Add(icon);
        }

        if (histogram.IsClippingLow)
        {
            AddClippingIcon(isAtLeft: true);
        }

        if (histogram.IsClippingHigh)

        {
            AddClippingIcon(isAtLeft: false);
        }
    }
}