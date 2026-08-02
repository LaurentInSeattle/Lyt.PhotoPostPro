namespace Lyt.PhotoPostPro.Workflow.Process.Shared;

public partial class BeforeAfterView : UserControl
{
    public BeforeAfterView()
    {
        this.InitializeComponent();
        this.SourceImagePortrait.PointerPressed += this.OnImagePointerPressed;
        this.SourceImageLandscape.PointerPressed += this.OnImagePointerPressed;
        this.ResultImagePortrait.PointerPressed += this.OnImagePointerPressed;
        this.ResultImageLandscape.PointerPressed += this.OnImagePointerPressed;
        this.VerticalSplitter.DragCompleted += this.OnDrag;
        this.VerticalSplitter.DragDelta += this.OnDrag;
        this.HorizontalSplitter.DragCompleted += this.OnDrag;
        this.HorizontalSplitter.DragDelta += this.OnDrag;
    }

    public void ZoomToFit()
    {
        if (this.SourceImagePortrait.IsVisible)
        {
            this.SourceImagePortrait.ZoomToFit();
        }

        if (this.ResultImagePortrait.IsVisible)
        {
            this.ResultImagePortrait.ZoomToFit();
        }

        if (this.SourceImageLandscape.IsVisible)
        {
            this.SourceImageLandscape.ZoomToFit();
        }

        if (this.ResultImageLandscape.IsVisible)
        {
            this.ResultImageLandscape.ZoomToFit();
        }
    }

    private void OnDrag(object? _, VectorEventArgs e) => this.ZoomToFit();

    private void OnImagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not ZoomableImage imgControl || !imgControl.IsVisible || imgControl.Image is not WriteableBitmap wbm)
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

        // Translate to image coordinates 
        Point pixel = imgControl.PointToImage(relativeClick);
        int pixelX = (int)pixel.X;
        int pixelY = (int)pixel.Y;
        if (pixelX == 0 && pixelY == 0)
        {
            // Out of bounds, by design of PointToImage above 
            return;
        }

        // Stay away from the border by one pixel, because later we will average pixel colors
        // in a 3x3 square around this center pixel 
        int width = wbm.PixelSize.Width;
        int height = wbm.PixelSize.Height;
        if (pixelX == 0)
        {
            pixelX = 1;
        }
        else if (pixelX == width - 1)
        {
            pixelX = width - 2;
        }

        if (pixelY == 0)
        {
            pixelY = 1;
        }
        else if (pixelY == height - 1)
        {
            pixelY = height - 2;
        }

        bool isSourceImage = name.StartsWith("Source");
        new ImageClickedMessage(isSourceImage, pixelX, pixelY, wbm).Publish();
        e.Handled = true;
    }
}