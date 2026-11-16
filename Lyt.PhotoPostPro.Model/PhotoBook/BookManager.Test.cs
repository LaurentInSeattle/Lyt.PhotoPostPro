namespace Lyt.PhotoPostPro.Model.PhotoBook;

public sealed partial class BookManager
{
    public void Test()
    {
        const string VeneziaImagePath = "C:\\Users\\Laurent\\Desktop\\TestImages\\Saved Pictures\\Venezia.jpg";
        const string ToscanaImagePath = "C:\\Users\\Laurent\\Desktop\\TestImages\\Saved Pictures\\Toscana.jpg";
        const string CapriImagePath = "C:\\Users\\Laurent\\Desktop\\TestImages\\Saved Pictures\\Capri3.jpg";
        const string VeronaImagePath = "C:\\Users\\Laurent\\Desktop\\TestImages\\Saved Pictures\\Verona.jpg";

        var book = this.GetBook("TestBook");
        if (book is not null)
        {
            this.DeleteBook(book, out string deleteMessage); 
        }

        book = this.CreateBook(
            "TestBook",
            "This is a test book!",
            BookPage.UsLetter,
            BookFormat.Landscape,
            PageBackground.Black,
            withFrontCover: true,
            withBackCover: false,
            out string message);
        if (book is null)
        {
            Debug.WriteLine( "  Create Book : " + message ); 
            return;
        }

        var venezia = book.ImportImage(this.photobooksFolderPath, VeneziaImagePath, out message);
        var toscana = book.ImportImage(this.photobooksFolderPath, ToscanaImagePath, out message);
        var capri = book.ImportImage(this.photobooksFolderPath, CapriImagePath, out message);
        var verona = book.ImportImage(this.photobooksFolderPath, VeronaImagePath, out message);

        book.IncludeImage(venezia);
        book.IncludeImage(toscana);
        book.IncludeImage(capri);
        book.IncludeImage(verona);

        // ! ok
        var front = book.FrontCover!.FrontPage;
        FullPageImageLayout.Create(book, front, venezia);

        // ! ok
        var back = book.FrontCover!.BackPage;
        EmptyLayout.Create(book, back);
        var titleStyle = new SlotTextStyle() { FontSize = 40, TextAlignment = BookTextAlignment.Center };
        back.Slots.Add(
            new TextSlot()
            {
                Position = new SlotPosition(x: 0, y: 300, dx: 792, dy: 200),
                Text = "Bellissima Italia, Amo Te!",
                Style = titleStyle,
            });

        var page1 = book.FromPageNumber(1);
        page1.Slots.Add(
            new ImageSlot()
            {
                Position = new SlotPosition(x: 40, y: 40, dx: 400, dy: 300),
                BookImageId = toscana,
            });

        page1.Slots.Add(
            new TextSlot()
            {
                Position = new SlotPosition(x: 480, y: 40, dx: 300, dy: 100),
                Text = "Paesaggio della Toscana",
            });

        page1.Slots.Add(
            new ImageSlot()
            {
                Position = new SlotPosition(x: 300, y: 340, dx: 400, dy: 300),
                BookImageId = verona,
            });

        page1.Slots.Add(
            new TextSlot()
            {
                Position = new SlotPosition(x: 40, y: 340, dx: 240, dy: 120),
                Text = "Ponte Pietra a Verona",
            });

        var page2 = book.FromPageNumber(2);
        SingleImageLayoutWithLegend.Create(book, page2, capri, "Giardini di Augusto - Isola di Capri");

        this.SaveBook(book);

        book.GeneratePdf(BookResolution.Print);
    }
}
