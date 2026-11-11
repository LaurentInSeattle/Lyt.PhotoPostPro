namespace Lyt.PhotoPostPro.Workflow.Tools.Libraries;

using static Lyt.PhotoPostPro.Workflow.Tools.ToolsStatics;

public sealed partial class LibrariesEditViewModel :
    ViewModel<LibrariesEditView>, IEditor
{
    private readonly PhotoPostProModel model;
    private readonly EditorViewModel editorViewModel;

    [ObservableProperty]
    public partial string FriendlyName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool FriendlyNameIsEnabled { get; set; }

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FolderPath { get; set; } = string.Empty;


    [ObservableProperty]
    public partial string ValidationMessage { get; set; } = string.Empty;

    public LibrariesEditViewModel(PhotoPostProModel model, EditorViewModel editorViewModel)
    {
        this.model = model;
        this.editorViewModel = editorViewModel;

        this.SetDefaults();
        this.ValidationMessage = string.Empty;
    }

    private void SetDefaults()
    {
        this.FriendlyName = "New Export Specification";
        this.Description = "Enter a short description here...";
    }

    partial void OnFriendlyNameChanged(string value) => this.ValidateAndMessage();

    partial void OnDescriptionChanged(string value) => this.ValidateAndMessage();

    private bool ValidateAndMessage()
    {
        if (!this.Validate(out string message))
        {
            this.ValidationMessage = this.Localize(message);
            this.editorViewModel.EnableButtons(enabled: false);
            return false;
        }

        this.ValidationMessage = string.Empty;
        this.editorViewModel.EnableButtons(enabled: true);
        return true;
    }

    private bool Validate(out string message)
    {
        message = string.Empty;
        string friendlyName = this.FriendlyName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(friendlyName) || friendlyName.Length < 1)
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

        return true;
    }

    // Populate the form with defaults 
    public void BeginAdd()
    {
        this.FriendlyNameIsEnabled = true;
        this.SetDefaults();
    }

    // Populate the form with provided editable 
    public void BeginEdit(IEditable editable)
    {
        if (editable is not ImageExport imageExport)
        {
            return;
        }

        this.FriendlyNameIsEnabled = false;

        this.FriendlyName = imageExport.FriendlyName;
        this.Description = imageExport.Description;
        this.ValidateAndMessage();
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
        if (!this.ValidateAndMessage())
        {
            return false;
        }

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
        if (!this.model.DeleteImageExport(this.FriendlyName.Trim(), out string message))
        {
            this.ValidationMessage = this.Localize(message);
            return false;
        }

        return true;
    }

    private ImageExport CollectData()
    {
        return new ImageExport()
        {
            FriendlyName = this.FriendlyName.Trim(),
            Description = this.Description.Trim(),
        };
    }
}
