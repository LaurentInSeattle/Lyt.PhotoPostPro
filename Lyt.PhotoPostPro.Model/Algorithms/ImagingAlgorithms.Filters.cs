namespace Lyt.PhotoPostPro.Model.Algorithms;

public static partial class ImagingAlgorithms
{
    public static bool Grayscale(this Image<Rgb48> image, float grayscaleAmount)
    {
        if (Math.Abs(grayscaleAmount) > 0.01)
        {
            // Always use the BT.709 standard for grayscale conversion, as it is the most accurate
            // for human perception and best for high definition images.
            image.Mutate(x => x.Grayscale(GrayscaleMode.Bt709, grayscaleAmount));
        }

        return true;
    }

    public static bool Sepia(this Image<Rgb48> image, float sepiaAmount)
    {
        if (Math.Abs(sepiaAmount) > 0.01)
        {
            image.Mutate(x => x.Sepia(sepiaAmount));
        }

        return true;
    }
}
