namespace Lyt.PhotoPostPro.Workflow.Documentation;

using global::Avalonia.Media.Imaging;

public sealed record class PdfPage(int PageNumber, Bitmap Page, Bitmap Thumbnail);

public sealed record class PdfPages(List<PdfPage> Pages, Exception? Exception = null); 

[SupportedOSPlatform("Windows")]
public static class PdfLoader
{
    public static async Task<PdfPages> Load(Stream pdfStream, string? pdfPassword = null)
    {        
        List<PdfPage> pages = [];
        try
        {
            PDFtoImage.RenderOptions renderOptions = new();
            int pageNumber = 0;
            var skiaBitmaps = Conversion.ToImagesAsync(pdfStream, leaveOpen: false, password: pdfPassword, renderOptions);
            await foreach (var skiaBitmap in skiaBitmaps)
            {
                // Create an Avalonia bitmap usable as a source for an image control 
                using var memoryStream = new MemoryStream();

                // Encode SKBitmap to PNG format into the stream
                skiaBitmap.Encode(memoryStream, SKEncodedImageFormat.Png, 100);
                memoryStream.Seek(0, SeekOrigin.Begin);

                // Create an Avalonia Bitmap from the stream and scale it down 
                var bitmapPage = new Bitmap(memoryStream);
                var bitmapThumbnail = CreateThumbnail(bitmapPage);
                ++pageNumber;
                pages.Add(new PdfPage(pageNumber, bitmapPage, bitmapThumbnail)); 
            }

            return new PdfPages(pages);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return new PdfPages(pages, ex);
        }
    }

    public static Bitmap CreateThumbnail(Bitmap originalBitmap, int targetWidth = 240, int targetHeight = 360)
    {
        // Calculate scale factor while maintaining the aspect ratio
        double scale = Math.Min(
            targetWidth / (double)originalBitmap.Size.Width,
            targetHeight / (double)originalBitmap.Size.Height);
        int scaledWidth = (int)(originalBitmap.Size.Width * scale);
        int scaledHeight = (int)(originalBitmap.Size.Height * scale);

        // Create the scaled thumbnail bitmap
        return originalBitmap.CreateScaledBitmap(
            new PixelSize(scaledWidth, scaledHeight), BitmapInterpolationMode.HighQuality);
    }
} 