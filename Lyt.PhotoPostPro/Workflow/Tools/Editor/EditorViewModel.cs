namespace Lyt.PhotoPostPro.Workflow.Tools.Editor;

public interface IEditor
{
    void BeginAdd();
    void BeginEdit(IEditable editable);
    void Add();
    void Save();
    void Delete();
}


public sealed partial class EditorViewModel : ViewModel<EditorView>
{
    public sealed class AddNewEditable(string friendlyName) : IEditable
    {
        public string FriendlyName { get; set; } = friendlyName;
    }

    private readonly PhotoPostProModel model;

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

    public EditorViewModel(PhotoPostProModel model)
    {
        this.model = model;
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
        this.EditableObjects.Clear();
        this.EditableObjects.Add(new AddNewEditable(this.Localize("Tools.Editor.AddNew")));
        foreach (var editable in editableObjects)
        {
            this.EditableObjects.Add(editable);
        }

        this.SelectedObjectIndex = -1;
        this.EditingForm = editingForm;
        if (this.EditingForm.DataContext is IEditor editor)
        {
            this.editor = editor;
        }
        else
        {
            throw new InvalidOperationException("EditingForm.DataContext must implement IEditor");
        }
    }

    [RelayCommand]
    public void OnAdd()
    {
        // Clicked "Add" button: refresh master list,
        // and then select new item in master list
        this.editor?.Add();
    }

    [RelayCommand]
    public void OnDelete()
    {
        // Clicked "Delete" button: refresh master list,
        // and then select new item in master list
        this.editor?.Delete();
    }

    [RelayCommand]
    public void OnSave()
    {
        // Clicked "Save" button
        this.editor?.Save();
    }
}
