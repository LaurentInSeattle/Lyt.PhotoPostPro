namespace Lyt.PhotoPostPro.Workflow.Tools.Signing;

using static Lyt.PhotoPostPro.Workflow.Tools.ToolsStatics;

public sealed partial class SignatureEditViewModel : ViewModel<SignatureEditView>, IEditor
{
    private readonly PhotoPostProModel model;
    private readonly EditorViewModel editorViewModel;

    private int fontSize = 26;

    [ObservableProperty]
    public partial string FriendlyName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool FriendlyNameIsEnabled { get; set; }

    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FontSizeString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial PppFontStyle PppFontStyle { get; set; }

    [ObservableProperty]
    public partial SignatureLocation Location { get; set; }

    [ObservableProperty]
    public partial List<FontFamily> SupportedFontFamilies { get; set; }

    [ObservableProperty]
    public partial int SelectedFontFamilyIndex { get; set; }

    [ObservableProperty]
    public partial Color ForegroundColor { get; set; } = Color.FromUInt32(0xFF_FF_FF_FF); // Pure White 

    [ObservableProperty]
    public partial List<string> SupportedFontWeights { get; set; }

    [ObservableProperty]
    public partial int SelectedTextFontWeightsIndex { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<string> SupportedPlacements { get; set; } = [];

    [ObservableProperty]
    public partial int SelectedPlacementIndex { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<string> SupportedFontStyles { get; set; } = [];

    [ObservableProperty]
    public partial int SelectedFontStylesIndex { get; set; }

    [ObservableProperty]
    public partial string ValidationMessage { get; set; }

    public SignatureEditViewModel(PhotoPostProModel model, EditorViewModel editorViewModel)
    {
        this.model = model;
        this.editorViewModel = editorViewModel;

        this.SetDefaults();
        this.SupportedFontFamilies = FontFamilies();
        this.SupportedFontWeights = SupportedFontWeightText;

        // Enforce property changed
        this.SelectedTextFontWeightsIndex = 0;
        this.SelectedTextFontWeightsIndex = 4;

        this.ValidationMessage = string.Empty;
    }

    private void SetDefaults()
    {
        this.ForegroundColor = Color.FromUInt32(0xFF_FF_F8_F0);
        this.FriendlyName = Signature.DefaultName;
        this.Text = "Edited with Photo Rebel";
        this.fontSize = 26;
        this.FontSizeString = this.fontSize.ToString("D");
    }

    private void PopulateLocalizedComboBoxes()
    {
        // We may need to localize (again) the supported placements text, so let's do 
        var list = new List<string>();
        foreach (string item in SupportedSignaturePlacementText)
        {
            list.Add(this.Localize(item));
        }

        this.SupportedPlacements = new(list);

        // Enforce property changed
        this.SelectedPlacementIndex = 0;
        this.SelectedPlacementIndex = 3;

        // Same for the supported font styles
        list.Clear();
        foreach (string item in SupportedFontStyleText)
        {
            list.Add(this.Localize(item));
        }

        this.SupportedFontStyles = new(list);

        // Enforce property changed
        this.SelectedFontStylesIndex = 0;
        this.SelectedFontStylesIndex = 2;
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
        if (editable is not Signature signature)
        {
            return;
        }

        this.FriendlyNameIsEnabled = false;
        this.PopulateLocalizedComboBoxes();

        this.FriendlyName = signature.FriendlyName;
        this.Text = signature.Text;

        this.SelectedPlacementIndex = -1;
        for (int i = 0; i < SupportedSignaturePlacementValues.Count; ++i)
        {
            if (signature.Location == SupportedSignaturePlacementValues[i])
            {
                this.SelectedPlacementIndex = i;
                break;
            }
        }

        this.ForegroundColor = Color.FromUInt32(signature.HexColorArgb);
        this.FontSizeString = signature.FontSize.ToString("D");

        this.SelectedTextFontWeightsIndex = -1;
        for (int i = 0; i < SupportedFontWeights.Count; ++i)
        {
            if (signature.FontWeight == SupportedFontWeightValues[i])
            {
                this.SelectedTextFontWeightsIndex = i;
                break;
            }
        }

        this.SelectedFontFamilyIndex = -1;
        for (int i = 0; i < this.SupportedFontFamilies.Count; ++i)
        {
            if (signature.FontFamily.Equals(this.SupportedFontFamilies[i].Name, StringComparison.InvariantCultureIgnoreCase))
            {
                this.SelectedFontFamilyIndex = i;
                break;
            }
        }

        this.SelectedFontStylesIndex = -1;
        for (int i = 0; i < SupportedFontStyleValues.Count; ++i)
        {
            if (signature.PppFontStyle == SupportedFontStyleValues[i])
            {
                this.SelectedFontStylesIndex = i;
                break;
            }
        }
    }

    partial void OnFriendlyNameChanged(string value) => this.ValidateAndMessage();

    partial void OnTextChanged(string value) => this.ValidateAndMessage();

    partial void OnFontSizeStringChanged(string value) => this.ValidateAndMessage();

    partial void OnSelectedTextFontWeightsIndexChanged(int value) => this.ValidateAndMessage();

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

        string text = this.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text) || friendlyName.Length < 3)
        {
            message = "Tools.Editor.Validation.TextRequired";
            return false;
        }

        string fontSizeString = this.FontSizeString?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fontSizeString) || !int.TryParse(fontSizeString, out int maybeFontSize))
        {
            message = "Tools.Editor.Validation.FontSizeRequired";
            return false;
        }

        if (maybeFontSize < 6 || maybeFontSize > 200)
        {
            message = "Tools.Editor.Validation.FontSizeOutOfRange";
            return false;
        }

        this.fontSize = maybeFontSize;

        return true;
    }

    // Clicked "Add" button - add new editable to model
    // Returns true if successful, so that the editor can refresh the master list and select a new item 
    public bool Add()
    {
        Signature? existing = this.model.Signatures.FromFriendlyName(this.FriendlyName.Trim());
        if (existing is not null)
        {
            this.ValidationMessage = this.Localize("Tools.Editor.Validation.FriendlyNameAlreadyExists");
            return false;
        }

        var newSignature = this.CollectData();
        if (!this.model.AddSignature(newSignature, out string message))
        {
            this.ValidationMessage = this.Localize(message);
            return false;
        }

        return true;
    }

    // Clicked "Save" button - Save edits to model 
    // Returns true if successful, so that the editor can refresh the master list and select a new item 
    public bool Save()
    {
        var editedSignature = this.CollectData();
        if (!this.model.EditSignature(editedSignature, out string message))
        {
            this.ValidationMessage = this.Localize(message);
            return false;
        }

        return true;
    }

    // Clicked "Delete" button - Remove from model
    // Returns true if successful, so that the editor can refresh the master list and select a new item 
    public bool Delete()
    {
        if (!this.model.DeleteSignature(this.FriendlyName.Trim(), out string message))
        {
            this.ValidationMessage = this.Localize(message);
            return false;
        }

        return true;
    }

    private Signature CollectData()
        => new()
        {
            FriendlyName = this.FriendlyName.Trim(),
            Text = this.Text.Trim(),
            FontSize = this.fontSize,
            FontFamily = this.SupportedFontFamilies[this.SelectedFontFamilyIndex].Name,
            FontWeight = SupportedFontWeightValues[this.SelectedTextFontWeightsIndex],
            PppFontStyle = SupportedFontStyleValues[this.SelectedFontStylesIndex],
            Location = SupportedSignaturePlacementValues[this.SelectedPlacementIndex],
            HexColorArgb = this.ForegroundColor.ToUInt32()
        };
}
