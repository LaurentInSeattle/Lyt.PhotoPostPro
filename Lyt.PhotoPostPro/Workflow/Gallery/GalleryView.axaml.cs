namespace Lyt.PhotoPostPro.Workflow.Gallery;

public partial class GalleryView : View
{
    protected override void OnDataContextChanged(object? sender, EventArgs e) { }

    private void OnImagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if ( this.DataContext is GalleryViewModel galleryViewModel )
        {
            galleryViewModel.OnImageClicked(); 

            // Mark event as handled so it doesn't bubble up
            e.Handled = true;
        }
    }
}