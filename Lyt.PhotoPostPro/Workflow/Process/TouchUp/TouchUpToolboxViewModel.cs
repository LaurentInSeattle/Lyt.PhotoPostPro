namespace Lyt.PhotoPostPro.Workflow.Process.TouchUp;

public sealed partial class TouchUpToolboxViewModel : 
    ToolboxViewModel<TouchUpToolboxView, TouchUpStep>,
    IRecipient<ImageClickedMessage>
{
    private bool doNotUpdateModel;
    private int clickedPixelX;
    private int clickedPixelY;

    public void Receive(ImageClickedMessage message)
    {
        // Calculate white patch color by averaging colors on a 3 by 3 area on the image
        this.clickedPixelX = message.PixelX;
        this.clickedPixelY = message.PixelY;

        //global::Avalonia.Media.Color patchColor =
        //        message.WriteableBitmap.GetColorAroundPixel(this.clickedPixelX, this.clickedPixelY);
        //this.SetWhitePatch(patchColor);
    }

#pragma warning disable CA1822 // Mark members as static
    // RelayCommand's cannot be static 


#pragma warning restore CA1822
}
