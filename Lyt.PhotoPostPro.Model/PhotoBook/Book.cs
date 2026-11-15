
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Lyt.PhotoPostPro.Model.PhotoBook;

// See:  https://github.com/GraphicMeat/PhotoBooks/blob/main/README.md 

using Color = QuestPDF.Infrastructure.Color;

public sealed partial class Book
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

    public Page FromPageNumber(int pageNumber)
    {
        var page = this.MaybeFromPageNumber(pageNumber);
        if (page is null)
        {
            if (Debugger.IsAttached) { Debugger.Break(); }
            throw new Exception("Invalid page number");
        }

        return page;
    }

    public Page? MaybeFromPageNumber(int pageNumber)
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
        if (this.WithFrontCover)
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

        void CreatePage(Page page, PageDescriptor pageDescriptor)
        {
            SetupPageDefaults(pageDescriptor);

            // By wrapping the images in an Unconstrained() element, we remove it from the normal flow of
            // the parent container. Then, using Offsets to shift it to specific coordinates.
            pageDescriptor.Content()
            .Layers(layers =>
            {
                // A PrimaryLayer is required to establish the base canvas
                layers.PrimaryLayer();

                // Add content as layers based on the slots in the page
                foreach (var slot in page.Slots)
                {
                    var position = slot.Position;
                    var descriptor =
                        layers.Layer()
                            .Unconstrained()
                            .OffsetX(position.X, Unit.Point)
                            .OffsetY(position.Y, Unit.Point)
                            .Width(position.Dx, Unit.Point)
                            .Height(position.Dy, Unit.Point); 
                    if (slot is ImageSlot imageSlot)
                    {
                        if (string.IsNullOrWhiteSpace(imageSlot.ImageFileName))
                        {
                            // Skip this slot if the image file is not known yet 
                            Debug.WriteLine($"No image file for slot: {imageSlot.Id}");
                            continue;
                        }

                        if (!File.Exists(imageSlot.ImageFileName))
                        {
                            // Skip this slot if the image file does not exist
                            Debug.WriteLine($"Image file not found: {imageSlot.ImageFileName}");
                            continue;
                        }

                        // Need to crop the source image to match the page aspect ratio
                        byte[] imageBytes = File.ReadAllBytes(imageSlot.ImageFileName);
                        var imageDescriptor = descriptor.Image(imageBytes); 
                        if ( imageSlot.ImageStretch == BookImageStretch.Stretch)
                        {
                            imageDescriptor.FitUnproportionally();
                        }
                        else
                        {
                            imageDescriptor.FitArea();
                        }
                    }
                    else if (slot is TextSlot textSlot)
                    {
                        if (string.IsNullOrWhiteSpace(textSlot.Text))
                        {
                            // Skip this slot if the text is not known yet 
                            Debug.WriteLine($"No text for slot: {textSlot.Id}");
                            continue;
                        }

                        var style = textSlot.Style;
                        var textDescriptor = descriptor
                            .Text(textSlot.Text)
                            .FontFamily(style.FontFamily)
                            .FontSize(style.FontSize)
                            .FontColor(style.Color);
                        if (style.FontStyle == BookFontStyle.Bold)
                        {
                            textDescriptor.Bold(); 
                        }
                        else if (style.FontStyle == BookFontStyle.Italic)
                        {
                            textDescriptor.Italic();
                        }
                        else if (style.FontStyle == BookFontStyle.BoldItalic)
                        {
                            textDescriptor.Bold().Italic();
                        }
                        // else: Regular, default, nothing to do 

                        if (style.TextAlignment == BookTextAlignment.Center)
                        {
                            textDescriptor.AlignCenter();
                        }
                        else if (style.TextAlignment == BookTextAlignment.Right)
                        {
                            textDescriptor.AlignRight();
                        }
                        // else : defaults to align left 
                    }
                }
            });
        }

        Document.Create(container =>
        {
            if (this.WithFrontCover && this.FrontCover is not null)
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
