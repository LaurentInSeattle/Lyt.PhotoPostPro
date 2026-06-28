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

    [ObservableProperty]
    public partial string Make { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Model { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Captured { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Aperture { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Exposure { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string ExposureBias { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string IsoSpeed { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string FocalLength { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string WithFlash { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Location { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Latitude { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Longitude { get; private set; } = string.Empty;

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

        if (meta.HasExifMetadata)
        {
            this.Make = meta.Make;
            this.Model = meta.Model;
            this.Captured =
                string.Format("Shot: {0} at {1}", meta.Captured.ToLongDateString(), meta.Captured.ToLongTimeString());
            this.Aperture = "Aperture: " + meta.Aperture;
            this.IsoSpeed = "ISO Speed: " + meta.IsoSpeed;
            this.Exposure = "Exposure: " + meta.Exposure;
            this.ExposureBias = "Bias: " + meta.ExposureBias;
            this.FocalLength = "Focal: " + meta.FocalLength;
            this.WithFlash = meta.WithFlash ? "With Flash" : "No Flash"; 
        }

        if (meta.HasLocationMetadata)
        {
            this.Location = "Location"; 
            this.Latitude = "Lat.: " + meta.LatitudeString;
            this.Longitude = "Long.: " + meta.LongitudeString;
        } 
        else
        {
            this.Location = string.Empty;
            this.Latitude = string.Empty;
            this.Longitude = string.Empty;
        }
    }
}
