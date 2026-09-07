namespace Lyt.PhotoPostPro.Workflow.Tools.Exporting;

public sealed partial class ExportEditViewModel : 
    ViewModel<ExportEditView>, IEditor
{
    private readonly PhotoPostProModel model;

    /* 

    public string FriendlyName { get; set; } = string.Empty;

    public ExportAction Action { get; set; } = ExportAction.None;

    public int Dimension { get; set; } = 1920;

    // Target size in megabytes when action is set to ExportAction.ToFileSize
    public float MegaBytes { get; set; } = 1.0f;

    public OutputFormat OutputFormat { get; set; } = OutputFormat.Jpeg;

    public int JpegQuality { get; set; } = 95;

    public bool IsGalleryFormat { get; set; } = false;

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
    public partial bool FriendlyNameIsDisabled { get; set; }

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    public ExportEditViewModel(PhotoPostProModel model)
    {
        this.model = model;
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
