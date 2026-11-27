namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

using Lyt.Avalonia.Mvvm.Behaviors.DragDrop;
using Lyt.PhotoPostPro.Workflow.Book.Messages;

using Books = Model.PhotoBook;
using Pages = Model.PhotoBook.Pages;
using Slots = Model.PhotoBook.Slots;

using System.IO;

public partial class BookPageViewModel :
    ViewModel<BookPageView>,
    IDropTarget,
    IRecipient<BookGridLinesMessage>,
    IRecipient<BookBindingAreaMessage>,
    IRecipient<BookPageEditedMessage>
{
    public readonly Pages.Page? Page;

    private readonly PhotoPostProModel model;

    [ObservableProperty]
    public partial double PageWidth { get; set; }

    [ObservableProperty]
    public partial double PageHeight { get; set; }

    [ObservableProperty]
    public partial bool IsDummy { get; set; }

    [ObservableProperty]
    public partial bool AreVisibleGridLines { get; set; } = true;

    [ObservableProperty]
    public partial bool IsVisibleBindingArea { get; set; } = true;

    [ObservableProperty]
    public partial HorizontalAlignment BindingAreaAlignmentSide { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<SlotViewModel> Slots { get; set; } = [];

    public BookPageViewModel(PhotoPostProModel model, Pages.Page? page)
    {
        this.model = model;
        this.Page = page;
        this.IsDummy = page is null;

        // ! We do have a book 
        var size = this.model.BookManager.CurrentBook!.PageSize;
        this.PageWidth = size.Width;
        this.PageHeight = size.Height;

        if (this.IsDummy)
        {
            // Even dummy page needs to be sized 
            this.AreVisibleGridLines = false;
            this.IsVisibleBindingArea = false;
            return;
        }

        this.Subscribe<BookGridLinesMessage>();
        this.Subscribe<BookBindingAreaMessage>();
        this.Subscribe<BookPageEditedMessage>();
        this.ShowGridLines(show: false);
        this.ShowBindingArea(show: false);
        this.CreateSlots();
    }

    public void Receive(BookGridLinesMessage message) => this.ShowGridLines(message.ShowGridLines);

    public void Receive(BookBindingAreaMessage message) => this.ShowBindingArea(message.ShowBindingArea);

    public void Receive(BookPageEditedMessage message)
    {
        if (this.Page != message.Page)
        {
            return;
        }

        this.Slots.Clear();
        this.CreateSlots();
    }

    public void ShowGridLines(bool show) => this.AreVisibleGridLines = !this.IsDummy && show;

    public void ShowBindingArea(bool show)
    {
        this.IsVisibleBindingArea = !this.IsDummy && show;

        // ! we are a page 
        this.BindingAreaAlignmentSide = this.Page!.IsFront ? HorizontalAlignment.Left: HorizontalAlignment.Right;
    }

    public void CreateSlots()
    {
        if (this.Page is null)
        {
            return;
        }

        foreach (var slot in this.Page.Slots)
        {
            var slotViewModel = new SlotViewModel(this.model, this.Page, slot);
            slotViewModel.CreateViewAndBind();
            this.Slots.Add(slotViewModel);
        }
    }

    public bool CanDrop(Point point, object dropped)
    {
        if (this.IsDummy)
        {
            return false;
        }

        // We have a selected ImageSlot on the page we cannnot drop 
        foreach (var slot in this.Slots)
        {
            if (slot.CanDrop(point, dropped))
            {
                slot.Highlight(highlight: true);
                return true;
            }
        }

        return true;
    }

    public void OnDrop(Point point, object droppedObject)
    {
        if (this.Page is null)
        {
            return;
        }

        Debug.WriteLine(droppedObject.GetType().FullName);
        if (droppedObject is not BookImageViewModel bookImageViewModel)
        {
            return;
        }

        Guid imageId = bookImageViewModel.Key;
        if (imageId == Guid.Empty)
        {
            return;
        }

        // Ready for drop 
        Debug.WriteLine(" Dropped Guid : " + imageId.ToString());

        // If we have a selected ImageSlot on the page, this is our actual drop target
        foreach (var slot in this.Slots)
        {
            if (slot.CanDrop(point, droppedObject))
            {
                slot.OnDrop(point, droppedObject);
                return;
            }
        }

        // No image slot selected we edit the entire page 
        this.model.BookManager.AddDroppedImage(this.Page, imageId);
    }
}
