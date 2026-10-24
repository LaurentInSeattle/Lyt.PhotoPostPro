namespace Lyt.PhotoPostPro.Workflow.Process.TouchUp;

public partial class TouchUpView : View
{
    private const string SpotTag = "Spot";
    private const string PatchTag = "Patch";

    public TouchUpView()
    {
        this.Image.PointerPressed += this.OnImagePointerPressed;
    }

    protected override void OnDataContextChanged(object? sender, EventArgs e) { }

    public bool HasTarget(bool isSpot)
    {
        string tag = isSpot ? SpotTag : PatchTag;
        foreach (var child in this.ImageCanvas.Children)
        {
            if (child is Ellipse oldEllipse && oldEllipse.Tag is string tagString && tagString == tag)
            {
                return true;
            }
        }

        return false;
    }

    public void ClearTarget(bool isSpot)
    {
        string tag = isSpot ? SpotTag : PatchTag;
        List<Ellipse> toRemove = [];
        foreach (var child in this.ImageCanvas.Children)
        {
            if (child is Ellipse oldEllipse && oldEllipse.Tag is string tagString && tagString == tag)
            {
                toRemove.Add(oldEllipse);
            }
        }

        foreach (var remove in toRemove)
        {
            this.ImageCanvas.Children.Remove(remove);
        }
    }

    public void DrawTarget(bool isSpot, double x, double y, double diameter, double thickness)
    {
        this.ClearTarget(isSpot);

        void AddEllipseAtXY(Ellipse ellipse)
        {
            ellipse.Tag = isSpot ? SpotTag : PatchTag;
            ellipse.Fill = Brushes.Transparent; 
            ellipse.StrokeThickness = thickness;
            ellipse.IsHitTestVisible = false;

            // Position the ellipse on the canvas using Canvas.Left and Canvas.Top
            Canvas.SetLeft(ellipse, x);
            Canvas.SetTop(ellipse, y);

            // Add the ellipse shape to the canvas children collection
            this.ImageCanvas.Children.Add(ellipse);
        }

        var outerEllipse = new Ellipse
        {
            Width = diameter,
            Height = diameter,
            Stroke = isSpot ? Brushes.OrangeRed : Brushes.LawnGreen,
        };

        AddEllipseAtXY(outerEllipse);

        double innerDiameter = diameter - thickness * 2.0;
        if (innerDiameter > 1.0)
        {
            var innerEllipse = new Ellipse
            {
                Width = innerDiameter,
                Height = innerDiameter,
                Stroke = isSpot ? Brushes.DarkRed : Brushes.DarkGreen,
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