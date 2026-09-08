namespace Lyt.PhotoPostPro.Workflow.Tools.Exporting;

using static Lyt.PhotoPostPro.Workflow.Tools.ToolsStatics;

public sealed partial class ExportEditViewModel :
    ViewModel<ExportEditView>, IEditor
{
    private readonly PhotoPostProModel model;
    private readonly EditorViewModel editorViewModel;

    private int resizeDimension = 1920;
    private int quality = 85;
    private bool isGalleryFormat;

    /* 

    public bool WithSignature { get; set; } = false;

    public string SignatureName { get; set; } = string.Empty;

    public bool WithWatermark { get; set; } = false;

    public string WatermarkName { get; set; } = string.Empty;

    public bool WithBorders { get; set; } = false;

    public ImageBorderStyle BorderStyle { get; set; } = ImageBorderStyle.None;

    public ImageBorderThickness BorderThickness { get; set; } = ImageBorderThickness.Thick;

    // String added to filename to identify the export type
    public string PostFix { get; set; } = string.Empty;

    */

    [ObservableProperty]
    public partial string FriendlyName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool FriendlyNameIsEnabled { get; set; }

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ShouldResize { get; set; }

    [ObservableProperty]
    public partial string ResizeDimensionString { get; set; } = "1920";

    [ObservableProperty]
    public partial bool IsResizeDimensionEnabled { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<string> SupportedOutputFormats { get; set; } = [];

    [ObservableProperty]
    public partial int SelectedOutputFormatIndex { get; set; }

    [ObservableProperty]
    public partial bool IsCompressionEnabled { get; set; }

    [ObservableProperty]
    public partial string QualityString { get; set; } = "85";

    [ObservableProperty]
    public partial string ValidationMessage { get; set; } = string.Empty;

    public ExportEditViewModel(PhotoPostProModel model, EditorViewModel editorViewModel)
    {
        this.model = model;
        this.editorViewModel = editorViewModel;

        this.SetDefaults();
        this.ValidationMessage = string.Empty;
    }

    private void SetDefaults()
    {
        this.FriendlyName = "New Export Specification";
        this.ShouldResize = true;
        this.resizeDimension = 1920;
        this.ResizeDimensionString = this.resizeDimension.ToString("D");
        this.quality = 85;
        this.QualityString = this.quality.ToString("D");
    }

    private void PopulateLocalizedComboBoxes()
    {
        // We may need to localize (again) the supported placements text, so let's do 
        var list = new List<string>();
        foreach (string item in SupportedOutputFormatText)
        {
            list.Add(this.Localize(item));
        }

        this.SupportedOutputFormats = new(list);

        // Enforce property changed
        this.SelectedOutputFormatIndex = 1;
        this.SelectedOutputFormatIndex = 0;
    }

    partial void OnFriendlyNameChanged(string value) => this.ValidateAndMessage();

    partial void OnDescriptionChanged(string value) => this.ValidateAndMessage();

    partial void OnResizeDimensionStringChanged(string value) => this.ValidateAndMessage();

    partial void OnSelectedOutputFormatIndexChanged(int value) => this.ValidateAndMessage();

    partial void OnQualityStringChanged(string value) => this.ValidateAndMessage();


    partial void OnShouldResizeChanged(bool value)
    {
        this.IsResizeDimensionEnabled = value;
        this.ValidateAndMessage();
    }

    private void ValidateAndMessage()
    {
        if (!this.Validate(out string message))
        {
            this.ValidationMessage = this.Localize(message);
            this.editorViewModel.EnableButtons(enabled: false);
            return;
        }

        this.ValidationMessage = string.Empty;
        this.editorViewModel.EnableButtons(enabled: true);
    }

    private bool Validate(out string message)
    {
        message = string.Empty;
        string friendlyName = this.FriendlyName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(friendlyName) || friendlyName.Length < 3)
        {
            message = "Tools.Editor.Validation.FriendlyNameRequired";
            return false;
        }

        if (this.ShouldResize)
        {
            string resizeDimensionString = this.ResizeDimensionString?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(resizeDimensionString) || !int.TryParse(resizeDimensionString, out int maybeResizeDimension))
            {
                message = "Tools.Editor.Validation.ResizeDimensionRequired";
                return false;
            }

            if (maybeResizeDimension < 360 || maybeResizeDimension > 12 * 1024)
            {
                message = "Tools.Editor.Validation.ResizeDimensionOutOfRange";
                return false;
            }

            this.resizeDimension = maybeResizeDimension;
        }

        if (this.SelectedOutputFormatIndex >= 0 && this.SelectedOutputFormatIndex < SupportedOutputFormatValues.Count)
        {
            var outputFormat = SupportedOutputFormatValues[this.SelectedOutputFormatIndex];
            this.IsCompressionEnabled = (outputFormat == OutputFormat.Jpeg) || (outputFormat == OutputFormat.WebP);
            if (this.IsCompressionEnabled)
            {
                string qualityString = this.QualityString?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(qualityString) || !int.TryParse(qualityString, out int maybeQuality))
                {
                    message = "Tools.Editor.Validation.QualityRequired";
                    return false;
                }

                if (maybeQuality < 20 || maybeQuality > 100)
                {
                    message = "Tools.Editor.Validation.QualityOutOfRange";
                    return false;
                }

                this.quality = maybeQuality;
            }
        }

        return true;
    }

    // Populate the form with defaults 
    public void BeginAdd()
    {
        this.FriendlyNameIsEnabled = true;
        this.PopulateLocalizedComboBoxes();
        this.SetDefaults();
    }

    // Populate the form with provided editable 
    public void BeginEdit(IEditable editable)
    {
        if (editable is not ImageExport imageExport)
        {
            return;
        }

        this.isGalleryFormat = imageExport.IsGalleryFormat;
        this.FriendlyNameIsEnabled = false;
        this.PopulateLocalizedComboBoxes();

        this.FriendlyName = imageExport.FriendlyName;
        this.Description = imageExport.Description;
        this.ShouldResize = imageExport.Action == ExportAction.ToDimensions;
        this.ResizeDimensionString = imageExport.Dimension.ToString("D");
        this.IsResizeDimensionEnabled = this.ShouldResize;

        this.SelectedOutputFormatIndex = 0;
        for (int i = 0; i < SupportedOutputFormatValues.Count; ++i)
        {
            if (imageExport.OutputFormat == SupportedOutputFormatValues[i])
            {
                this.SelectedOutputFormatIndex = i;
                break;
            }
        }

        this.QualityString = imageExport.Quality.ToString("D");
    }

    // Clicked "Add" button - add new editable to model, refresh master list,
    // and then select new item in master list
    public bool Add()
    {
        return true;
    }

    // Clicked "Save" button - Save edits to model 
    public bool Save()
    {
        return true;
    }

    // Clicked "Delete" button - Remove from model, refresh master list,
    // and then select new item in master list
    public bool Delete()
    {
        if (this.isGalleryFormat)
        {
            this.ValidationMessage = this.Localize("Tools.Editor.Validation.CannotDeleteGalleryFormat");
            return false;
        }

        return true;
    }

    private ImageExport CollectData()
    => new()
    {
        FriendlyName = this.FriendlyName.Trim(),
        Description = this.Description.Trim(),
        Action = this.ShouldResize ? ExportAction.ToDimensions : ExportAction.None,
        OutputFormat = SupportedOutputFormatValues[this.SelectedOutputFormatIndex],
        Quality = this.quality,
        IsGalleryFormat = this.isGalleryFormat,
        PostFix = string.Empty,
        Dimension = this.resizeDimension,
        WithSignature = false,
        SignatureName = string.Empty,
        WithWatermark = false,
        WatermarkName = string.Empty,
        WithBorders = false,
        BorderStyle = ImageBorderStyle.None,
        BorderThickness = ImageBorderThickness.Thick,
    };

}
