namespace Lyt.PhotoPostPro.Panes.MetadataPane;

public sealed partial class MetadataViewModel : 
    ViewModel<MetadataView>,
    IRecipient<LanguageChangedMessage>,
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
    public partial string MakeModel { get; private set; } = string.Empty;

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

    private Metadata? metadata;

    public MetadataViewModel()
    {
        this.Subscribe<MetadataGeneratedMessage>();
        this.Subscribe<LanguageChangedMessage>();
    }

    public MetadataViewModel(Metadata metadata) : this()
    {
        this.metadata = metadata;
        this.Update(metadata);
    }

    public void Receive(MetadataGeneratedMessage message)
    {
        Dispatch.OnUiThread(() =>
        {
            this.metadata = message.Metadata;
            this.Update(this.metadata); 
        }, DispatcherPriority.ApplicationIdle);
    }

    public void Receive(LanguageChangedMessage message)
    {
        if ( this.metadata is null)
        {
            return; 
        }

        Dispatch.OnUiThread(() =>
        {
            this.Update(this.metadata);
        }, DispatcherPriority.ApplicationIdle);
    }

    public void Update(Metadata metadata)
    {
        this.Filename = string.Format("{0} : {1}", metadata.Extension, metadata.Filename);
        this.SizeMB = metadata.SizeMB;
        this.Dimensions = metadata.Dimensions;
        string sep = new(System.IO.Path.DirectorySeparatorChar, 1);
        string sepSpace = " " + sep + " "; 
        this.FullPath = metadata.FullPath.Replace(sep, sepSpace);
        var localDT = metadata.FileDateUTC.ToLocalTime();

        string fileCreatedFmt = this.Localize("Metadata.FileCreatedFmt"); 
        this.FileDateTime =
            string.Format(fileCreatedFmt, localDT.ToLongDateString(), localDT.ToLongTimeString()); 

        if (metadata.HasExifMetadata)
        {
            this.MakeModel = metadata.Make + " " + metadata.Model;

            string fileCapturedFmt = this.Localize("Metadata.FileCapturedFmt");
            this.Captured =
                string.Format(fileCapturedFmt, metadata.Captured.ToLongDateString(), metadata.Captured.ToLongTimeString());
            string aperture = this.Localize("Metadata.Aperture");
            this.Aperture = aperture + " " + metadata.Aperture;
            string isoSpeed = this.Localize("Metadata.ISOSpeed");
            this.IsoSpeed = isoSpeed + " " + metadata.IsoSpeed;
            string exposure = this.Localize("Metadata.Exposure");
            this.Exposure = exposure + " " + metadata.Exposure;
            string exposureBias = this.Localize("Metadata.ExposureBias");
            this.ExposureBias = exposureBias + " " + metadata.ExposureBias;
            string focalLength = this.Localize("Metadata.FocalLength");
            this.FocalLength = focalLength + " " + metadata.FocalLength;
            this.WithFlash = 
                metadata.WithFlash ?
                    this.Localize("Metadata.WithFlash") :
                    this.Localize("Metadata.NoFlash"); 
        }
        else
        {
            this.MakeModel = string.Empty;
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
            this.Location = this.Localize("Metadata.Location");
            string latitude = this.Localize("Metadata.Latitude");
            this.Latitude = latitude + " " + metadata.LatitudeString;
            string longitude = this.Localize("Metadata.Longitude");
            this.Longitude = longitude + " " + metadata.LongitudeString;
        } 
        else
        {
            this.Location = string.Empty;
            this.Latitude = string.Empty;
            this.Longitude = string.Empty;
        }
    }
}
