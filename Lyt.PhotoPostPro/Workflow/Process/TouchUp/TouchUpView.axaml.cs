namespace Lyt.PhotoPostPro.Workflow.Process.TouchUp;

public partial class TouchUpView : View
{
    public TouchUpView()
    {
        this.Image.PointerPressed += this.OnImagePointerPressed;
    }

    protected override void OnDataContextChanged(object? sender, EventArgs e) { }

    public void DrawTargetSpot(string id, double x, double y, double radius)
    {
        List<Ellipse> toRemove = [];
        foreach (var child in this.ImageCanvas.Children)
        {
            if (child is Ellipse oldEllipse && oldEllipse.Tag?.ToString() == id)
            {
                toRemove.Add(oldEllipse);
            }
        }

        foreach (var remove in toRemove)
        {
            this.ImageCanvas.Children.Remove(remove);
        }

        void AddEllipseAtXY(Ellipse ellipse)
        {
            // Position the ellipse on the canvas using Canvas.Left and Canvas.Top
            Canvas.SetLeft(ellipse, x);
            Canvas.SetTop(ellipse, y);

            // Add the ellipse shape to the canvas children collection
            this.ImageCanvas.Children.Add(ellipse);
        }

        double thickness = radius / 8.0;
        var outerEllipse = new Ellipse
        {
            Tag = id,
            Width = radius,
            Height = radius,
            Fill = Brushes.Transparent,
            Stroke = Brushes.White,
            StrokeThickness = thickness,
        };

        AddEllipseAtXY(outerEllipse);

        var innerEllipse = new Ellipse
        {
            Tag = id,
            Width = radius - thickness * 2.0,
            Height = radius - thickness * 2.0,
            Fill = Brushes.Transparent,
            Stroke = Brushes.Black,
            StrokeThickness = thickness,
        };

        x += thickness;
        y += thickness;
        AddEllipseAtXY(innerEllipse);
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
        new ImageClickedMessage(isSourceImage, pixelX, pixelY, wbm).Publish();
        e.Handled = true;
    }
}