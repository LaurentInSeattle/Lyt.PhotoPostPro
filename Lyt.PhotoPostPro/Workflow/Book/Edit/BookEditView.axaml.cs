namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

using Lyt.Avalonia.Mvvm.Behaviors.DragDrop;

public partial class BookEditView : View
{
    protected override void OnDataContextChanged(object? sender, EventArgs e)
    {
        base.OnDataContextChanged(sender, e);
        new DragOverAble(/* StageView.HideDropTarget*/ ).Attach(this);
    }
}
