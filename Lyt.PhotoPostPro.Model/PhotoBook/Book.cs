
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

    public FrontCover? FrontCover { get; set; } 

    public BackCover? BackCover { get; set; } 

    public List<Page> Pages { get; } = [];

    public PageBackground DefaultPageBackground { get; set; } = PageBackground.Black;

    public List<SlotTextStyle> TextStyles { get; } = [];

    public void Initialize()
    { 
        this.FrontCover = new FrontCover(this);
        this.Pages.Add(new Page(this, 1));
        this.Pages.Add(new Page(this, 2));
        this.BackCover = new BackCover(this);
    }

    public void GeneratePdf()
    {
        var pageSizeBase =
            this.BookPage switch
            {
                BookPage.UsLetter => PageSizes.Letter,
                BookPage.EuroA4 => PageSizes.A4,
                _ => throw new NotImplementedException($"Book page {this.BookPage} is not implemented."),
            };
        var pageSize =
            this.Format switch
            {
                BookFormat.Landscape => pageSizeBase.Landscape(),
                BookFormat.Portrait => pageSizeBase.Portrait(),
                _ => throw new NotImplementedException($"Book format {this.Format} is not implemented."),
            };

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
            page.Size(pageSize);
            page.Margin(0, Unit.Point);
            page.DefaultTextStyle(x => x.FontSize(20));
        }

        Document.Create(container =>
        {
            container.Page(frontCoverFront =>
            {
                SetupPageDefaults(frontCoverFront);
                frontCoverFront.Content()
                .Column(column =>
                {
                    // Add content to the column
                });
            });

            container.Page(frontCoverBack =>
            {
                SetupPageDefaults(frontCoverBack);
                frontCoverBack.Content()
                .Column(column =>
                {
                    // Add content to the column
                });
            });

            foreach (var page in this.Pages)
            {
                container.Page(pageContainer =>
                {
                    SetupPageDefaults(pageContainer);
                    pageContainer.Content()
                    .Column(column =>
                    {
                        // Add content to the column
                        foreach (var slot in page.Slots)
                        {
                            var position = slot.Position;
                            if (slot is ImageSlot imageSlot)
                            {

                                byte[] imageBytes = File.ReadAllBytes(imageSlot.ImageFileName);
                                column.Item()
                                    .OffsetX(position.X, Unit.Point)
                                    .OffsetY(position.Y, Unit.Point)
                                    .Width(position.Dx, Unit.Point)
                                    .Height(position.Dy, Unit.Point)
                                    .Image(imageBytes);
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
                });
            }

            container.Page(backCoverFront =>
            {
                SetupPageDefaults(backCoverFront);
                backCoverFront.Content()
                .Column(column =>
                {
                    // Add content to the column
                });
            });

            container.Page(backCoverBack =>
            {
                SetupPageDefaults(backCoverBack);
                backCoverBack.Content()
                .Column(column =>
                {
                    // Add content to the column
                });
            });

        }).GeneratePdf(this.OutputPdfFileName); 
    }
} 
