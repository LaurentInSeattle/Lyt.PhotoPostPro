namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

using Lyt.PhotoPostPro.Model.PhotoBook.Typography;

using static TextStyleUtilities; 

public partial class TextStyleViewModel :
    ViewModel<TextStyleView>
    // , IRecipient<DocPageNavigateMessage>
{
    public readonly SlotTextStyle SlotTextStyle;

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

    public TextStyleViewModel(SlotTextStyle slotTextStyle)
    {
        this.SlotTextStyle = slotTextStyle;

        // Styling the text block 
        // this.PageColor = Page.
        this.Text = this.SlotTextStyle.FriendlyName;
        this.FontFamily = new FontFamily(this.SlotTextStyle.FontFamily);
        this.FontSize = this.SlotTextStyle.FontSize;
        this.ForegroundColor = FromTextStyle(this.SlotTextStyle);
        var fontStyles = FromTextStyleFontStyle(this.SlotTextStyle);
        this.FontStyle = fontStyles.Item1;
        this.FontWeight = fontStyles.Item2;
        this.TextAlignment = FromTextStyleTextAlignment(this.SlotTextStyle);
    }
}
