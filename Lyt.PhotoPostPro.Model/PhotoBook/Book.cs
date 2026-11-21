
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Lyt.PhotoPostPro.Model.PhotoBook;

// See:  https://github.com/GraphicMeat/PhotoBooks/blob/main/README.md 

using Color = QuestPDF.Infrastructure.Color;

public sealed partial class Book : IEditable
{
    public const string LowResPrefix = "Low_";
    public const string MediumResPrefix = "Medium_";
    public const string HighResPrefix = "High_";
    public const string JpgExtension = ".jpg";


    public Book( /* Required for serialization */ ) { }

    public string FriendlyName { get; set; } = string.Empty;

    [JsonIgnore]
    public bool CanDelete => true;

    public DateTime Created = DateTime.UtcNow;

    public DateTime Updated = DateTime.UtcNow;

    public string FolderName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string OutputPdfFileName { get; set; } = string.Empty;

    public BookPage BookPage { get; set; } = BookPage.UsLetter;

    public BookFormat Format { get; set; } = BookFormat.Landscape;

    public PageBackground DefaultPageBackground { get; set; } = PageBackground.Black;

    public bool WithFrontCover { get; set; } = true;

    public FrontCover? FrontCover { get; set; }

    public bool WithBackCover { get; set; } = true;

    public BackCover? BackCover { get; set; }

    public List<Page> Pages { get; set; } = [];

    public Dictionary<Guid, BookImage> ImageCandidates { get; set; } = [];

    public HashSet<Guid> IncludedImages { get; set; } = [];

    public List<SlotTextStyle> TextStyles { get; set; } = [];

    [JsonIgnore]
    public ILogger? MaybeLogger { get; set; }

    [JsonIgnore]
    // ! Ok
    public ILogger Logger => this.MaybeLogger!;

    public int PageIndexFromPage(Page targetPage)
    {
        int pageIndex = -1;
        int toAdd = 0; 
        if (this.WithFrontCover)
        {
            if ( targetPage == this.FrontCover!.FrontPage)
            {
                return 0; 
            }
            else if (targetPage == this.FrontCover!.BackPage)
            {
                return 1;
            }
            else
            {
                toAdd = 2; 
            }
        }

        if (this.WithBackCover)
        {
            if (targetPage == this.BackCover!.FrontPage)
            {
                return toAdd + this.Pages.Count;
            }
            else if (targetPage == this.BackCover!.BackPage)
            {
                return 1 + toAdd + this.Pages.Count;
            }
        }

        for (int i = 0; i < this.Pages.Count; ++i)
        {
            var page = this.Pages[i];
            if (page == targetPage)
            {
                pageIndex = i;
                break;
            }
        }

        return pageIndex + toAdd;
    }

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
    {
        if (this.Pages is null || this.Pages.Count == 0)
        {
            return null;
        }

        if ((pageNumber < 0) || (pageNumber >= this.Pages.Count))
        {
            return null;
        }

        return this.Pages[pageNumber];
    }

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

    public float PageWidth => this.PageSize.Width;

    public float PageHeight => this.PageSize.Height;

    public void Initialize()
    {
        if (this.WithFrontCover)
        {
            this.FrontCover =
                new FrontCover()
                {
                    FrontPage = new Page(this),
                    BackPage = new Page(this)
                };
        }

        this.Pages.Add(new Page(this));
        this.Pages.Add(new Page(this));

        if (this.WithBackCover)
        {
            this.BackCover =
                new BackCover()
                {
                    FrontPage = new Page(this),
                    BackPage = new Page(this)
                };
        }
    }

