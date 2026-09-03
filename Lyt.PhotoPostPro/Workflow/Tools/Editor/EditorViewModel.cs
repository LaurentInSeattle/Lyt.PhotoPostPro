namespace Lyt.PhotoPostPro.Workflow.Tools.Editor;

public sealed partial class EditorViewModel : ViewModel<EditorView>
{
    private readonly PhotoPostProModel model;
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
        this.EditableObjects.Clear() ;
    }

    partial void OnSelectedObjectIndexChanged(int value)
    {
        if ( value < 0 || value >= this.EditableObjects.Count)
        {
            return;
        }

        var selected = this.EditableObjects[value];
        if ( selected is null)
        {
            return; 
        }

        this.FriendlyName = selected.FriendlyName; 
    }

    public void Populate(IEnumerable<IEditable> editableObjects, UserControl editingForm)
    {
        this.EditableObjects.Clear();
        foreach (var editable in editableObjects)
        {
            this.EditableObjects.Add(editable);
        }

        this.SelectedObjectIndex = -1; 
        this.EditingForm = editingForm; 
    }

    [RelayCommand]
    public void OnAdd()
    {
    }

    [RelayCommand]
    public void OnDelete()
    {
    }

    [RelayCommand]
    public void OnSave()
    {
    }
}
