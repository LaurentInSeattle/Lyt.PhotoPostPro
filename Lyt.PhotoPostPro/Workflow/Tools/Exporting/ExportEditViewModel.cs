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
    public partial ObservableCollection<string> SupportedBorderStyles { get; set; } = [];

    [ObservableProperty]
    public partial int SelectedBorderStyleIndex { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<string> SupportedBorderThicknesses { get; set; } = [];

    [ObservableProperty]
    public partial int SelectedBorderThicknessIndex { get; set; }

    [ObservableProperty]
    public partial bool IsBorderThicknessEnabled { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableSignatures { get; set; } = [];

    [ObservableProperty]
    public partial int SelectedSignatureIndex { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableWatermarks { get; set; } = [];

    [ObservableProperty]
    public partial int SelectedWatermarkIndex { get; set; }

    [ObservableProperty]
    public partial string PostFix { get; set; } = string.Empty;

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
        this.SelectedOutputFormatIndex = 0;
        this.IsCompressionEnabled = false;
        this.SelectedBorderStyleIndex = 0;
        this.SelectedBorderThicknessIndex = 0;
        this.IsBorderThicknessEnabled = false;
        this.PostFix = "_XXX";
    }

    private void PopulateLocalizedComboBoxes()
    {
        // We may need to localize (again)
        var list = new List<string>();
        foreach (string item in SupportedOutputFormatText)
        {
            list.Add(this.Localize(item));
        }

        this.SupportedOutputFormats = new(list);

        // Enforce property changed
        this.SelectedOutputFormatIndex = 1;
        this.SelectedOutputFormatIndex = 0;
        this.IsCompressionEnabled = false;

        list = [];
        foreach (string item in SupportedImageBorderStyleText)
        {
            list.Add(this.Localize(item));
        }

        this.SupportedBorderStyles = new(list);

        // Enforce property changed
        this.SelectedBorderStyleIndex = 1;
        this.SelectedBorderStyleIndex = 0;
        this.IsBorderThicknessEnabled = false;

        list = [];
        foreach (string item in SupportedImageBorderThicknessText)
        {
            list.Add(this.Localize(item));
        }

        this.SupportedBorderThicknesses = new(list);

        // Enforce property changed
        this.SelectedBorderThicknessIndex = 1;
        this.SelectedBorderThicknessIndex = 0;

        list = [];
        list.Add(this.Localize("Tools.Editor.Signature.None"));
        IEnumerable<string> signatures = this.model.Signatures.AvailableSignatures.Select(s => s.FriendlyName);
        foreach (string signature in signatures)
        {
            list.Add(signature);
        }

        this.AvailableSignatures = new(list);

        // Enforce property changed
        this.SelectedSignatureIndex = 1;
        this.SelectedSignatureIndex = 0;

        list = [];
        list.Add(this.Localize("Tools.Editor.Watermark.None"));
        IEnumerable<string> watermarks = this.model.Watermarks.AvailableWatermarks.Select(w => w.FriendlyName);
        foreach (string watermark in watermarks)
        {
            list.Add(watermark);
        }

        this.AvailableWatermarks = new(list);

        // Enforce property changed
        this.SelectedWatermarkIndex = 1;
        this.SelectedWatermarkIndex = 0;
    }

    partial void OnFriendlyNameChanged(string value) => this.ValidateAndMessage();

    partial void OnDescriptionChanged(string value) => this.ValidateAndMessage();

    partial void OnResizeDimensionStringChanged(string value) => this.ValidateAndMessage();

    partial void OnSelectedOutputFormatIndexChanged(int value) => this.ValidateAndMessage();

    partial void OnQualityStringChanged(string value) => this.ValidateAndMessage();

    partial void OnSelectedBorderStyleIndexChanged(int value) => this.ValidateAndMessage();

    partial void OnSelectedBorderThicknessIndexChanged(int value) => this.ValidateAndMessage();

    partial void OnSelectedSignatureIndexChanged(int value) => this.ValidateAndMessage();

    partial void OnSelectedWatermarkIndexChanged(int value) => this.ValidateAndMessage();

    partial void OnPostFixChanged(string value) => this.ValidateAndMessage();

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

        string description = this.Description?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(description) || description.Length < 3)
        {
            message = "Tools.Editor.Validation.DescriptionNameRequired";
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

        if (this.SelectedBorderStyleIndex >= 0 && this.SelectedBorderStyleIndex < SupportedImageBorderStyleValues.Count)
        {
            ImageBorderStyle borderStyle = SupportedImageBorderStyleValues[this.SelectedBorderStyleIndex];
            this.IsBorderThicknessEnabled = borderStyle != ImageBorderStyle.None;
        }

        if (!this.isGalleryFormat)
        {
            // All exports must have a post-fix to identify the export type.
            // This is used to avoid overwriting the original image and to identify the export type in the filename.
            // Exception : Gallery format exports do not require a post-fix.
            string postFix = this.PostFix?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(postFix) || postFix.Length == 0)
            {
                message = "Tools.Editor.Validation.PostFixRequired";
                return false;
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

        this.SelectedBorderStyleIndex = 0;
        for (int i = 0; i < SupportedImageBorderStyleValues.Count; ++i)
        {
            if (imageExport.BorderStyle == SupportedImageBorderStyleValues[i])
            {
                this.SelectedBorderStyleIndex = i;
                break;
            }
        }

        this.SelectedBorderThicknessIndex = 0;
        for (int i = 0; i < SupportedImageBorderThicknessValues.Count; ++i)
        {
            if (imageExport.BorderThickness == SupportedImageBorderThicknessValues[i])
            {
                this.SelectedBorderThicknessIndex = i;
                break;
            }
        }

        this.IsBorderThicknessEnabled = imageExport.BorderStyle != ImageBorderStyle.None;

        this.SelectedSignatureIndex = 0;
        var signatures = this.model.Signatures.AvailableSignatures;
        for (int i = 0; i < signatures.Count; ++i)
        {
            if (imageExport.SignatureName == signatures[i].FriendlyName)
            {
                // Plus one because zero is no signature 
                this.SelectedSignatureIndex = i + 1;
                break;
            }
        }

        this.SelectedWatermarkIndex = 0;
        var watermarks = this.model.Watermarks.AvailableWatermarks;
        for (int i = 0; i < watermarks.Count; ++i)
        {
            if (imageExport.WatermarkName == watermarks[i].FriendlyName)
            {
                // Plus one because zero is no watermark 
                this.SelectedWatermarkIndex = i + 1;
                break;
            }
        }

        this.PostFix = imageExport.PostFix;
    }

    // Clicked "Add" button - add new editable to model
    public bool Add()
    {
        ImageExport? existing = this.model.ImageExports.FromFriendlyName(this.FriendlyName.Trim());
        if (existing is not null)
        {
            this.ValidationMessage = this.Localize("Tools.Editor.Validation.FriendlyNameAlreadyExists");
            return false;
        }

        var newImageExport = this.CollectData();
        if (!this.model.AddImageExport(newImageExport, out string message))
        {
            this.ValidationMessage = this.Localize(message);
            return false;
        }

        return true;
    }

    // Clicked "Save" button - Save edits to model 
    public bool Save()
    {
        var editedImageExport = this.CollectData();
        if (!this.model.EditImageExport(editedImageExport, out string message))
        {
            this.ValidationMessage = this.Localize(message);
            return false;
        }

        return true;
    }

    // Clicked "Delete" button - Remove from model, 
    public bool Delete()
    {
        if (this.isGalleryFormat)
        {
            this.ValidationMessage = this.Localize("Tools.Editor.Validation.CannotDeleteGalleryFormat");
            return false;
        }

        if (!this.model.DeleteImageExport(this.FriendlyName.Trim(), out string message))
        {
            this.ValidationMessage = this.Localize(message);
            return false;
        }

        return true;
    }

    private ImageExport CollectData()
    {
        // Ensure that the post-fix starts with an underscore
        string postFix = this.PostFix?.Trim() ?? string.Empty;
        if (!postFix.StartsWith('_'))
        {
            postFix = '_' + postFix;
        }

        // Index 0 is "None" so we only want to use a signature if the index is greater than 0
        string signatureName = string.Empty;
        if (this.SelectedSignatureIndex > 0 && this.SelectedSignatureIndex < this.AvailableSignatures.Count)
        {
            signatureName = this.AvailableSignatures[this.SelectedSignatureIndex];
        }

        // Index 0 is "None" so we only want to use a watermark if the index is greater than 0
        string watermarkName = string.Empty;
        if (this.SelectedWatermarkIndex > 0 && this.SelectedWatermarkIndex < this.AvailableWatermarks.Count)
        {
            watermarkName = this.AvailableWatermarks[this.SelectedWatermarkIndex];
        }

        return new ImageExport()
        {
            FriendlyName = this.FriendlyName.Trim(),
            Description = this.Description.Trim(),
            Action = this.ShouldResize ? ExportAction.ToDimensions : ExportAction.None,
            Dimension = this.resizeDimension,
            OutputFormat = SupportedOutputFormatValues[this.SelectedOutputFormatIndex],
            Quality = this.quality,
            IsGalleryFormat = this.isGalleryFormat,
            BorderStyle = SupportedImageBorderStyleValues[this.SelectedBorderStyleIndex],
            BorderThickness = SupportedImageBorderThicknessValues[this.SelectedBorderThicknessIndex],

            PostFix = postFix,
            SignatureName = signatureName,
            WatermarkName = watermarkName,
        };
    }
}
