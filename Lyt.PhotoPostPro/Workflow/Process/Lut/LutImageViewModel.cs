namespace Lyt.PhotoPostPro.Workflow.Process.Lut;

/// <summary>  Creates a thumbnail view model </summary>
public sealed partial class LutImageViewModel(ISelectListener parent, LutMetadata metadata, WriteableBitmap image) : 
    ViewModel<LutImageView>
{
    public readonly LutMetadata Metadata = metadata;

    private readonly ISelectListener parent = parent;

    [ObservableProperty]
    public partial string Title { get; set; } = metadata.IsEmpty ? "Original" : metadata.FriendlyName;

    [ObservableProperty]
    public partial WriteableBitmap Image { get; set; } = image;

    internal void OnSelect() => this.parent.OnSelect(this);

#pragma warning disable CA1822 // Mark members as static
    // Relay commands cannot be static 


#pragma warning restore CA1822 // Mark members as static

    //internal void ShowDeselected(Model.GameObjects.Game game)
    //{
    //    if (this.Game == game)
    //    {
    //        return;
    //    }

    //    if (this.IsBound)
    //    {
    //        this.View.Deselect();
    //    }
    //}

    //internal void ShowSelected()
    //{
    //    if (this.IsBound)
    //    {
    //        this.View.Select();
    //    }
    //}

}

