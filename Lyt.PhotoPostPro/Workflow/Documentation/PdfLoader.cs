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
    {

    }

    public static void BeginLoadDocumentation(string language = "", string? pdfPassword = null)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            language = "en-US";
        }

        Task.Run(() =>
        {
            MemoryStream memoryStream = LoadDocumentationFile(language);
            LoadPages(memoryStream, pdfPassword);
        });
    }

    private static MemoryStream LoadDocumentationFile(string language)
    {
        ResourcesUtilities.SetExecutingAssembly(Assembly.GetExecutingAssembly());
        ResourcesUtilities.SetResourcesPath("Lyt.PhotoPostPro");
        string docPath = string.Format("Doc_{0}.pdf", language);
        byte[] pdfBytes = ResourcesUtilities.LoadEmbeddedBinaryResource(docPath, out string? resourceName);
        return new MemoryStream(pdfBytes);
    }

#pragma warning disable CA1416 
    // Validate platform compatibility
    // For : Conversion.ToImagesAsync  

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

    public static Bitmap CreateThumbnail(Bitmap originalBitmap, int targetWidth = 320, int targetHeight = 360)
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