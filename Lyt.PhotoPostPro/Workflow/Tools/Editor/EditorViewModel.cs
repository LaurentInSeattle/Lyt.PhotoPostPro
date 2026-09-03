namespace Lyt.PhotoPostPro.Workflow.Tools.Editor;

public interface IEditableObject
{
    string FriendlyName { get; set; }

    UserControl EditingForm { get; set; }
}

public sealed partial class EditorViewModel : ViewModel<EditorView>
{
    private readonly PhotoPostProModel model;
    private bool isFirstActivation;

    [ObservableProperty]
    // The collection of editable items in the master list - left side 
    public partial ObservableCollection<IEditableObject> EditableObjects { get; set; } = [];

    [ObservableProperty]
    // the currently selected editable item in the master list 
    public partial IEditableObject? SelectedObject { get; set; }

    [ObservableProperty]
    // SelectedIndex in the master list 
    public partial int SelectedObjectIndex { get; set; }

    [ObservableProperty]
    // Selected object name 
    public partial string FriendlyName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial UserControl? EditingForm { get; set; } 

    public EditorViewModel(PhotoPostProModel model)
    {
        this.model = model;
        this.isFirstActivation = true;
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
        var selected = this.SelectedObject;

    }

}