    public Guid ImportImage(string booksPath, string imagePath, string thumbailPath, out string message)
    {
        message = string.Empty;

        if (!File.Exists(imagePath))
        {
            message = "Workflow.Book.NotExist";
            return Guid.Empty;
        }

        if (!ImageLoader.HasImageSharpExtension(imagePath))
        {
            message = "Workflow.Book.NotSupported";
            return Guid.Empty;
        }

        if (!ImageLoader.HasJpgExtension(imagePath))
        {
            message = "Workflow.Book.NotJpg";
            return Guid.Empty;
        }

        try
        {
            // Step 0 - Prepare file paths 
            var bookImage = new BookImage();
            string imageBaseFileName = bookImage.Id.ToShortString();
            string highResImageFileName = string.Concat(HighResPrefix, imageBaseFileName, JpgExtension);
            string mediumResImageFileName = string.Concat(MediumResPrefix, imageBaseFileName, JpgExtension);
            string lowResImageFileName = string.Concat(LowResPrefix, imageBaseFileName, JpgExtension);
            string folderPath = System.IO.Path.Combine(booksPath, this.FolderName);

            string highResImageFilePath = System.IO.Path.Combine(folderPath, highResImageFileName);
            string mediumResImageFilePath = System.IO.Path.Combine(folderPath, mediumResImageFileName);
            string lowResImageFilePath = System.IO.Path.Combine(folderPath, lowResImageFileName);

            // Step 1 - Load the image file into memory 
            var imageFormat = SixLabors.ImageSharp.Image.DetectFormat(imagePath);
            if (imageFormat is null)
            {
                // errorMessage = "Unsupported image format in ImageSharp.";
                message = "Model.Loader.ImageSharpNotDetected";
                return Guid.Empty;
            }

            Debug.WriteLine(imageFormat.Name);

            var image = SixLabors.ImageSharp.Image.Load<Rgb24>(imagePath);
            if (image is null)
            {
                // errorMessage = "Failed to load the source image with ImageSharp.";
                message = "Model.Loader.ImageSharpFailedLoad";
                return Guid.Empty;
            }

            Debug.WriteLine("Image loaded with ImageSharp: " + imagePath);

            void EncodeAndSave(SixLabors.ImageSharp.Image image, string targetPath, bool isHd)
            {
                var saveMemoryStream = new MemoryStream();
                int quality = isHd ? ImageLoader.HdImageQuality : ImageLoader.ThumbnailQuality;
                image.SaveAsJpeg(saveMemoryStream, new JpegEncoder() { Quality = quality });
                byte[] jpgEncoded = saveMemoryStream.ToArray();
                File.WriteAllBytes(targetPath, jpgEncoded);
            }

            // Step 2 - Copy original 
            if (ImageLoader.HasJpgExtension(imagePath))
            {
                // Original is JPG : just copy
                File.Copy(imagePath, highResImageFilePath);
            }
            else
            {
                // Original is NOT JPG : Reencode and save 
                EncodeAndSave(image, highResImageFilePath, isHd: true);
            }

            bookImage.HighResPath = highResImageFilePath;

            void Clone(string targetPath, bool isHd)
            {
                // Create with cloning 
                var clone = image.Clone();
                clone.Mutate(x => x.Resize(
                    new ResizeOptions
                    {
                        Size = ImageLoader.ThumbnailSize(image.Width, image.Height, isHd),
                        Mode = ResizeMode.Max, // Constrains dimensions while keeping aspect ratio
                        Sampler = KnownResamplers.Lanczos3 // High quality downsampling filter
                    }));

                EncodeAndSave(clone, targetPath, isHd);
            }

            // Step 3 - create a medium size for UI purposes  
            int maxDimension = Math.Max(image.Height, image.Width);
            if (maxDimension > 2000)
            {
                Clone(mediumResImageFilePath, isHd: true);
                bookImage.MediumResPath = mediumResImageFilePath;
            }
            else
            {
                // already medium size: will reuse the best high res image 
                // no need to resize
                bookImage.MediumResPath = highResImageFilePath;
            }

            // Step 4 - Create thumbnail if needed, or just copy provided 
            if (string.IsNullOrWhiteSpace(thumbailPath))
            {
                Clone(lowResImageFilePath, isHd: false);
            }
            else
            {
                // Provided: just copy
                File.Copy(thumbailPath, lowResImageFilePath);
            }

            bookImage.LowResPath = lowResImageFilePath;

            // Step 5 - Now a candidate image 
            this.ImageCandidates.Add(bookImage.Id, bookImage);
            return bookImage.Id;
        }
        catch (Exception ex)
        {
            string msg = "Exception thrown when importing image. \n" + ex.ToString();
            Debug.WriteLine(msg);
            this.Logger.Error(msg);
            return Guid.Empty;
        }
    }

    internal void IncludeImage(BookImage bookImage) => this.IncludeImage(bookImage.Id);

    internal void IncludeImage(Guid key)
    {
        if (this.ImageCandidates.TryGetValue(key, out BookImage? bookImage) && bookImage is not null)
        {
            this.IncludedImages.Add(key);
        }
    }

    public string GeneratePdf(BookResolution bookResolution, Stream? outputStream = null)
    {
        var start = DateTime.Now;

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
                        // Need to crop the source image to match the page aspect ratio
                        byte[]? imageBytes = imageSlot.Image(this, bookResolution);
                        if (imageBytes is null)
                        {
                            continue;
                        }

                        var imageDescriptor = descriptor.Image(imageBytes);
                        if (imageSlot.ImageStretch == BookImageStretch.Stretch)
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

        Document document = Document.Create(container =>
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
        });

        string toReturn;
        switch (bookResolution)
        {
            default:
            case BookResolution.Print:
                document.GeneratePdf(this.OutputPdfFileName);
                toReturn = this.OutputPdfFileName;
                break;

            case BookResolution.Display:
                // string file = string.Concat("LowRes_", this.OutputPdfFileName);
                document.GeneratePdf(this.OutputPdfFileName);
                // toReturn = file;
                toReturn = this.OutputPdfFileName;
                break;

            case BookResolution.Thumbnails:
                if (outputStream is not null)
                {
                    document.GeneratePdf(outputStream);
                }

                toReturn = string.Empty;
                break;
        }

        DateTime done = DateTime.Now;
        TimeSpan span = done - start;
        string msg = " Document created: " + span.TotalMilliseconds.ToString("F1") + " ms";
        Debug.WriteLine(msg);
        this.Logger.Info(msg);

        return toReturn;
    }
}
