namespace Lyt.PhotoPostPro.Model.PhotoBook.Slots;

public sealed class ImageSlot : Slot
{
    public Guid BookImageId { get; set; } = Guid.Empty;

    public int ImageWidth { get; set; }

    public int ImageHeight { get; set; }

    public BookImageStretch ImageStretch { get; set; } = BookImageStretch.Fit;

    public float AspectRatio => (float)this.ImageWidth / (float)this.ImageHeight;

    public string ImagePath(Book book, BookResolution bookResolution)
    {
        try
        {
            if (!book.IncludedImages.Contains(this.BookImageId))
            {
                return string.Empty;
            }

            if (!book.ImageCandidates.TryGetValue(this.BookImageId, out BookImage? bookImage) &&
                bookImage is null)
            {
                return string.Empty;
            }

            string imageFileName = bookResolution switch
            {
                BookResolution.Print => bookImage.HighResPath,
                BookResolution.Thumbnails => bookImage.LowResPath,
                _ => bookImage.MediumResPath,
            };

            if (string.IsNullOrWhiteSpace(imageFileName))
            {
                // Skip this slot if the image file is not known yet 
                Debug.WriteLine($"No image file for slot: {this.Id}");
                return string.Empty;
            }

            if (!File.Exists(imageFileName))
            {
                // Skip this slot if the image file does not exist
                Debug.WriteLine($"Image file not found: {imageFileName}");
                return string.Empty;
            }

            return imageFileName;
        }
        catch (Exception ex)
        {
            string msg = "Exception thrown when loading image. \n" + ex.ToString();
            Debug.WriteLine(msg);
            book.Logger.Error(msg);
            return string.Empty;
        }
    }

    public byte[]? Image(Book book, BookResolution bookResolution)
    {
        try
        {
            if (bookResolution == BookResolution.Thumbnails)
            {
                // ! We have it 
                if (book.BookManager!.CurrentBookThumbnails.TryGetValue(this.BookImageId, out byte[]? cachedImageBytes))
                {
                    if (cachedImageBytes is not null)
                    {
                        return cachedImageBytes;
                    }
                }
            }

            // May need to crop the source image to match the page aspect ratio
            string imageFileName = this.ImagePath(book, bookResolution);
            byte[] imageBytes = File.ReadAllBytes(imageFileName);
            return imageBytes;
        }
        catch (Exception ex)
        {
            string msg = "Exception thrown when loading image. \n" + ex.ToString();
            Debug.WriteLine(msg);
            book.Logger.Error(msg);
            return null;
        }
    }
}