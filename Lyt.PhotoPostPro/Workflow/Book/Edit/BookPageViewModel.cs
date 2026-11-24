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
    IRecipient<BookPageEditedMessage>
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
        if (this.IsDummy)
        {
            return;
        }

        // ! We do have a book 
        var size = this.model.BookManager.CurrentBook!.PageSize;
        this.PageWidth = size.Width;
        this.PageHeight = size.Height;

        this.Subscribe<BookGridLinesMessage>();
        this.Subscribe<BookPageEditedMessage>();
        this.ShowGridLines(show: false);
        this.CreateSlots();
    }

    //public override void OnViewLoaded()
    //{
    //    base.OnViewLoaded();
    //    Schedule.OnUiThread(60, () => 
    //    {
    //        if (this.IsBound)
    //        {
    //            this.View.AddSlots(this.Slots);
    //        } 
    //        else
    //        {
    //            if ( Debugger.IsAttached) { Debugger.Break();  }
    //        }
    //    }, DispatcherPriority.Background);          
    //} 

    public void Receive(BookGridLinesMessage message) => this.ShowGridLines(message.ShowGridLines);

    public void Receive(BookPageEditedMessage message)
    {
        if (this.page != message.Page)
        {
            return;
        }

        this.Slots.Clear();
        this.CreateSlots();
        //this.View.AddSlots(this.Slots);
    }

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
        if (this.page is null)
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
        this.model.BookManager.AddDroppedImage(this.page, imageId);
    }
}
