namespace Lyt.PhotoPostPro.Workflow.Process.Shared;

public partial class BeforeAfterView : UserControl
{
    public BeforeAfterView()
    {
        this.InitializeComponent();
        this.SourceImagePortrait.PointerPressed += OnImagePointerPressed;
        this.SourceImageLandscape.PointerPressed += OnImagePointerPressed;
        this.ResultImagePortrait.PointerPressed += OnImagePointerPressed;
        this.ResultImageLandscape.PointerPressed += OnImagePointerPressed;
    }

    private void OnImagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Image imgControl && imgControl.Source is WriteableBitmap wbm)
        {
            // Obtain click position relative to the visually scaled layout control
            Point relativeClick = e.GetPosition(imgControl);

            // Translate layout points to matching underlying bitmap pixel indices
            int width = wbm.PixelSize.Width;
            int pixelX = (int)(relativeClick.X * (width / imgControl.Bounds.Width));
            int height = wbm.PixelSize.Height;
            int pixelY = (int)(relativeClick.Y * (height / imgControl.Bounds.Height));

            // Stay away from the border by one pixel 
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

            string? name = imgControl.Name;
            if (!string.IsNullOrWhiteSpace(name))
            {
                bool isSourceImage = name.StartsWith("Source");
                new ImageClickedMessage(isSourceImage, pixelX, pixelY, wbm).Publish();
            }
        }
    }
}