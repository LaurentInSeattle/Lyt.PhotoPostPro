namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

using Lyt.PhotoPostPro.Model.PhotoBook.Typography;

using Books = Model.PhotoBook;
using Pages = Model.PhotoBook.Pages;
using Slots = Model.PhotoBook.Slots;

public partial class StylingPanelViewModel :
    ViewModel<StylingPanelView>,
    IRecipient<BookTextSlotSelectedMessage>
{
    private bool isProgammaticUpdate;
    private SlotViewModel? selectedTextSlot; 

    [ObservableProperty]
    // The collection of styles in the list box
    public partial ObservableCollection<TextStyleViewModel> Styles { get; set; } = [];

    [ObservableProperty]
    // SelectedIndex in the styles list box  
    public partial int SelectedStyleIndex { get; set; }

    public StylingPanelViewModel(PhotoPostProModel model)
    {
        this.Subscribe<BookTextSlotSelectedMessage>(); 
            
        var styles = model.TextStyles.AvailableSlotTextStyles;
        List<TextStyleViewModel> list = [with(Styles.Count)];
        foreach (var style in styles)
        {
            TextStyleViewModel textStyleViewModel = new(style);
            list.Add(textStyleViewModel);
        }

        this.Styles = [with(list)];
    }

    public void Receive(BookTextSlotSelectedMessage message)
    {
        this.selectedTextSlot = message.TextSlotViewModel; 
        if (this.selectedTextSlot is null)
        {
            // nothing selected 
            return; 
        }

        if (this.selectedTextSlot.Slot is not Slots.TextSlot textSlot)
        {
            // not text , should never happen 
            return;
        }

        string styleName = textSlot.Style.FriendlyName; 
        With.Flag(ref this.isProgammaticUpdate, () =>
        {
            // Find matching style in list             
            for (int index = 0; index < this.Styles.Count; ++index)
            {
                var styleViewModel = this.Styles[index]; 
                if ( styleViewModel.SlotTextStyle.FriendlyName == styleName)
                {
                    this.SelectedStyleIndex = index;
                    break; 
                }
            }
        }); 
    }

    partial void OnSelectedStyleIndexChanged(int value)
    {
        if (this.isProgammaticUpdate)
        {
            return; 
        }

        if (this.selectedTextSlot is null)
        {
            // nothing selected 
            return;
        }

        if (this.selectedTextSlot.Slot is not Slots.TextSlot)
        {
            // not text , should never happen 
            return;
        }

        var style = this.Styles[value].SlotTextStyle;
        new BookApplyStyleMessage(this.selectedTextSlot, style).Publish(); 
    }
}
