namespace Lyt.PhotoPostPro.Workflow.Tools.Editor;

public sealed partial class EditorViewModel : ViewModel<EditorView>
{
    public sealed class AddNewEditable(string friendlyName) : IEditable
    {
        public string FriendlyName { get; set; } = friendlyName;
    }

    private readonly PhotoPostProModel model;
    private readonly IEditorDataProvider editorDataProvider; 

    private IEditor? editor;
    private bool isFirstActivation;

    [ObservableProperty]
    // The collection of editable items in the master list - left side 
    public partial ObservableCollection<IEditable> EditableObjects { get; set; } = [];

    [ObservableProperty]
    // SelectedIndex in the master list 
    public partial int SelectedObjectIndex { get; set; }

    [ObservableProperty]
    // Selected object name 
    public partial string FriendlyName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial UserControl? EditingForm { get; set; }

    [ObservableProperty]
    public partial bool IsEditMode { get; set; }

    [ObservableProperty]
    public partial bool IsAddMode { get; set; }

    [ObservableProperty]
    public partial bool IsAddButtonDisabled { get; set; }

    [ObservableProperty]
    public partial bool IsSaveButtonDisabled { get; set; }

    [ObservableProperty]
    public partial bool IsDeleteButtonDisabled { get; set; }


    public EditorViewModel(IEditorDataProvider editorDataProvider, PhotoPostProModel model )
    {
        this.model = model;
        this.editorDataProvider = editorDataProvider;

        this.isFirstActivation = true;
        this.IsEditMode = true;
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);

        if (this.isFirstActivation)
        {
            // This cannot be done in the constructor
            this.isFirstActivation = false;
        }
    }

    public override void Deactivate()
    {
        base.Deactivate();
        this.EditableObjects.Clear();
    }

    public void EnableButtons(bool enabled)
    {
        this.IsAddButtonDisabled = !enabled;
        this.IsSaveButtonDisabled = !enabled;
        this.IsDeleteButtonDisabled = !enabled;
    }
    partial void OnSelectedObjectIndexChanged(int value)
    {
        if (value < 0 || value >= this.EditableObjects.Count)
        {
            return;
        }

        var selected = this.EditableObjects[value];
        if (selected is null)
        {
            return;
        }

        this.FriendlyName = selected.FriendlyName;
        if (selected is AddNewEditable)
        {
            this.IsAddMode = true;
            this.IsEditMode = false;
            this.editor?.BeginAdd();
        }
        else
        {
            this.IsAddMode = false;
            this.IsEditMode = true;
            this.editor?.BeginEdit(selected);
        }
    }

    public void Populate(IEnumerable<IEditable> editableObjects, UserControl editingForm)
    {
        this.EditingForm = editingForm;
        if (this.EditingForm.DataContext is IEditor editor)
        {
            this.editor = editor;
        }
        else
        {
            throw new InvalidOperationException("EditingForm.DataContext must implement IEditor");
        }

        this.Refresh(editableObjects);
    }

    public void Refresh(IEnumerable<IEditable> editableObjects)
    {
        this.EditableObjects.Clear();
        this.EditableObjects.Add(new AddNewEditable(this.Localize("Tools.Editor.AddNew")));
        foreach (var editable in editableObjects)
        {
            this.EditableObjects.Add(editable);
        }

        // Force property changed to ensure that the selected index is reset
        // And will initialize the editing form with 'Add New...'
        this.SelectedObjectIndex = -1;
        this.SelectedObjectIndex = 0;
    }

    [RelayCommand]
    public void OnAdd()
    {
        if (this.editor is null)
        {
            return; 
        }

        // Clicked "Add" button: refresh master list,
        // and then select new item in master list
        if ( this.editor.Add())
        {
            this.editorDataProvider.Refresh();
        }
    }

    [RelayCommand]
    public void OnDelete()
    {
        if (this.editor is null)
        {
            return;
        }

        // Clicked "Delete" button: refresh master list,
        // and then select new item in master list
        if (this.editor.Delete())
        {
            this.editorDataProvider.Refresh();
        }
    }

    [RelayCommand]
    public void OnSave()
    {
        if (this.editor is null)
        {
            return;
        }

        // Clicked "Save" button
        if (this.editor.Save())
        {
            this.editorDataProvider.Refresh();
        }
    }
}
