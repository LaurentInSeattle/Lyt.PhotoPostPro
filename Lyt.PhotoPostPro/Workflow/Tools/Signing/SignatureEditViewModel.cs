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
    public partial string FriendlyName { get; set; }

    [ObservableProperty]
    public partial string Text { get; set; }

    [ObservableProperty]
    public partial string FontSizeString { get; set; }

    [ObservableProperty]
    public partial PppFontStyle PppFontStyle { get; set; }

    [ObservableProperty]
    public partial List<FontFamily> SupportedFontFamilies { get; set; }

    [ObservableProperty]
    public partial int SelectedFontFamilyIndex { get; set; }

    [ObservableProperty]
    public partial Color ForegroundColor { get; set; }

    [ObservableProperty]
    public partial List<string> SupportedFontWeights { get; set; }

    [ObservableProperty]
    public partial int SelectedTextFontWeightsIndex { get; set; }

    [ObservableProperty]
    public partial string ValidationMessage { get; set; }

    public SignatureEditViewModel(PhotoPostProModel model)
    {
        this.model = model;

        this.fontSize = 26;
        this.fontWeight = 400;
        this.ForegroundColor = Color.FromUInt32(0xFF_00_00_00);
        this.FriendlyName = Signature.DefaultName;
        this.Text = Signature.DefaultName;
        this.FontSizeString = this.fontSize.ToString("D");
        this.SupportedFontWeights = SignatureEditViewModel.SupportedFontWeightText;

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

        // Enforce property changed
        this.SelectedTextFontWeightsIndex = 0;
        this.SelectedTextFontWeightsIndex = 6;
    }

    public override void OnViewLoaded() 
    {
        base.OnViewLoaded(); 
    } 

    public override void Activate(object? activationParameters) 
    {
        base.Activate(activationParameters);
    }

    // Populate the form with defaults 
    public void BeginAdd() 
    { 
    }

    // Populate the form with provided editable 
    public void BeginEdit(IEditable editable)
    { 
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
