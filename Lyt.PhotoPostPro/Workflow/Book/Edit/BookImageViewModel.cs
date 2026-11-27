namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

using Lyt.Avalonia.Mvvm.Behaviors.DragDrop;

public partial class BookImageViewModel : ViewModel<BookImageView>, IDragAbleViewModel
// , IRecipient<DocPageNavigateMessage>
{
    public const string CustomDragAndDropFormat = "BookImageViewModel";

    public readonly Guid Key;

    [ObservableProperty]
    public partial Bitmap Bitmap { get; set; }

    public BookImageViewModel(Guid key, Bitmap bitmap, bool isGhost = false)
    {
        this.Key = key;
        this.Bitmap = bitmap;
        this.IsGhost = isGhost;
    }

    public void Detach ()
    {
        if (this.DragAble is null)
        {
            return; 
        }

        this.DragAble.Detach();
    }

    public void AttachDraggable(ListBox listBox, Canvas canvas)
    {
        if (!this.IsGhost)
        {
            if (listBox.ContainerFromItem(this) is ListBoxItem container)
            {
                if (container.Content is BookImageViewModel && container.Presenter is not null)
                {
                    if (container.Presenter.Child is BookImageView view)
                    {
                        if (this.DragAble is null)
                        {
                            this.DragAble = new DragAble(canvas);
                        }

                        if (this.DragAble.IsAttached)
                        {
                            // Resolves the case of going full screen: views have been re-created
                            if (this.DragAble.AssociatedObject != view)
                            {
                                this.DragAble.Detach();
                                this.DragAble.Attach(view);
                            }
                        }
                        else
                        {
                            this.DragAble.Attach(view);
                        }
                    }
                }
            }
        }
    }

    /// <summary> True when this is a ghost view model. </summary>
    public bool IsGhost { get; private set; }

    public string DragDropFormat => BookImageViewModel.CustomDragAndDropFormat;

    public DragAble? DragAble { get; protected set; }

    public void OnEntered()
    {

    }

    public void OnExited()
    {

    }


    public void OnLongPress()
    {
        // Show bigger image ? 
    }


    public void OnClicked(bool isRightClick)
    {
    }

    public bool OnBeginDrag() => true;

    public View CreateGhostView()
    {
        var ghostViewModel = new BookImageViewModel(this.Key, this.Bitmap, isGhost: true);
        ghostViewModel.CreateViewAndBind();
        var view = ghostViewModel.View;
        view.ZIndex = 999_999;
        view.Opacity = 0.8;
        view.InvalidateVisual();
        return view;
    }
}
