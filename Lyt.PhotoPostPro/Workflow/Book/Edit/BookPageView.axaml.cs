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
        this.AddHandler(DragDrop.DropEvent, this.OnDrop);
        this.AddHandler(DragDrop.DragEnterEvent, this.OnDragEnter);
        this.AddHandler(DragDrop.DragLeaveEvent, this.OnDragLeave);
        this.AddHandler(PointerExitedEvent, this.OnLeave);
    }

    ~BookPageView()
    {
        this.RemoveHandler(DragDrop.DragEnterEvent, this.OnDragEnter);
        this.RemoveHandler(DragDrop.DragLeaveEvent, this.OnDragLeave);
        this.RemoveHandler(PointerExitedEvent, this.OnLeave);
    }

    protected override void OnDataContextChanged(object? sender, EventArgs e)
    {
        base.OnDataContextChanged(sender, e);
        if (this.DataContext is BookPageViewModel bookPageViewModel &&
            !bookPageViewModel.IsDummy)
        {
            new DragOverAble(/* StageView.HideDropTarget*/ ).Attach(this);
            new DropAble(/* StageView.HideDropTarget*/ ).Attach(this);
        }
    }

    private void OnDragEnter(object? _, DragEventArgs e)
    {
        if (this.DataContext is BookPageViewModel bookPageViewModel &&
            !bookPageViewModel.IsDummy)
        {
            if (hotBrush is not null)
            {
                this.DropRectangle.Stroke = hotBrush;
            }
        }
    }

    private void OnDragLeave(object? _, DragEventArgs e) => this.DropRectangle.Stroke = Brushes.Transparent;

    private void OnDrop(object? _, DragEventArgs e) => this.DropRectangle.Stroke = Brushes.Transparent;

    private void OnLeave(object? _, EventArgs e) => this.DropRectangle.Stroke = Brushes.Transparent;

    private void OnCanvasPointerPressed(object? sender, global::Avalonia.Input.PointerPressedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        topLevel?.FocusManager?.Focus(null);
    }
}