namespace Lyt.PhotoPostPro.Workflow.Documentation;

using global::Avalonia.Media.Imaging;

using Lyt.Resources;

public sealed record class PdfPageInViewMessage(int PageNumber, int PageCount);

public sealed record class PdfPageLoadedMessage(PdfPage PdfPage);

public sealed record class PdfLoadedStatusMessage(bool Complete, Exception? Exception = null);

public sealed record class PdfPage(int PageNumber, Bitmap Page, Bitmap Thumbnail);

//[SupportedOSPlatform("Windows")]
public static class PdfLoader
{
    public static void Unload()
        => new PdfLoadedStatusMessage(Complete: false).Publish();

    public static bool LoadFirstPage(Document document, string language = "", string? pdfPassword = null)
    {
        MemoryStream memoryStream = LoadDocumentFile(document, language);
        var bitmap = LoadFirstPage(memoryStream, pdfPassword);
        document.FirstPage = bitmap; 
        return bitmap is not null;
    }

    public static void BeginLoadDocument(Document document, string language = "", string? pdfPassword = null)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            language = "en-US";
        }

        Task.Run(() =>
        {
            MemoryStream memoryStream = LoadDocumentFile(document, language);
            LoadPages(memoryStream, pdfPassword);
        });
    }

    private static MemoryStream LoadDocumentFile(Document document, string language)
    {
        ResourcesUtilities.SetExecutingAssembly(Assembly.GetExecutingAssembly());
        ResourcesUtilities.SetResourcesPath("Lyt.PhotoPostPro");
        string docPath = document.ResourcePath; //  string.Format("Doc_{0}.pdf", language);
        byte[] pdfBytes = ResourcesUtilities.LoadEmbeddedBinaryResource(docPath, out string? resourceName);
        return new MemoryStream(pdfBytes);
    }

#pragma warning disable CA1416 
    // Validate platform compatibility
    // For : Conversion.ToImagesAsync  , Conversion.ToImage 

    private static Bitmap? LoadFirstPage(Stream pdfStream, string? pdfPassword = null)
    {
        try
        {
            // Single image load is synchronous 
            PDFtoImage.RenderOptions renderOptions = new();
            var skiaBitmap =
                Conversion.ToImage(pdfStream, page: 0, leaveOpen: false, password: pdfPassword, renderOptions);
            
            // Create an Avalonia bitmap usable as a source for an image control and scale it down
            var bitmapPage = ToAvaloniaBitmap(skiaBitmap);
            var bitmapThumbnail = CreateThumbnail(bitmapPage, targetWidth: 1200, targetHeight: 1200);
            Debug.WriteLine(" Loaded document first page ");
            return bitmapThumbnail;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return null;
        }
    }

    private static async void LoadPages(Stream pdfStream, string? pdfPassword = null)
    {
        try
        {
            PDFtoImage.RenderOptions renderOptions = new();
            int pageNumber = 0;
            var skiaBitmaps = Conversion.ToImagesAsync(pdfStream, leaveOpen: false, password: pdfPassword, renderOptions);
            await foreach (var skiaBitmap in skiaBitmaps)
            {
                // DONT: This is the way mentioned on the PDFtoImage github page 
                // But this is very slow ad inefficient
                //
                //// Encode SKBitmap to PNG format into the stream
                //using var memoryStream = new MemoryStream();
                //skiaBitmap.Encode(memoryStream, SKEncodedImageFormat.Png, 100);
                //memoryStream.Seek(0, SeekOrigin.Begin);
                //
                //// Create an Avalonia Bitmap from the stream 
                //var bitmapPage = new Bitmap(memoryStream);

                // Create an Avalonia bitmap usable as a source for an image control and scale it down
                var bitmapPage = ToAvaloniaBitmap(skiaBitmap);
                var bitmapThumbnail = CreateThumbnail(bitmapPage);
                ++pageNumber;
                var page = new PdfPage(pageNumber, bitmapPage, bitmapThumbnail);

                Debug.WriteLine(" Loaded documentation page " + pageNumber.ToString());
                new PdfPageLoadedMessage(page).Publish();
            }

            new PdfLoadedStatusMessage(Complete: true).Publish();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            new PdfLoadedStatusMessage(Complete: false, Exception: ex).Publish();
        }
    }

#pragma warning restore CA1416 // Validate platform compatibility

    public static Bitmap ToAvaloniaBitmap(SKBitmap skBitmap)
    {
        // Create an Avalonia WriteableBitmap matching the exact dimensions
        var pixelSize = new PixelSize(skBitmap.Width, skBitmap.Height);

        // Match Avalonia's native screen DPI (typically 96)
        var dpi = new Vector(96, 96);

        // Ensure the color type and alpha aligns with Avalonia expectations
        PixelFormat pixelFormat =
            skBitmap.ColorType == SKColorType.Bgra8888 ? PixelFormat.Bgra8888 : PixelFormat.Rgba8888;
        AlphaFormat alphaFormat =
            skBitmap.AlphaType == SKAlphaType.Premul ? AlphaFormat.Premul : AlphaFormat.Opaque;

        return new Bitmap(
            pixelFormat, alphaFormat, data: skBitmap.GetPixels(),
            pixelSize, dpi, stride: skBitmap.RowBytes);
    }

    public static Bitmap CreateThumbnail(Bitmap originalBitmap, int targetWidth = 400, int targetHeight = 420)
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