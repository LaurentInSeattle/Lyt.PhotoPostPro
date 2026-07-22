namespace Lyt.PhotoPostPro.Model.Utilities;

public sealed class Histograms
{
    public Histogram Red { get; private set; }
    
    public Histogram Green { get; private set; }
    
    public Histogram Blue { get; private set; }

    public Histogram Luminosity { get; private set; }

    public Histograms(Image<RgbaVector> image)
    {
        // Initialize histograms for 256 possible intensity levels (0-255)
        // No point to display 65356 points 
        float[] redHistogram = new float[256];
        float[] greenHistogram = new float[256];
        float[] blueHistogram = new float[256];
        float[] grayHistogram = new float[256];

        int pixelCount = 0;

        // Parallelize the loop over the rows
        int height = image.Height;
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast, safe access
            Span<RgbaVector> row = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < row.Length; x++)
            {
                ++pixelCount;

                // Increment the bin for each color channel
                Rgba32 pixel = row[x].ToRgba32();
                redHistogram[pixel.R]++;
                greenHistogram[pixel.G]++;
                blueHistogram[pixel.B]++;

                // Calculate human perceived luminosity and increment the gray bin
                // Calculate perceived luminance: Simplified formula, linear  
                int gray1000 = 299 * pixel.R + 587 * pixel.G + 114 * pixel.B;
                int gray = (gray1000 + 500) / 1000;
                if (gray == 256)
                {
                    // Possible because of rounding 
                    gray = 255;
                }

                if (gray > 255)
                {
                    if (Debugger.IsAttached) { Debugger.Break(); }
                }

                grayHistogram[gray]++;
            }
        });

        // Analyze the histograms 
        // To work around clipping issues causing bad averaging we run finding maxes
        // in the mid-tones, starting at 12, ending at 244
        const int clipOffset = 12;
        float maxRed = 0;
        float maxGreen = 0;
        float maxBlue = 0;
        float maxGray = 0;
        for (int i = clipOffset; i < 256 - clipOffset; ++i)
        {
            float currentRed = redHistogram[i];
            if (currentRed > maxRed)
            {
                maxRed = currentRed;
            }

            float currentGreen = greenHistogram[i];
            if (currentGreen > maxGreen)
            {
                maxGreen = currentGreen;
            }

            float currentBlue = blueHistogram[i];
            if (currentBlue > maxBlue)
            {
                maxBlue = currentBlue;
            }

            float currentGray = grayHistogram[i];
            if (currentGray > maxGray)
            {
                maxGray = currentGray;
            }
        }

        float avgRed = 0;
        float avgGreen = 0;
        float avgBlue = 0;
        float avgGray = 0;
        for (int i = 0; i < 256; ++i)
        {
            float currentRed = redHistogram[i];
            avgRed += currentRed;
            float currentGreen = greenHistogram[i];
            avgGreen += currentGreen;
            float currentBlue = blueHistogram[i];
            avgBlue += currentBlue;
            float currentGray = grayHistogram[i];
            avgGray += currentGray;
        }

        avgRed /= 256;
        avgGreen /= 256;
        avgBlue /= 256;
        avgGray /= 256;

        float minAvgColor = Math.Min(avgRed, Math.Min(avgBlue, avgGreen));
        float maxColor = Math.Max(maxRed, Math.Max(maxBlue, maxGreen)) + minAvgColor;
        maxGray += avgGray;

        bool isClippingLowRed = false;
        bool isClippingLowGreen = false;
        bool isClippingLowBlue = false;
        bool isClippingLowGray = false;

        for (int i = 0; i < clipOffset; ++i)
        {
            if (redHistogram[i] > maxRed)
            {
                isClippingLowRed = true;
            }

            if (greenHistogram[i] > maxGreen)
            {
                isClippingLowGreen = true;
            }

            if (blueHistogram[i] > maxBlue)
            {
                isClippingLowBlue = true;
            }

            if (grayHistogram[i] > maxGray)
            {
                isClippingLowGray = true;
            }
        }

        bool isClippingHighRed = false;
        bool isClippingHighGreen = false;
        bool isClippingHighBlue = false;
        bool isClippingHighGray = false;

        for (int i = 256 - clipOffset; i < 256; ++i)
        {
            if (redHistogram[i] > maxRed)
            {
                isClippingHighRed = true;
            }

            if (greenHistogram[i] > maxGreen)
            {
                isClippingHighGreen = true;
            }

            if (blueHistogram[i] > maxBlue)
            {
                isClippingHighBlue = true;
            }

            if (grayHistogram[i] > maxGray)
            {
                isClippingHighGray = true;
            }
        }

        // Normalize the histograms 
        for (int i = 0; i < 256; ++i)
        {
            redHistogram[i] /= maxColor;
            greenHistogram[i] /= maxColor;
            blueHistogram[i] /= maxColor;
            grayHistogram[i] /= maxGray;
        }

        this.Red = new() { Bins = redHistogram, IsClippingLow = isClippingLowRed, IsClippingHigh = isClippingHighRed };
        this.Green = new() { Bins = greenHistogram, IsClippingLow = isClippingLowGreen, IsClippingHigh = isClippingHighGreen };
        this.Blue = new() { Bins = blueHistogram, IsClippingLow = isClippingLowBlue, IsClippingHigh = isClippingHighBlue };
        this.Luminosity = new() { Bins = grayHistogram, IsClippingLow = isClippingLowGray, IsClippingHigh = isClippingHighGray };
    }
}
