namespace Lyt.PhotoPostPro.Workflow.Tools.PhotoBooks;

using static Lyt.Avalonia.Mvvm.Dialogs.ModalMessage;
using static Lyt.PhotoPostPro.Workflow.Tools.ToolsStatics;

public sealed partial class PhotoBookEditViewModel :
    ViewModel<PhotoBookEditView>, IEditor
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
    public partial ObservableCollection<string> SupportedPages { get; set; } = [];

    [ObservableProperty]
    public partial int SelectedPageIndex { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<string> SupportedOrientations { get; set; } = [];

    [ObservableProperty]
    public partial int SelectedOrientationIndex { get; set; }

    [ObservableProperty]
    public partial string ValidationMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEditMode { get; set; } = false;

    public PhotoBookEditViewModel(PhotoPostProModel model, EditorViewModel editorViewModel)
    {
        this.model = model;
        this.editorViewModel = editorViewModel;

        this.SetDefaults();
        this.PopulateLocalizedComboBoxes(); 
        this.ValidationMessage = string.Empty;
    }

    private void SetDefaults()
    {
        this.FriendlyName = "New Photo Book";
        this.Description = "Enter a short description here...";
    }

    private void PopulateLocalizedComboBoxes()
    {
        // We may need to localize 
        var list = new List<string>();
        foreach (string item in SupportedPagesText)
        {
            list.Add(this.Localize(item));
        }

        this.SupportedPages = new(list);

        // Enforce property changed
        this.SelectedPageIndex = 1;
        this.SelectedPageIndex = 0;

        list = new List<string>();
        foreach (string item in SupportedOrientationsText)
        {
            list.Add(this.Localize(item));
        }

        this.SupportedOrientations = new(list);

        // Enforce property changed
        this.SelectedOrientationIndex = 1;
        this.SelectedOrientationIndex = 0;
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

        if (this.SelectedPageIndex >= 0 && this.SelectedPageIndex < SupportedPagesValues.Count)
        {
            var page = SupportedPagesValues[this.SelectedPageIndex];
        }
        else
        {
            return false; 
        }
        
        return true;
    }

    // Populate the form with defaults 
    public void BeginAdd()
    {
        this.IsEditMode = false;
        this.FriendlyNameIsEnabled = true;
        this.SetDefaults();
    }

    // Populate the form with provided editable 
    public void BeginEdit(IEditable editable)
    {
        if (editable is not Model.PhotoBook.Book book)
        {
            return;
        }

        this.IsEditMode = true;
        this.FriendlyNameIsEnabled = false;
        this.FriendlyName = book.FriendlyName;
        this.Description = book.Description;
        this.ValidateAndMessage();
    }

    // Clicked "Add" button - add new editable to model
    public bool Add()
    {
        Model.PhotoBook.Book? existing = this.model.BookManager.GetBook(this.FriendlyName.Trim());
        if (existing is not null)
        {
            this.ValidationMessage = this.Localize("Tools.Editor.Validation.FriendlyNameAlreadyExists");
            return false;
        }

        string friendlyName = this.FriendlyName.Trim();
        if (friendlyName.Length < 2)
        {
            this.ValidationMessage = this.Localize("Tools.Editor.Validation.FriendlyNameTooShort");
            return false;
        }

        string description = this.Description.Trim();
        BookPage page = SupportedPagesValues[this.SelectedPageIndex];
        BookFormat bookFormat = SupportedOrientationsValues[this.SelectedOrientationIndex]; 

        if (this.model.BookManager.CreateBook(
            friendlyName, description, 
            page, bookFormat,
            PageBackground.Black,
            withFrontCover: true,
            withBackCover: false,
            out string message) is null)
        {
            this.ValidationMessage = this.Localize(message);
            return false;
        }


        return true;
    }

    // Clicked "Save" button - Save edits to model 
    public bool Save() => false; 

    // Clicked "Delete" button - Remove from model, 
    public bool Delete()
    {
        if (!this.model.BookManager.DeleteBook(this.FriendlyName.Trim(), out string message))
        {
            this.ValidationMessage = this.Localize(message);
            return false;
        }

        return true;
    }
}
