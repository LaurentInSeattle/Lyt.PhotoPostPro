namespace Lyt.PhotoPostPro.Workflow.Process.TouchUp;

public sealed partial class TouchUpToolboxViewModel : 
    ToolboxViewModel<TouchUpToolboxView, TouchUpStep>,
    IRecipient<ImageClickedMessage>
{
    private bool doNotUpdateModel;
    private int spotPixelX;
    private int spotPixelY;
    private int cleanPixelX;
    private int cleanPixelY;
    private double radius ;

    public TouchUpToolboxViewModel()
    {
        this.Subscribe<ImageClickedMessage>();
    }

    public void Receive(ImageClickedMessage message)
    {
        if ( !this.IsActivated)
        {
            return; 
        }

        this.spotPixelX = message.PixelX;
        this.spotPixelY = message.PixelY;

        var vm = App.GetRequiredService<TouchUpViewModel>();
        if ( vm is not null && vm.IsBound)
        {
            var bitmapSize = message.WriteableBitmap.PixelSize;
            int largestDimension = Math.Max(bitmapSize.Height, bitmapSize.Width);
            this.radius = largestDimension / 70.0;
            double x = this.spotPixelX - this.radius / 2.0;
            double y = this.spotPixelY - this.radius / 2.0;
            vm.View.DrawTargetSpot("clickedPixel", x, y, this.radius);
        }
    }

#pragma warning disable CA1822 // Mark members as static
    // RelayCommand's cannot be static 


#pragma warning restore CA1822
}
