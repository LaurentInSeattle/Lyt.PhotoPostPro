namespace Lyt.PhotoPostPro.Workflow.Tools.Signing;

public sealed partial class SignatureEditViewModel :
    ViewModel<SignatureEditView>, IEditor
{
    private static readonly List<int> SupportedFontWeightValues =
    [
        100, 200, 300, 350 ,
        400, 500, 600, 700,
        800, 900, 
        // 950 // Apparently not supported 
    ];

    private static readonly List<string> SupportedFontWeightText =
    [
        "Thin - 100",
        "Extra Light - 200",
        "Light - 300",
        "Semi Light - 350",

        "Normal / Regular - 400",
        "Medium - 500",
        "Semi Bold - 600",
        "Bold - 700",

        "Extra Bold - 800",
        "Heavy - 900",
        // "Solid - 950", // 950 // Apparently not supported 
    ];

    private static readonly List<SignatureLocation> SupportedSignaturePlacementValues =
    [
        SignatureLocation.TopLeft,
        SignatureLocation.TopRight,
        SignatureLocation.BottomLeft,
        SignatureLocation.BottomRight,
    ];

    private static readonly List<string> SupportedSignaturePlacementText =
    [
        "Tools.Editor.TopLeft",
        "Tools.Editor.TopRight",
        "Tools.Editor.BottomLeft",
        "Tools.Editor.BottomRight",
    ];

    private static readonly List<PppFontStyle> SupportedFontStyleValues =
    [
        PppFontStyle.Regular,
        PppFontStyle.Bold ,
        PppFontStyle.Italic,
        PppFontStyle.BoldItalic,
    ];

    private static readonly List<string> SupportedFontStyleText =
    [
        "Tools.Editor.Regular",
        "Tools.Editor.Bold",
        "Tools.Editor.Italic",
        "Tools.Editor.BoldItalic",
    ];

    private readonly PhotoPostProModel model;
    private int fontSize = 26;
    private int fontWeight = 400;

    /* 
    public string FriendlyName { get; set; } = string.Empty;

    public string Text { get; set; } = "Edited with Photo Rebel";

    public int FontSize { get; set; } = 26;

    public string FontFamily { get; set; } = "Segoe Script";

    public PppFontStyle PppFontStyle { get; set; } = PppFontStyle.Italic;

    public SignatureLocation Location { get; set; } = SignatureLocation.BottomRight;

    public uint HexColorArgb { get; set; } = 0xFFFFFFFF;
    */

    [ObservableProperty]
    public partial string FriendlyName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FontSizeString { get; set; }

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

    public SignatureEditViewModel(PhotoPostProModel model)
    {
        this.model = model;

        this.SetDefaults();
        var fontCollection = FontManager.Current.SystemFonts;
        var fontFamilies = new List<FontFamily>(fontCollection).OrderBy(x => x.Name).ToList();

        // UGLY HACK !
        // Crash when opening the combo if the InterV font is present in the list
        // Note: Inter is doing fine...
        var toRemove =
            (from family in fontFamilies
             where family.Name.StartsWith("InterV", StringComparison.InvariantCultureIgnoreCase)
             // where family.Name.StartsWith("Inter", StringComparison.InvariantCultureIgnoreCase) 
             select family).ToList();
        if (toRemove.Count > 0)
        {
            foreach (var family in toRemove)
            {
                fontFamilies.Remove(family);
            }
        }

        this.SupportedFontFamilies = fontFamilies;
        this.SupportedFontWeights = SignatureEditViewModel.SupportedFontWeightText;

        // Enforce property changed
        this.SelectedTextFontWeightsIndex = 0;
        this.SelectedTextFontWeightsIndex = 4;

        this.ValidationMessage = string.Empty;
    }

    private void SetDefaults()
    {
        this.fontSize = 26;
        this.fontWeight = 400;
        this.ForegroundColor = Color.FromUInt32(0xFF_FF_FA_F0);
        this.FriendlyName = Signature.DefaultName;
        this.Text = "Edited with Photo Rebel";
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
            return; 
        }

        this.ValidationMessage = string.Empty; 
    }

    private bool Validate(out string message)
    {
        message = string.Empty;
        return true; 
    }

    // Clicked "Add" button - add new editable to model, refresh master list,
    // and then select new item in master list
    public void Add()
    {

    }

    // Clicked "Save" button - Save edits to model 
    public void Save()
    {

    }

    // Clicked "Delete" button - Remove from model, refresh master list,
    // and then select new item in master list
    public void Delete()
    {

    }

}
