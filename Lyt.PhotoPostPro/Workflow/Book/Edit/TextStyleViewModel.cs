namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

using Lyt.PhotoPostPro.Model.PhotoBook.Pages;
using Lyt.PhotoPostPro.Model.PhotoBook.Slots;
using Lyt.PhotoPostPro.Model.PhotoBook.Typography;

using static TextStyleUtilities; 

public partial class TextStyleViewModel :
    ViewModel<TextStyleView>
    // , IRecipient<DocPageNavigateMessage>
{
    private readonly SlotTextStyle slotTextStyle;

    [ObservableProperty]
    public partial SolidColorBrush PageColor { get; set; } = new(Colors.Black);

    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    [ObservableProperty]
    public partial FontFamily FontFamily { get; set; } = SlotViewModel.DefaultFontFamily;

    [ObservableProperty]
    public partial double FontSize { get; set; }

    [ObservableProperty]
    public partial FontStyle FontStyle { get; set; }

    [ObservableProperty]
    public partial FontWeight FontWeight { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush ForegroundColor { get; set; } = new(Colors.White);

    [ObservableProperty]
    public partial TextAlignment TextAlignment { get; set; }

    public TextStyleViewModel(Page page, SlotTextStyle slotTextStyle)
    {
        this.slotTextStyle = slotTextStyle;

        // Styling the text block 
        // this.PageColor = Page.
        this.Text = this.slotTextStyle.FriendlyName;
        this.FontFamily = new FontFamily(this.slotTextStyle.FontFamily);
        this.FontSize = this.slotTextStyle.FontSize;
        this.ForegroundColor = FromTextStyle(this.slotTextStyle);
        var fontStyles = FromTextStyleFontStyle(this.slotTextStyle);
        this.FontStyle = fontStyles.Item1;
        this.FontWeight = fontStyles.Item2;
        this.TextAlignment = FromTextStyleTextAlignment(this.slotTextStyle);
    }
}
