namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

using Lyt.Avalonia.Mvvm.Behaviors.DragDrop;
using Lyt.PhotoPostPro.Workflow.Book.Messages;

using Books = Model.PhotoBook;
using Pages = Model.PhotoBook.Pages;
using Slots = Model.PhotoBook.Slots;

public partial class BookPageViewModel : 
    ViewModel<BookPageView>,
    IDropTarget, 
    IRecipient<BookGridLinesMessage>
{
    private readonly PhotoPostProModel model;

    private readonly Pages.Page? page;

    [ObservableProperty]
    public partial double PageWidth { get; set; }

    [ObservableProperty]
    public partial double PageHeight { get; set; }

    [ObservableProperty]
    public partial bool IsDummy { get; set; }

    [ObservableProperty]
    public partial bool AreVisibleGridLines { get; set; } = true;

    [ObservableProperty]
    public partial ObservableCollection<SlotViewModel> Slots { get; set; } = [];

    public BookPageViewModel(PhotoPostProModel model, Pages.Page? page)
    {
        this.model = model;
        this.page = page;
        this.IsDummy = page is null;
        if ( this.IsDummy)
        {
            return; 
        }

        // ! We do have a book 
        var size = this.model.BookManager.CurrentBook!.PageSize; 
        this.PageWidth = size.Width;
        this.PageHeight = size.Height;

        this.Subscribe<BookGridLinesMessage>(); 
        this.ShowGridLines(show: false);
        this.CreateSlots();
    }

    public void Receive(BookGridLinesMessage message) => this.ShowGridLines(message.ShowGridLines); 

    public void ShowGridLines(bool show) => this.AreVisibleGridLines = !this.IsDummy && show;

    public void CreateSlots()
    {
        if (this.page is null)
        {
            return;
        }

        foreach (var slot in this.page.Slots)
        {
            var slotViewModel = new SlotViewModel(this.model, this.page, slot);
            this.Slots.Add(slotViewModel);
        }
    }

    public bool CanDrop(Point point, object droppedObject)
    {
        return true; 
    }

    public void OnDrop(Point point, object droppedObject)
    {

    }
}
