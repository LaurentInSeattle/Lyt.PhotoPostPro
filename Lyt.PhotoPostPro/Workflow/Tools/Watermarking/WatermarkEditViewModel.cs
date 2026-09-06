namespace Lyt.PhotoPostPro.Workflow.Tools.Watermarking;

public sealed partial class WatermarkEditViewModel :
    ViewModel<WatermarkEditView>, IEditor
{
    private readonly PhotoPostProModel model;
    private readonly EditorViewModel editorViewModel;
    /* 
    public string FriendlyName { get; set; } = string.Empty;

    public string Text { get; set; } = "Edited with Photo Rebel";

    public int FontSize { get; set; } = 26;

    public string FontFamily { get; set; } = "Segoe Script";

    public PppFontStyle PppFontStyle { get; set; } = PppFontStyle.Italic;

    public SignatureLocation Location { get; set; } = SignatureLocation.BottomRight;

    public uint HexColorArgb { get; set; } = 0xFFFFFFFF;
    */

    public WatermarkEditViewModel(PhotoPostProModel model, EditorViewModel editorViewModel)
    {
        this.model = model;
        this.editorViewModel = editorViewModel;
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
        return true;
    }
}
