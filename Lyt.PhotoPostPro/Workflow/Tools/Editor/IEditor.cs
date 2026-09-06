namespace Lyt.PhotoPostPro.Workflow.Tools.Editor;

public interface IEditor
{
    void BeginAdd();

    void BeginEdit(IEditable editable);
    
    bool Add();
    
    bool Save();
    
    bool Delete();
}

public interface IEditorDataProvider
{
    void Refresh();
}
