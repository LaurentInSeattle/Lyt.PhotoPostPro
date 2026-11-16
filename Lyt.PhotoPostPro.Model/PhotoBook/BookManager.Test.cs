namespace Lyt.PhotoPostPro.Model.PhotoBook;

public sealed partial class BookManager
{
    public static void Test(FileManagerModel fileManager)
    {
        const string VeneziaImagePath = "C:\\Users\\Laurent\\Desktop\\TestImages\\Saved Pictures\\Venezia.jpg";
        const string ToscanaImagePath = "C:\\Users\\Laurent\\Desktop\\TestImages\\Saved Pictures\\Toscana.jpg";
        const string CapriImagePath = "C:\\Users\\Laurent\\Desktop\\TestImages\\Saved Pictures\\Capri3.jpg";
        const string VeronaImagePath = "C:\\Users\\Laurent\\Desktop\\TestImages\\Saved Pictures\\Verona.jpg";

        var book = new Book
        {
            UniqueName = "TestBook",
            FolderName = "TestBook",
            Description = "This is a test book!",
            OutputPdfFileName = "C:\\Users\\Laurent\\Desktop\\PhotoRebel\\TestBook.pdf",
            BookPage = BookPage.UsLetter,
            Format = BookFormat.Landscape,
            DefaultPageBackground = PageBackground.Black,
            WithFrontCover = true,
            WithBackCover = false,
        };

        book.Initialize();

        // ! ok
        var front = book.FrontCover!.FrontPage;
        FullPageImageLayout.Create(book, front, VeneziaImagePath);

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
                ImageFileName = ToscanaImagePath,
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
                ImageFileName = VeronaImagePath,
            });

        page1.Slots.Add(
            new TextSlot()
            {
                Position = new SlotPosition(x: 40, y: 340, dx: 240, dy: 120),
                Text = "Ponte Pietra a Verona",
            });

        var page2 = book.FromPageNumber(2);
        SingleImageLayoutWithLegend.Create(book, page2, CapriImagePath, "Giardini di Augusto - Isola di Capri");


        book.GeneratePdf();

        const string BookPath = "C:\\Users\\Laurent\\Desktop\\PhotoRebel\\TestBook.json"; 
        var jsonTypeInfo = AppJsonContext.Default.Book;
        string serialized = fileManager.Serialize(book, jsonTypeInfo);
        File.WriteAllText(BookPath, serialized);

        string read = File.ReadAllText(BookPath);
        var maybeBook = fileManager.Deserialize<Book>(read, jsonTypeInfo); 
        if ( maybeBook is Book readBook )
        {
            readBook.OutputPdfFileName = "C:\\Users\\Laurent\\Desktop\\PhotoRebel\\TestBookReturn.pdf";
            readBook.GeneratePdf(); 
        }
    }
}
