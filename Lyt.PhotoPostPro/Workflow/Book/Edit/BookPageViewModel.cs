namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

using Books = Model.PhotoBook;
using Pages = Model.PhotoBook.Pages;
using Slots = Model.PhotoBook.Slots;

public partial class BookPageViewModel : ViewModel<BookPageView>
// , IRecipient<DocPageNavigateMessage>
{
    private readonly PhotoPostProModel model;

    private readonly Pages.Page? page;

    [ObservableProperty]
    public partial bool IsDummy { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush GridLinesStrokeBrush { get; set; } = new(Colors.Transparent);

    [ObservableProperty]
    public partial ObservableCollection<SlotViewModel> Slots { get; set; } = [];

    public BookPageViewModel(PhotoPostProModel model, Pages.Page? page)
    {
        this.model = model;
        this.page = page;
        this.IsDummy = page is null;
        this.ShowGridLines(show: true);
        this.CreateSlots();
    }

    public void ShowGridLines(bool show)
        => this.GridLinesStrokeBrush = show ? new(Colors.DarkGray) : new(Colors.Transparent);

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
}
