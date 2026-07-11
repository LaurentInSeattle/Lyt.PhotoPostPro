namespace Lyt.PhotoPostPro.Panes.MetadataPane;

using Lyt.PhotoPostPro.Model.Loader;

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
            this.Update(message.Metadata); 
        }, DispatcherPriority.ApplicationIdle);
    }

    private void Update(Metadata metadata)
    {
        this.Filename = string.Format("{0} : {1}", metadata.Extension, metadata.Filename);
        this.SizeMB = metadata.SizeMB + " on disk";
        this.Dimensions = metadata.Dimensions + " pixels";
        string sep = new(System.IO.Path.DirectorySeparatorChar, 1);
        string sepSpace = " " + sep + " "; 
        this.FullPath = metadata.FullPath.Replace(sep, sepSpace);
        var localDT = metadata.FileDateUTC.ToLocalTime();
        this.FileDateTime =
            string.Format("File created: {0} at {1}", localDT.ToLongDateString(), localDT.ToLongTimeString()); 

        if (metadata.HasExifMetadata)
        {
            this.Make = metadata.Make;
            this.Model = metadata.Model;
            this.Captured =
                string.Format("Shot: {0} at {1}", metadata.Captured.ToLongDateString(), metadata.Captured.ToLongTimeString());
            this.Aperture = "Aperture: " + metadata.Aperture;
            this.IsoSpeed = "ISO Speed: " + metadata.IsoSpeed;
            this.Exposure = "Exposure: " + metadata.Exposure;
            this.ExposureBias = "Bias: " + metadata.ExposureBias;
            this.FocalLength = "Focal: " + metadata.FocalLength;
            this.WithFlash = metadata.WithFlash ? "With Flash" : "No Flash"; 
        }
        else
        {
            this.Make = string.Empty;
            this.Model = string.Empty;
            this.Captured = string.Empty;
            this.Aperture = string.Empty;
            this.IsoSpeed = string.Empty;
            this.Exposure = string.Empty;
            this.ExposureBias = string.Empty;
            this.FocalLength = string.Empty;
            this.WithFlash = string.Empty;
        }

        if (metadata.HasLocationMetadata)
        {
            this.Location = "Location"; 
            this.Latitude = "Lat.: " + metadata.LatitudeString;
            this.Longitude = "Long.: " + metadata.LongitudeString;
        } 
        else
        {
            this.Location = string.Empty;
            this.Latitude = string.Empty;
            this.Longitude = string.Empty;
        }
    }
}
