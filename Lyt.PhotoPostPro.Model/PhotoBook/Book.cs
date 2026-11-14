
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Lyt.PhotoPostPro.Model.PhotoBook;

// See:  https://github.com/GraphicMeat/PhotoBooks/blob/main/README.md 

using Color = QuestPDF.Infrastructure.Color;

public sealed class Book
{
    public Book( /* Required for serialization */ ) { }

    public string UniqueName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string OutputPdfFileName { get; set; } = string.Empty;

    public BookPage BookPage { get; set; } = BookPage.UsLetter;

    public BookFormat Format { get; set; } = BookFormat.Landscape;

    public bool WithFrontCover { get; set; } = true;

    public FrontCover? FrontCover { get; set; }

    public bool WithBackCover { get; set; } = true;

    public BackCover? BackCover { get; set; } 

    public List<Page> Pages { get; } = [];

    public PageBackground DefaultPageBackground { get; set; } = PageBackground.Black;

    public List<SlotTextStyle> TextStyles { get; } = [];

    public Page? FromPageNumber (int pageNumber)
        => this.Pages.FirstOrDefault(p => p.PageNumber == pageNumber);

    public PageSize PageSizeBase =>
        this.BookPage switch
        {
            BookPage.UsLetter => PageSizes.Letter,
            BookPage.EuroA4 => PageSizes.A4,
            _ => throw new NotImplementedException($"Book page {this.BookPage} is not implemented."),
        };

    public PageSize PageSize =>
            this.Format switch
            {
                BookFormat.Landscape => this.PageSizeBase.Landscape(),
                BookFormat.Portrait => this.PageSizeBase.Portrait(),
                _ => throw new NotImplementedException($"Book format {this.Format} is not implemented."),
            };

    public void Initialize()
    { 
        if ( this.WithFrontCover)
        {
            this.FrontCover = new FrontCover(this);
        }

        this.Pages.Add(new Page(this, 1));
        this.Pages.Add(new Page(this, 2));

        if (this.WithBackCover)
        {
            this.BackCover = new BackCover(this);
        }
    }


    /*
     * 
     * 
By wrapping your image in an Unconstrained() element, you remove it from the normal flow of the parent container. 
    Then, use Translate(x, y) to shift it to specific coordinates.     * 

container.Layers(layers =>
{
    // Layer 1: Normal document content goes here
    layers.PrimaryLayer().Text("Normal flow content...");

    // Layer 2: Absolute position overlay
    layers.Layer().Unconstrained().Translate(x: 100, y: 200).Element(e =>
    {
        e.Width(150) // Optional: restrict size
         .Image("path/to/your/image.png")
         .FitArea();
    });
});


using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

To place blocks of text at exact coordinates in QuestPDF, combine .Layers(), .Unconstrained(), 
and .OffsetX() / .OffsetY() (formerly named .TranslateX() / .TranslateY())


Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4);
        // Set margin to 0 so (0, 0) corresponds to the exact top-left corner of the physical page
        page.Margin(0); 

        page.Content().Layers(layers =>
        {
            // A PrimaryLayer is required to establish the base canvas
            layers.PrimaryLayer();

            // Block 1: Placed at X = 100pt, Y = 150pt
            layers.Layer()
                .Unconstrained()
                .OffsetX(100)
                .OffsetY(150)
                .Text("First absolute text block")
                .FontSize(14);

            // Block 2: Placed at X = 300pt, Y = 420pt
            layers.Layer()
                .Unconstrained()
                .OffsetX(300)
                .OffsetY(420)
                .Width(200) // Optional: constrains width so long text wraps properly
                .Text("Second block with constrained wrapping width.")
                .FontSize(12);
        });
    });
});

     
     
     * 
     * 
    */
    public void GeneratePdf()
    {
        Color pageBackgroundColor =
            this.DefaultPageBackground switch
            {
                PageBackground.Black => Colors.Black,
                PageBackground.White => Colors.White,
                _ => throw new NotImplementedException($"Page background {this.DefaultPageBackground} is not implemented."),
            };

        void SetupPageDefaults(PageDescriptor page)
        {
            page.PageColor(pageBackgroundColor);
            page.Size(this.PageSize);
            page.Margin(0, Unit.Point);
            page.DefaultTextStyle(x => x.FontSize(20));
        }

        void CreatePage (Page page, PageDescriptor pageDescriptor)
        {
            SetupPageDefaults(pageDescriptor);
            pageDescriptor.Content()
            .Column(column =>
            {
                // Add content to the column
                foreach (var slot in page.Slots)
                {
                    var position = slot.Position;
                    if (slot is ImageSlot imageSlot)
                    {
                        if (!File.Exists(imageSlot.ImageFileName))
                        {
                            // Skip this slot if the image file does not exist
                            Debug.WriteLine($"Image file not found: {imageSlot.ImageFileName}");
                            continue;
                        }

                        // Need to crop the source image to match the page aspect ratio
                        byte[] imageBytes = File.ReadAllBytes(imageSlot.ImageFileName);
                        column.Item()
                            .OffsetX(position.X, Unit.Point)
                            .OffsetY(position.Y, Unit.Point)
                            .Width(position.Dx, Unit.Point)
                            .Height(position.Dy, Unit.Point)
                            .Image(imageBytes)
                            .FitArea();
                    }
                    else if (slot is TextSlot textSlot)
                    {
                        column.Item()
                            .OffsetX(position.X, Unit.Point)
                            .OffsetY(position.Y, Unit.Point)
                            .Width(position.Dx, Unit.Point)
                            .Height(position.Dy, Unit.Point)
                            .Text(textSlot.Text);
                    }
                }
            });
        }

        Document.Create(container =>
        {
            if ( this.WithFrontCover && this.FrontCover is not null)
            {
                container.Page(frontCoverFront =>
                {
                    CreatePage(this.FrontCover.FrontPage, frontCoverFront);
                });

                container.Page(frontCoverBack =>
                {
                    CreatePage(this.FrontCover.BackPage, frontCoverBack);
                });
            }

            foreach (var page in this.Pages)
            {
                container.Page(pageContainer =>
                {
                    CreatePage(page, pageContainer);
                });
            }

            if (this.WithBackCover && this.BackCover is not null)
            {
                container.Page(backCoverFront =>
                {
                    CreatePage(this.BackCover.FrontPage, backCoverFront);
                });
                container.Page(backCoverBack =>
                {
                    CreatePage(this.BackCover.BackPage, backCoverBack);
                });
            }
        }).GeneratePdf(this.OutputPdfFileName); 
    }
} 
