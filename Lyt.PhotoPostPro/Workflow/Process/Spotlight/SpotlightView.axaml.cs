namespace Lyt.PhotoPostPro.Workflow.Process.Spotlight;

using static System.Net.WebRequestMethods;

public partial class SpotlightView : View
{
    private const string SpotTag = "Spot";

    public SpotlightView()
        => this.Image.PointerPressed += this.OnImagePointerPressed;

    protected override void OnDataContextChanged(object? sender, EventArgs e) { }

    public bool HasTarget()
    {
        foreach (var child in this.ImageCanvas.Children)
        {
            if (child is Ellipse oldEllipse && oldEllipse.Tag is string tagString && tagString == SpotTag)
            {
                return true;
            }
        }

        return false;
    }

    public void ClearTarget()
    {
        List<Ellipse> toRemove = [];
        foreach (var child in this.ImageCanvas.Children)
        {
            if (child is Ellipse oldEllipse && oldEllipse.Tag is string tagString && tagString == SpotTag)
            {
                toRemove.Add(oldEllipse);
            }
        }

        foreach (var remove in toRemove)
        {
            this.ImageCanvas.Children.Remove(remove);
        }
    }

    public void DrawTarget(double x, double y, double axisX, double axisY, double rotation, double thickness)
    {
        this.ClearTarget();

        var renderTransform = new RotateTransform(rotation); 

        void AddEllipseAtXY(Ellipse ellipse)
        {
            ellipse.Tag = SpotTag;
            ellipse.StrokeThickness = thickness;
            ellipse.Fill = Brushes.Transparent;
            ellipse.IsHitTestVisible = false;
            ellipse.RenderTransform = renderTransform;

            // Position the ellipse on the canvas using Canvas.Left and Canvas.Top
            Canvas.SetLeft(ellipse, x);
            Canvas.SetTop(ellipse, y);

            // Add the ellipse shape to the canvas children collection
            this.ImageCanvas.Children.Add(ellipse);
        }

        var outerEllipse = new Ellipse
        {
            Width = axisX,
            Height = axisY,
            Stroke = Brushes.Goldenrod,
        };

        AddEllipseAtXY(outerEllipse);

        double width = axisX - thickness * 2.0;
        double height = axisY - thickness * 2.0;
        if ((width > 2.0) && (height > 2.0))
        {
            var innerEllipse = new Ellipse
            {
                Width = width,
                Height = height,
                Stroke = Brushes.Black,
            };

            x += thickness;
            y += thickness;
            AddEllipseAtXY(innerEllipse);
        }
    }

    private void OnImagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Image imgControl || !imgControl.IsVisible || imgControl.Source is not WriteableBitmap wbm)
        {
            return;
        }

        string? name = imgControl.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        // Obtain click position relative to the control
        Point relativeClick = e.GetPosition(imgControl);
        int pixelX = (int)relativeClick.X;
        int pixelY = (int)relativeClick.Y;

        // Debug.WriteLine($"OnImagePointerPressed: {name} clicked at ({pixelX}, {pixelY})");

        bool isSourceImage = name.StartsWith("Source");
        bool isShifted = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        new ImageClickedMessage(isSourceImage, isShifted, pixelX, pixelY, wbm).Publish();
        e.Handled = true;
    }
}