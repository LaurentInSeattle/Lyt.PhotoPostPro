namespace Lyt.PhotoPostPro.Workflow.Tools.Editor;

public interface IEditor
{
    void BeginAdd();

    void BeginEdit(IEditable editable);
    
    void Add();
    
    void Save();
    
    void Delete();
}
