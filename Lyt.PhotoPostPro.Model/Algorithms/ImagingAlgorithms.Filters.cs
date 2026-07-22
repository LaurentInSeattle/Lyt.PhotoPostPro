namespace Lyt.PhotoPostPro.Model.Algorithms;

public static partial class ImagingAlgorithms
{
    public static bool Grayscale(this Image<HalfVector4> image, float grayscaleAmount)
    {
        if (Math.Abs(grayscaleAmount) > 0.01)
        {
            // Always use the BT.709 standard for grayscale conversion, as it is the most accurate
            // for human perception and best for high definition images.
            image.Mutate(x => x.Grayscale(GrayscaleMode.Bt709, grayscaleAmount));
        }

        return true;
    }

    public static bool Sepia(this Image<HalfVector4> image, float sepiaAmount)
    {
        if (Math.Abs(sepiaAmount) > 0.01)
        {
            image.Mutate(x => x.Sepia(sepiaAmount));
        }

        return true;
    }

    public static bool Pixelate(this Image<HalfVector4> image, float pixelationAmount)
    {
        int amount = (int) ( 0.5f + 100.0f * pixelationAmount);
        if ( amount > 0)
        {
            image.Mutate(x => x.Pixelate(amount));
        }

        return true;
    }

    public static bool Lomograph(this Image<HalfVector4> image)
    {
        image.Mutate(x => x.Lomograph());
        return true;
    }

    public static bool Kodachrome(this Image<HalfVector4> image)
    {
        image.Mutate(x => x.Kodachrome());
        return true;
    }

    public static bool Polaroid(this Image<HalfVector4> image)
    {
        image.Mutate(x => x.Polaroid());
        return true;
    }

    public static bool BlackWhite(this Image<HalfVector4> image)
    {
        image.Mutate(x => x.BlackWhite());
        return true;
    }

    public static bool Vignette(this Image<HalfVector4> image)
    {
        image.Mutate(x => x.Vignette(Color.Black));
        return true;
    }
}
