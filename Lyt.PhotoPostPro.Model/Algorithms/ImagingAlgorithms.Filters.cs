namespace Lyt.PhotoPostPro.Model.Algorithms;

using static ImagingUtilities;

internal static partial class ImagingAlgorithms
{
    internal static bool Grayscale(this Image<RgbaHalf> image, float grayscaleAmount)
    {
        if (Math.Abs(grayscaleAmount) > 0.01)
        {
            // Always use the BT.709 standard for grayscale conversion, as it is the most accurate
            // for human perception and best for high definition images.
            image.Mutate(x => x.Grayscale(GrayscaleMode.Bt709, grayscaleAmount));
        }

        return true;
    }

    internal static bool Sepia(this Image<RgbaHalf> image, float sepiaAmount)
    {
        if (Math.Abs(sepiaAmount) > 0.01)
        {
            image.Mutate(x => x.Sepia(sepiaAmount));
        }

        return true;
    }

    internal static bool BlackWhite(this Image<RgbaHalf> image)
    {
        image.Mutate(x => x.BlackWhite());
        return true;
    }

    internal static bool Vignette(this Image<RgbaHalf> image, float vignetteAmount)
    {
        vignetteAmount /= 2.0f;
        var color = Color.ParseHex("#D8000000", ColorHexFormat.Argb);
        float amount = (1.0f - vignetteAmount);
        float radiusX = image.Width * amount / 1.8f;
        float radiusY = image.Height * amount / 1.8f;
        image.Mutate(x => x.Vignette(
            color, radiusX, radiusY, rectangle: new Rectangle(0, 0, image.Width, image.Height)));
        return true;
    }

    internal static bool Pixelate(this Image<RgbaHalf> image, float pixelationAmount)
    {
        int amount = (int)(0.5f + 100.0f * pixelationAmount);
        if (amount > 0)
        {
            image.Mutate(x => x.Pixelate(amount));
        }

        return true;
    }

    internal static bool Lomograph(this Image<RgbaHalf> image)
    {
        image.Mutate(x => x.Lomograph());
        return true;
    }

    internal static bool Kodachrome(this Image<RgbaHalf> image)
    {
        image.Mutate(x => x.Kodachrome());
        return true;
    }

    internal static bool Polaroid(this Image<RgbaHalf> image)
    {
        image.Mutate(x => x.Polaroid());
        return true;
    }

    internal static bool HueRotation(this Image<RgbaHalf> image, float rotation)
    {
        // hue rotation should be in [0, 1]

        int height = image.Height;
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast, safe access
            Span<RgbaHalf> pixelRow = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < pixelRow.Length; x++)
            {
                var pixel = pixelRow[x];
                float r = (float)pixel.R;
                float g = (float)pixel.G;
                float b = (float)pixel.B;
                ColorUtilities.RgbToHsl(r, g, b, out float hue, out float saturation, out float lightness);

                // Convert back to RGB and update the pixel
                hue += rotation;
                ColorUtilities.HslToRgb(hue, saturation, lightness, out float tr, out float tg, out float tb);
                pixelRow[x].R = ClipH((Half)tr);
                pixelRow[x].G = ClipH((Half)tg);
                pixelRow[x].B = ClipH((Half)tb);
            }
        });

        return true;
    }
}
