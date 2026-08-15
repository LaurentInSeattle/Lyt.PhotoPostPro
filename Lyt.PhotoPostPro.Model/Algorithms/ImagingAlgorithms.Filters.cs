namespace Lyt.PhotoPostPro.Model.Algorithms;

public static partial class ImagingAlgorithms
{
    public static bool Grayscale(this Image<RgbaHalf> image, float grayscaleAmount)
    {
        if (Math.Abs(grayscaleAmount) > 0.01)
        {
            // Always use the BT.709 standard for grayscale conversion, as it is the most accurate
            // for human perception and best for high definition images.
            image.Mutate(x => x.Grayscale(GrayscaleMode.Bt709, grayscaleAmount));
        }

        return true;
    }

    public static bool Sepia(this Image<RgbaHalf> image, float sepiaAmount)
    {
        if (Math.Abs(sepiaAmount) > 0.01)
        {
            image.Mutate(x => x.Sepia(sepiaAmount));
        }

        return true;
    }

    public static bool BlackWhite(this Image<RgbaHalf> image)
    {
        image.Mutate(x => x.BlackWhite());
        return true;
    }

    public static bool Vignette(this Image<RgbaHalf> image, float vignetteAmount)
    {
        var color = Color.ParseHex("#A8202020", ColorHexFormat.Argb);
        float amount = (1.0f - vignetteAmount); 
        float radiusX = image.Width *  amount / 2.0f;
        float radiusY = image.Height * amount / 2.0f;
        image.Mutate(x => x.Vignette(
            color, radiusX, radiusY, rectangle: new Rectangle(0, 0, image.Width, image.Height)));
        return true;
    }

    public static bool Pixelate(this Image<RgbaHalf> image, float pixelationAmount)
    {
        int amount = (int)(0.5f + 100.0f * pixelationAmount);
        if (amount > 0)
        {
            image.Mutate(x => x.Pixelate(amount));
        }

        return true;
    }

    public static bool Lomograph(this Image<RgbaHalf> image)
    {
        image.Mutate(x => x.Lomograph());
        return true;
    }

    public static bool Kodachrome(this Image<RgbaHalf> image)
    {
        image.Mutate(x => x.Kodachrome());
        return true;
    }

    public static bool Polaroid(this Image<RgbaHalf> image)
    {
        image.Mutate(x => x.Polaroid());
        return true;
    }
}
