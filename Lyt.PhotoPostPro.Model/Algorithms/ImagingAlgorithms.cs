namespace Lyt.PhotoPostPro.Model.Algorithms;

using static ImagingUtilities;

public static class ImagingAlgorithms
{
    /// <summary> Creates a 64K-byte Look-Up Table for fast gamma correction. </summary>
    /// <param name="gamma">Gamma value (e.g., 2.2 to brighten midtones, 0.45 to darken).</param>
    public static ushort[] CreateGammaLUT(double gamma)
    {
        // Prevent potential division by zero
        if (gamma <= 0)
        {
            gamma = 1.0;
        }

        double inverseGamma = 1.0 / gamma;
        ushort[] lut = new ushort[65536];
        Parallel.For(0, 65536, i =>
        {
            // Normalize to 0.0 - 1.0  and apply power curve
            double normalized = i / 65536.0;
            double corrected = Math.Pow(normalized, inverseGamma);

            // Scale back to 0 - 65536 and round safely
            lut[i] = Clip16(Math.Round(corrected * 65536.0));
        });

        return lut;
    }

    public static ushort[] Gamma(this Image<Rgb48> image, double gamma, double gain, int shift)
    {
        // Will return the LUT for use in the UI 
        ushort[] lut = ImagingAlgorithms.CreateGammaLUT(gamma);

        // Parallelize the loop over the rows
        int height = image.Height;
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast, safe access
            Span<Rgb48> row = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < row.Length; x++)
            {
                // Custom pixel manipulation
                Rgb48 pixel = row[x];
                row[x].R = Clip16(gain * (lut[pixel.R] + shift));
                row[x].G = Clip16(gain * (lut[pixel.G] + shift));
                row[x].B = Clip16(gain * (lut[pixel.B] + shift));
            }
        });

        return lut;
    }

    //  Implementing a highlights and shadows adjustment requires extracting the pixel data and running 
    //  a non-linear luminance alteration formula. it calculates the perceived luminance of each pixel, 
    //  then maps highlights and shadows independently using a power curve(gamma mapping).
    // 
    //  The formula converts the RGB values to grayscale luminance, and then applies separate adjustment variables 
    //  h (for highlights) and s (for shadows),  constrained between -1.0 and +1.0.
    //  Luminance is calculated using the standard perceived brightness coefficients. The shift factors h and s are then 
    //  calculated adaptively.
    public static void HighlightsShadows(this Image<Rgb48> image, float highlightAmount, float shadowAmount)
    {
        // Parallelize the loop over the rows
        int height = image.Height;
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast, safe access
            Span<Rgb48> row = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < row.Length; x++)
            {
                Rgb48 pixel = row[x];

                // Normalize RGB to [0, 1] range
                float r = pixel.R / 65535.0f;
                float g = pixel.G / 65535.0f;
                float b = pixel.B / 65535.0f;

                // Calculate perceived luminance
                float luminance = (float)Math.Sqrt(0.299 * (r * r) + 0.587 * (g * g) + 0.114 * (b * b));

                // Compute adjustment factors
                float h = highlightAmount * 0.05f * ((float)Math.Pow(8.0, luminance) - 1.0f);
                float s = shadowAmount * 0.05f * ((float)Math.Pow(8.0, 1.0f - luminance) - 1.0f);
                float hs = h + s;

                // Apply shifts, denormalize, clip and update pixel
                row[x].R = DeNormalizeClip16(r + hs);
                row[x].G = DeNormalizeClip16(g + hs);
                row[x].B = DeNormalizeClip16(b + hs);
            }
        });
    }

    // By setting the saturationThreshold to 0.4, any pixel that is more than 40 % saturated gets skipped. 
    // The algorithm now looks at the neutral sidewalks, stones, gray tree trunks, or white clothing in the photo
    // to find the true color cast.
    public static bool FilteredGrayWorldAWB(this Image<Rgb48> image, float saturationThreshold = 0.4f)
    {
        long totalR = 0, totalG = 0, totalB = 0;
        long validPixelCount = 0;

        // Sum up only the low-saturation pixels
        int height = image.Height;
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast, safe access
            Span<Rgb48> pixelRow = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < pixelRow.Length; x++)
            {
                Rgb48 pixel = pixelRow[x];
                ushort r = pixel.R;
                ushort g = pixel.G;
                ushort b = pixel.B;

                // Calculate saturation (scaled from 0.0 to 1.0)
                ushort max = Math.Max(r, Math.Max(g, b));
                ushort min = Math.Min(r, Math.Min(g, b));
                float saturation = (max == 0) ? 0f : (float)(max - min) / max;

                // Only count the pixel if it is below our color intensity limit
                if (saturation <= saturationThreshold)
                {
                    totalR += r;
                    totalG += g;
                    totalB += b;
                    validPixelCount++;
                }
            }
        });

        // The whole image is hyper-saturated
        if (validPixelCount == 0)
        {
            // re-run the algorithm with a different threshold 
            return false;
        }

        // Calculate averages from the filtered pool
        double avgR = (double)totalR / validPixelCount;
        double avgG = (double)totalG / validPixelCount;
        double avgB = (double)totalB / validPixelCount;

        // Prevent zero divides for calculating gains 
        if (avgR == 0)
        {
            avgR = 1;
        }

        if (avgG == 0)
        {
            avgG = 1;
        }

        if (avgB == 0)
        {
            avgB = 1;
        }

        // Find the target gray value and coefficients
        double targetGray = (avgR + avgG + avgB) / 3.0;
        float rGain = (float)(targetGray / avgR);
        float gGain = (float)(targetGray / avgG);
        float bGain = (float)(targetGray / avgB);

        // Apply the gains to EVERY pixel in the image
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast, safe access
            Span<Rgb48> pixelRow = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < pixelRow.Length; x++)
            {
                Rgb48 pixel = pixelRow[x];
                float r = pixel.R * rGain;
                float g = pixel.G * gGain;
                float b = pixel.B * bGain;
                pixelRow[x].R = Clip16(r);
                pixelRow[x].G = Clip16(g);
                pixelRow[x].B = Clip16(b);
            }
        });

        return true; 
    }
}