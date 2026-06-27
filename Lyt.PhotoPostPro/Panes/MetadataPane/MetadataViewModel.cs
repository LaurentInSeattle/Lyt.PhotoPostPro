namespace Lyt.PhotoPostPro.Panes.MetadataPane;

public sealed partial class MetadataViewModel : 
    ViewModel<MetadataView>, 
    IRecipient<MetadataGeneratedMessage>
{
    [ObservableProperty]
    public partial string FullPath { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Filename { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string SizeMB { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Dimensions { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string FileDateTime { get; private set; } = string.Empty;

    public MetadataViewModel() => this.Subscribe<MetadataGeneratedMessage>();

    public void Receive(MetadataGeneratedMessage message)
    {
        Dispatch.OnUiThread(() =>
        {
            this.Update(message.ProcessMetadata); 
        }, DispatcherPriority.ApplicationIdle);
    }

    private void Update(ProcessMetadata meta)
    {
        this.Filename = string.Format("{0} : {1}", meta.Extension, meta.Filename);
        this.SizeMB = meta.SizeMB + " on disk";
        this.Dimensions = meta.Dimensions + " pixels";
        string sep = new(System.IO.Path.DirectorySeparatorChar, 1);
        string sepSpace = " " + sep + " "; 
        this.FullPath = meta.FullPath.Replace(sep, sepSpace);
        var localDT = meta.FileDateUTC.ToLocalTime();
        this.FileDateTime =
            string.Format("File created: {0} at {1}", localDT.ToLongDateString(), localDT.ToLongTimeString()); 
    }
}
