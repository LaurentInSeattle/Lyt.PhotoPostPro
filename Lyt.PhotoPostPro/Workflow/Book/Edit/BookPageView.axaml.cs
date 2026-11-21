namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

using Lyt.Avalonia.Mvvm.Behaviors.DragDrop;

using static Lyt.Avalonia.Controls.Utilities;


public partial class BookPageView : View
{
    private static readonly SolidColorBrush? hotBrush;

    static BookPageView()
    {
        TryFindResource<SolidColorBrush>("FreshGreen_1_100", out SolidColorBrush? brush);
        if (brush is not null)
        {
            hotBrush = brush;
        }
    }

    public BookPageView() : base()
    {
        DragDrop.SetAllowDrop(this, true);
        this.AddHandler(DragDrop.DragEnterEvent, this.OnDragEnter);
        this.AddHandler(DragDrop.DragLeaveEvent, this.OnDragLeave);
    }

    ~BookPageView()
    {
        this.RemoveHandler(DragDrop.DragEnterEvent, this.OnDragEnter);
        this.RemoveHandler(DragDrop.DragLeaveEvent, this.OnDragLeave);
    }

    protected override void OnDataContextChanged(object? sender, EventArgs e)
    {
        base.OnDataContextChanged(sender, e);
        new DragOverAble(/* StageView.HideDropTarget*/ ).Attach(this);
        new DropAble(/* StageView.HideDropTarget*/ ).Attach(this);
    }

    private void OnDragEnter(object? _, DragEventArgs e)
    {
        if (hotBrush is not null)
        {
            this.DropRectangle.Stroke = hotBrush;
        }
    }

    private void OnDragLeave(object? _, DragEventArgs e)
    {
        this.DropRectangle.Stroke = Brushes.Transparent;
    }

}