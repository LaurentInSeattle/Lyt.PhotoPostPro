namespace Lyt.PhotoPostPro.Panes.MetadataPane;

public sealed partial class MetadataViewModel :
    ViewModel<MetadataView>,
    IRecipient<LanguageChangedMessage>,
    IRecipient<MetadataGeneratedMessage>,
    IRecipient<LibraryMetadataUpdateMessage>
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
    public partial bool HasExif { get; private set; }

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
    public partial bool HasLocation { get; private set; }

    [ObservableProperty]
    public partial string Location { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Latitude { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Longitude { get; private set; } = string.Empty;

    private Metadata? metadata;
    private double metadataLatitude;
    private double metadataLongitude;

    public MetadataViewModel()
    {
        // Needed to hide the web navigate button when there is no metadata yet
        // and enforce property changed 
        this.HasLocation = true;
        this.HasLocation = false;

        this.Subscribe<MetadataGeneratedMessage>();
        this.Subscribe<LibraryMetadataUpdateMessage>();
        this.Subscribe<LanguageChangedMessage>();
    }

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();

        this.View.IsVisible = false;
        this.HasLocation = false;
        this.View.WebNavigateButton.Margin = new Thickness(0, 0, -1000, 0);
        this.View.WebNavigateButton.IsShown = false;
        this.View.WebNavigateButton.IsVisible = false;
    }

    public MetadataViewModel(Metadata metadata) : this()
    {
        this.metadata = metadata;
        this.Update(metadata);
    }

    public void Receive(LibraryMetadataUpdateMessage message)
    {
        this.metadata = message.Metadata;
        this.DispatchUpdate();
    }

    public void Receive(MetadataGeneratedMessage message)
    {
        this.metadata = message.Metadata;
        this.DispatchUpdate();
    }

    public void Receive(LanguageChangedMessage message) => this.DispatchUpdate(); 

    private void DispatchUpdate()
        => Dispatch.OnUiThread(() =>
        {
            if (this.metadata is null)
            {
                return;
            }

            this.Update(this.metadata);
        }, DispatcherPriority.ApplicationIdle);

    public void Update(Metadata metadata)
    {
        this.metadataLatitude = Metadata.InvalidLatLong;
        this.metadataLongitude = Metadata.InvalidLatLong;
        this.HasLocation = false;
        if (this.IsBound)
        {
            this.View.IsVisible = true;
        }

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

        this.HasExif = metadata.HasExifMetadata;
        if (this.HasExif)
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

        this.HasLocation = metadata.HasLocationMetadata;
        if (this.HasLocation)
        {
            this.Location = this.Localize("Metadata.Location");
            string latitude = this.Localize("Metadata.Latitude");
            this.Latitude = latitude + " " + metadata.LatitudeString;
            string longitude = this.Localize("Metadata.Longitude");
            this.Longitude = longitude + " " + metadata.LongitudeString;
            this.metadataLatitude = metadata.Latitude;
            this.metadataLongitude = metadata.Longitude;
            if (this.IsBound)
            {
                this.View.WebNavigateButton.Margin = new Thickness(0, 0, 16, 0);
                this.View.WebNavigateButton.IsShown = true;
            }
        }
        else
        {
            this.Location = string.Empty;
            this.Latitude = string.Empty;
            this.Longitude = string.Empty;
            if (this.IsBound)
            {
                this.View.WebNavigateButton.Margin = new Thickness(0, 0, -1000, 0);
                this.View.WebNavigateButton.IsShown = false;
            }
        }
    }

    [RelayCommand]
    public void OnWebNavigate()
    {
        if ((this.metadata is null) ||
            !this.HasLocation ||
            double.IsNaN(this.metadataLatitude) ||
            double.IsNaN(this.metadataLongitude))
        {
            return;
        }

        WebUtilities.OpenLocationUrl(
            WebUtilities.GeoProtocol.GoogleMapsLink, this.metadataLatitude, this.metadataLongitude);
    }
}
