namespace Lyt.PhotoPostPro.Workflow.Process.Lut;

/// <summary>  Creates a LUT Image view model </summary>
public sealed partial class LutImageViewModel: 
    ViewModel<LutImageView>
{
    public readonly LutMetadata Metadata;

    private readonly ISelectListener parent;

    public LutImageViewModel(ISelectListener parent, LutMetadata metadata, WriteableBitmap image)
    {
        this.parent = parent;
        this.Metadata = metadata;
        this.Image = image;
        this.Title = 
            metadata.IsEmpty ?
                this.Localize("Workflow.Lut.Original") :
                metadata.FriendlyName;
    }

    [ObservableProperty]
    public partial string Title { get; set; } 

    [ObservableProperty]
    public partial WriteableBitmap Image { get; set; }

    internal void OnSelect() => this.parent.OnSelect(this);

}

