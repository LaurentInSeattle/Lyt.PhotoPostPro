namespace Lyt.PhotoPostPro.Model.Algorithms;

using static ImagingUtilities;
using static System.MathF;

public static partial class ImagingAlgorithms
{
    #region Brightness / Gamma 

    public const int LutSize = 1024;

    /// <summary> Creates a Look-Up Table for fast gamma correction. </summary>
    /// <param name="gamma">Gamma value (e.g., 2.2 to brighten midtones, 0.45 to darken).</param>
    public static float[] CreateGammaLUT(float gamma)
    {
        // Prevent potential division by zero
        if (gamma <= 0)
        {
            gamma = 1.0f;
        }

        float inverseGamma = 1.0f / gamma;
        float[] lut = new float[LutSize];
        Parallel.For(0, LutSize, i =>
        {
            // Normalize to 0.0 - 1.0  and apply power curve
            float normalized = (float)i / (LutSize - 1);
            float corrected = MathF.Pow(normalized, inverseGamma);

            // Scale back to 0 - 1 and round safely
            lut[i] = ClipF(corrected);
        });

        return lut;
    }

    public static float LutLookup(float[] lut, float value)
    {
        int low = (int)Math.Floor(value * LutSize);
        int high = low + 1;
        if ((low < 0) || (high >= LutSize))
        {
            return value;
        }

        float vLow = lut[low];
        float vHigh = lut[high];
        float weight = (value - vLow) / (vHigh - vLow);
        return float.Lerp(vLow, vHigh, weight);
    }

    public static float[] Gamma(this Image<HalfVector4> image, float gamma, float gain, float shift)
    {
        // Will return the LUT for use in the UI 
        float[] lut = ImagingAlgorithms.CreateGammaLUT(gamma);

        // Parallelize the loop over the rows
        int height = image.Height;
        // Parallel.For(0, height, y =>
        for (int y = 0; y < height; y++)
        {
            // Get a span for the current row for fast, safe access
            Span<HalfVector4> row = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < row.Length; x++)
            {
                // pixel manipulation
                var pixel = row[x].ToScaledVector4();
                float r = LutLookup(lut, pixel.X);
                float g = LutLookup(lut, pixel.Y);
                float b = LutLookup(lut, pixel.Z);
                pixel.X = ClipF(gain * (r + shift));
                pixel.Y = ClipF(gain * (g + shift));
                pixel.Z = ClipF(gain * (b + shift));
            }
            // });
        } 
        return lut;
    }

    #endregion Brightness / Gamma 

    #region White Balance 

    // Tanner Helland Algorithm 
    // See: 	https://tannerhelland.com/2012/09/18/convert-temperature-rgb-algorithm-code.html
    //
    // NOT Working so well 
    // 
    public static void AdjustColorTemperature(this Image<HalfVector4> image, float kelvin)
    {
        float[] rgb = GetRgbFromTemperature(kelvin);
        float red = rgb[0];
        float green = rgb[1];
        float blue = rgb[2];

        // Parallelize the loop over the rows
        int height = image.Height;
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast, safe access
            Span<HalfVector4> row = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < row.Length; x++)
            {
                // Apply the temperature scaling
                var pixel = row[x].ToScaledVector4();
                pixel.X = ClipF(pixel.X * rgb[0]);
                pixel.Y = ClipF(pixel.Y * rgb[1]);
                pixel.Z = ClipF(pixel.Z * rgb[2]);
            }
        });
    }

    public static float[] GetRgbFromTemperature(float temperature)
    {
        // Temperature must fit between 1000 and 40000 degrees.
        // All calculations require temperature / 100, so only do the conversion once.
        temperature = Math.Clamp(temperature, 1000, 40000);
        temperature /= 100;

        // Compute each color in turn.
        float red, green, blue;

        // First: red.
        if (temperature <= 66)
        {
            red = 1.0f;
        }
        else
        {
            // Note: the R-squared value for this approximation is 0.988.
            red = 329.698727446f * MathF.Pow(temperature - 60.0f, -0.1332047592f) / 255.0f;
            red = ClipF(red);
        }

        // Second: green.
        if (temperature <= 66)
        {
            // Note: the R-squared value for this approximation is 0.996.
            green = (99.4708025861f * MathF.Log(temperature) - 161.1195681661f) / 255.0f;
        }
        else
        {
            // Note: the R-squared value for this approximation is 0.987.
            green = 288.1221695283f * MathF.Pow(temperature - 60.0f, -0.0755148492f) / 255.0f;
        }

        green = ClipF(green);

        // Third: blue.
        if (temperature >= 66)
        {
            blue = 1.0f;
        }
        else if (temperature <= 19)
        {
            blue = 0.0f;
        }
        else
        {
            // Note: the R-squared value for this approximation is 0.998.
            blue = (138.5177312231f * MathF.Log(temperature - 10.0f) - 305.0447927307f) / 255.0f;
            blue = ClipF(blue);
        }

        return [red, green, blue];
    }

    // By setting the saturationThreshold to 0.4, any pixel that is more than 40 % saturated gets skipped. 
    // The algorithm now looks at the neutral sidewalks, stones, gray tree trunks, or white clothing in the photo
    // to find the true color cast.
    public static bool FilteredGrayWorldAWB(this Image<HalfVector4> image, float saturationThreshold = 0.4f)
    {
        float totalR = 0, totalG = 0, totalB = 0;
        long validPixelCount = 0;

        // Sum up only the low-saturation pixels
        int height = image.Height;
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast, safe access
            Span<HalfVector4> pixelRow = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < pixelRow.Length; x++)
            {
                var pixel = pixelRow[x].ToScaledVector4();
                float r = pixel.X;
                float g = pixel.Y;
                float b = pixel.Z;

                // Calculate saturation (scaled from 0.0 to 1.0)
                float max = MathF.Max(r, MathF.Max(g, b));
                float min = MathF.Min(r, MathF.Min(g, b));
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
        float avgR = totalR / validPixelCount;
        float avgG = totalG / validPixelCount;
        float avgB = totalB / validPixelCount;

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
        float targetGray = (avgR + avgG + avgB) / 3.0f;
        float rGain = targetGray / avgR;
        float gGain = targetGray / avgG;
        float bGain = targetGray / avgB;

        // Apply the gains to EVERY pixel in the image
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast, safe access
            Span<HalfVector4> pixelRow = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < pixelRow.Length; x++)
            {
                var pixel = pixelRow[x].ToScaledVector4();
                pixel.X = ClipF(pixel.X * rGain);
                pixel.Y = ClipF(pixel.Y * gGain);
                pixel.Z = ClipF(pixel.Z * bGain);
            }
        });

        return true;
    }

    public static void WhitePatchWhiteBalance(this Image<HalfVector4> image, float r, float g, float b)
    {
        float luminance = (float)MathF.Sqrt(0.299f * (r * r) + 0.587f * (g * g) + 0.114f * (b * b));
        float rGain = r < 0.001f ? 1.0f : luminance / r;
        float gGain = g < 0.001f ? 1.0f : luminance / g;
        float bGain = b < 0.001f ? 1.0f : luminance / b;

        // Apply the gains to all pixels in the image
        int height = image.Height;
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast, safe access
            Span<HalfVector4> pixelRow = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < pixelRow.Length; x++)
            {
                var pixel = pixelRow[x].ToScaledVector4();
                pixel.X = ClipF(pixel.X * rGain);
                pixel.Y = ClipF(pixel.Y * gGain);
                pixel.Z = ClipF(pixel.Z * bGain);
            }
        });
    }

    #endregion  White Balance 

    #region Highlights and Shadows

    public static void HighlightsShadows(this Image<HalfVector4> image, float highlight, float shadow)
    {
        const float compress = 0.5f;
        const float low_approximation = 0.01f;
        const float shadowColor = 1.0f;
        const float highlightColor = 1.0f;

        float highlights_sign_negated = MathF.CopySign(1.0f, -highlight);
        float shadows_sign = MathF.CopySign(1.0f, shadow);

        // Parallelize the loop over the rows
        int height = image.Height;
        Parallel.For(0, height, rowIndex =>
        {
            // Get a span for the current row for fast, safe access
            Span<HalfVector4> row = image.DangerousGetPixelRowMemory(rowIndex).Span;
            for (int x = 0; x < row.Length; x++)
            {
                bool pixelChanged = false;

                var pixel = row[x].ToScaledVector4();
                float r = pixel.X;
                float g = pixel.Y;
                float b = pixel.Z;
                ColorUtilities.RgbToYiq(r, g, b, out float y, out float i, out float q);

                // No blur yet , use same for now 
                //
                // The highlight and shadow adjustments are applied to the luminance (Y) channel of the original image.
                // The original algorithm uses a blurred image.
                // 
                float yBlur = y;

                float tb0 = 1.0f - yBlur;
                if (tb0 < 1.0f - compress)
                {
                    float highlights2 = highlight * highlight;
                    float highlights_xform = Math.Min(1.0f - tb0 / (1.0f - compress), 1.0f);

                    while (highlights2 > 0.0f)
                    {
                        float la = y;
                        float la_abs = Math.Abs(la);
                        float la_inverted = 1.0f - la;
                        float la_inverted_abs = Math.Abs(la_inverted);
                        float lb = (tb0 - 0.5f) * highlights_sign_negated * Math.Sign(la_inverted) + 0.5f;

                        float lref = MathF.CopySign(
                            la_abs > low_approximation ?
                                1.0f / la_abs :
                                1.0f / low_approximation, la);
                        float href = MathF.CopySign(
                            la_inverted_abs > low_approximation ? 1.0f / la_inverted_abs : 1.0f / low_approximation,
                            la_inverted);

                        float chunk = highlights2 > 1.0f ? 1.0f : highlights2;
                        float optrans = chunk * highlights_xform;
                        highlights2 -= 1.0f;

                        y = la * (1.0f - optrans) + (la > 0.5f ?
                                1.0f - (1.0f - 2.0f * (la - 0.5f)) * (1.0f - lb) :
                                2.0f * la * lb) * optrans;

                        i = i * (1.0f - optrans) +
                            i * (y * lref * (1.0f - highlightColor) + (1.0f - y) * href * highlightColor) * optrans;

                        q = q * (1.0f - optrans) +
                            q * (y * lref * (1.0f - highlightColor) + (1.0f - y) * href * highlightColor) * optrans;

                        pixelChanged = true;
                    }
                }

                if (tb0 > compress)
                {
                    float shadows2 = shadow * shadow;
                    float shadows_xform = Math.Min(tb0 / (1.0f - compress) - compress / (1.0f - compress), 1.0f);

                    while (shadows2 > 0.0f)
                    {
                        float la = y;
                        float la_abs = Math.Abs(la);
                        float la_inverted = 1.0f - la;
                        float la_inverted_abs = Math.Abs(la_inverted);
                        float lb = (tb0 - 0.5f) * shadows_sign * Math.Sign(la_inverted) + 0.5f;

                        float lref = MathF.CopySign(
                            la_abs > low_approximation ? 1.0f / la_abs : 1.0f / low_approximation, la);
                        float href = MathF.CopySign(
                            la_inverted_abs > low_approximation ? 1.0f / la_inverted_abs : 1.0f / low_approximation,
                            la_inverted);

                        float chunk = shadows2 > 1.0f ? 1.0f : shadows2;
                        float optrans = chunk * shadows_xform;

                        shadows2 -= 1.0f;

                        y = la * (1.0f - optrans) + (la > 0.5f ?
                                1.0f - (1.0f - 2.0f * (la - 0.5f)) * (1.0f - lb) :
                                2.0f * la * lb) * optrans;

                        i = i * (1.0f - optrans) +
                            i * (y * lref * (1.0f - shadowColor) + (1.0f - y) * href * shadowColor) * optrans;

                        q = q * (1.0f - optrans) +
                            q * (y * lref * (1.0f - shadowColor) + (1.0f - y) * href * shadowColor) * optrans;

                        pixelChanged = true;
                    }
                }

                if (pixelChanged)
                {
                    ColorUtilities.YiqToRgb(y, i, q, out r, out g, out b);
                    pixel.X = ClipF(r);
                    pixel.Y = ClipF(g);
                    pixel.Z = ClipF(b);
                }
            }
        });
    }

    #region DEAD 

    // To eliminate sharp edges and halos, pre-apply a Gaussian Blur to a working luminance mask before integrating
    // the adjustments

    //  Implementing a highlights and shadows adjustment requires extracting the pixel data and running 
    //  a non-linear luminance alteration formula. it calculates the perceived luminance of each pixel, 
    //  then maps highlights and shadows independently using a power curve(gamma mapping).
    // 
    //public static void BAD_HighlightsShadows(this Image<HalfVector4> image, float highlightAmount, float shadowAmount)
    //{
    //    const float luminanceLowThreshold = 0.35f;
    //    const float luminanceHighThreshold = 0.65f;
    //    const float clipFactor = 4.5f;

    //    // shadowAmount and highlightAmount range from -1 to 1
    //    // Convert parameters to gamma modifiers
    //    float shadowGamma = 1.0f - shadowAmount;
    //    float highlightGamma = 1.0f - highlightAmount;

    //    // Parallelize the loop over the rows
    //    int height = image.Height;
    //    Parallel.For(0, height, y =>
    //    {
    //        // Get a span for the current row for fast, safe access
    //        Span<HalfVector4> row = image.DangerousGetPixelRowMemory(y).Span;
    //        for (int x = 0; x < row.Length; x++)
    //        {
    //            HalfVector4 pixel = row[x];

    //            // Normalize RGB to [0, 1] float range
    //            float r = pixel.R / 65535.0f;
    //            float g = pixel.G / 65535.0f;
    //            float b = pixel.B / 65535.0f;

    //            // Calculate perceived luminance
    //            float luminance = (float)MathF.Sqrt(0.299f * (r * r) + 0.587f * (g * g) + 0.114f * (b * b));
    //            float factor = 1.0f;
    //            if (luminance < luminanceLowThreshold && shadowAmount != 0)
    //            {
    //                // Shadow adjustment: targets dark areas 
    //                factor = (float)MathF.Pow(2.0f * luminance, shadowGamma) / (2.0f * luminance);
    //            }
    //            else if (luminance >= luminanceHighThreshold && highlightAmount != 0)
    //            {
    //                // Highlight adjustment: targets bright areas 
    //                factor = (float)MathF.Pow(2.0f * (1.0f - luminance), highlightGamma) / (2.0f * (1.0f - luminance));
    //            }
    //            else
    //            {
    //                // No change in the midtones 
    //                continue;
    //            }

    //            // Clip factor to prevent anomalies
    //            factor = Math.Max(0.0f, Math.Min(clipFactor, factor));

    //            // Denormalize 
    //            factor *= 65535.0f;

    //            // Apply factor selectively while preserving hues
    //            row[x].R = (ushort)Math.Min(65535, r * factor);
    //            row[x].G = (ushort)Math.Min(65535, g * factor);
    //            row[x].B = (ushort)Math.Min(65535, b * factor);
    //        }
    //    });
    //}

    //// Does not work better than the above 
    //// Keep for now 
    //public static void ALT_HighlightsShadows(this Image<HalfVector4> image, float highlightAmount, float shadowAmount)
    //{
    //    const float lightnessLowThreshold = 0.30f;
    //    const float lightnessHighThreshold = 0.70f;

    //    // Parallelize the loop over the rows
    //    int height = image.Height;
    //    Parallel.For(0, height, y =>
    //    {
    //        // Get a span for the current row for fast, safe access
    //        Span<HalfVector4> row = image.DangerousGetPixelRowMemory(y).Span;
    //        for (int x = 0; x < row.Length; x++)
    //        {
    //            HalfVector4 pixel = row[x];

    //            // Normalize RGB to [0, 1] float range
    //            float r = pixel.R / 65535.0f;
    //            float g = pixel.G / 65535.0f;
    //            float b = pixel.B / 65535.0f;

    //            // Convert normalize RGB to HSL color space
    //            ColorUtilities.RgbToHsl(r, g, b, out float hue, out float saturation, out float lightness);

    //            if (lightness <= 0.0f)
    //            {
    //                Debugger.Break();
    //            }

    //            if (lightness < lightnessLowThreshold)
    //            {
    //                // Scale the adjustment factor based on how dark the pixel is
    //                // Modify shadows (Lightness is low, e.g., below 40%)
    //                float shadowWeight = (lightnessLowThreshold - lightness) / lightnessLowThreshold;
    //                lightness += shadowAmount * shadowWeight;
    //            }
    //            else if (lightness > lightnessHighThreshold)
    //            {
    //                // Modify highlights: Lightness is high: above 60%
    //                // Scale the adjustment factor based on how bright the pixel is
    //                float highlightWeight = (lightness - lightnessHighThreshold) / lightnessHighThreshold;
    //                lightness += highlightAmount * highlightWeight;
    //            }

    //            // Keep lightness bound within 0.0 to 1.0 limits
    //            lightness = ClipF(lightness);

    //            // Convert back to RGB space and update pixel 
    //            ColorUtilities.HslToRgb(hue, saturation, lightness, out ushort newR, out ushort newG, out ushort newB);
    //            row[x].R = newR;
    //            row[x].G = newG;
    //            row[x].B = newB;
    //        }
    //    });
    //}
    #endregion DEAD 

    #endregion Highlights and Shadows

    #region Vibrance 

    // See:
    // https://github.com/zachsaw/RenderScripts/blob/master/RenderScripts/ImageProcessingShaders/SweetFX/Vibrance.hlsl 
    // 
    // Intelligently saturates (or desaturates if you use negative values) the pixels depending on
    // their original saturation.
    // Vibrance intelligently boosts the saturation of pixels so pixels that had little color get a larger boost
    // than pixels that had a lot.
    // This avoids oversaturation of pixels that were already very saturated.
    // 
    // All three amounts [-1.00 to 1.00] on the UI 
    public static void Vibrance(this Image<HalfVector4> image, float redAmount, float greenAmount, float blueAmount)
    {
        const float scaleFactor = 3.3f;
        redAmount *= scaleFactor;
        greenAmount *= scaleFactor;
        blueAmount *= scaleFactor;

        float signRed = (float)Math.Sign(redAmount);
        float signGreen = (float)Math.Sign(greenAmount);
        float signBlue = (float)Math.Sign(blueAmount);

        // Parallelize the loop over the rows
        int height = image.Height;
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast, safe access
            Span<HalfVector4> row = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < row.Length; x++)
            {
                var pixel = row[x].ToScaledVector4();
                float r = pixel.X;
                float g = pixel.Y;
                float b = pixel.Z;

                // Calculate perceived luminance
                float luminance = (float)MathF.Sqrt(0.299f * (r * r) + 0.587f * (g * g) + 0.114f * (b * b));

                // Find the strongest color
                float maxColor = MathF.Max(r, MathF.Max(g, b));

                //Find the weakest color
                float minColor = MathF.Min(r, MathF.Min(g, b));

                // The difference between the two is the saturation
                float saturation = maxColor - minColor;

                // Linear Interpolation between luminance and original by 1 + (1-saturation) - current
                // (1.0 + (Vibrance_coeff * (1.0 - (sign(Vibrance_coeff) * saturation))))
                float redCoeff = 1.0f + (redAmount * (1.0f - signRed * saturation));
                r = float.Lerp(luminance, r, redCoeff);
                float greenCoeff = 1.0f + (greenAmount * (1.0f - signGreen * saturation));
                g = float.Lerp(luminance, g, greenCoeff);
                float blueCoeff = 1.0f + (blueAmount * (1.0f - signBlue * saturation));
                b = float.Lerp(luminance, b, blueCoeff);

                pixel.X = ClipF(r);
                pixel.Y = ClipF(g);
                pixel.Z = ClipF(b);
            }
        });
    }

    #endregion Vibrance 

    #region SCurves Contrast

    // Adjusting the multiplier will alter contrast intensity
    private static float[] CreateSCurveLUT(float contrastMultiplier)
    {
        float[] lut = new float[LutSize];
        Parallel.For(0, LutSize, i =>
        {
            // Normalize to 0.0 - 1.0  and apply power curve
            float normalized = (float)i / (LutSize - 1);

            // Mathematical S-Curve (Sigmoid function)
            float sCurveValue = 1.0f / (1.0f + MathF.Exp(-contrastMultiplier * (normalized - 0.5f)));
            lut[i] = ClipF(sCurveValue);
        });
        return lut;
    }

    public static void ApplySCurveContrast(
        this Image<HalfVector4> image, float redAmount, float greenAmount, float blueAmount)
    {
        // Only one table should change between calls, consider caching 
        float[] redLut = CreateSCurveLUT(redAmount);
        float[] greenLut = CreateSCurveLUT(greenAmount);
        float[] blueLut = CreateSCurveLUT(blueAmount);

        // Parallelize the loop over the rows
        int height = image.Height;
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast, safe access
            Span<HalfVector4> row = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < row.Length; x++)
            {
                // pixel manipulation
                var pixel = row[x].ToScaledVector4();
                float r = LutLookup(redLut, pixel.X);
                float g = LutLookup(greenLut, pixel.Y);
                float b = LutLookup(blueLut, pixel.Z);
                pixel.X = ClipF(r);
                pixel.Y = ClipF(g);
                pixel.Z = ClipF(b);
            }
        });
    }

    #endregion SCurves Contrast

    #region Vignette

    public static void Vignette(
        this Image<HalfVector4> image, float top, float bottom, float left, float right, float lightness)
    {
        int topRow = (int)(image.Height * top);
        int bottomRow = (int)(image.Height * (1.0f - bottom));
        int leftCol = (int)(image.Width * left);
        int rightCol = (int)(image.Width * (1.0f - right));
        bool darkVignette = lightness < 0.0f;
        float lightnessFactor = MathF.Abs(lightness);

        // Parallelize the loop over the rows
        int height = image.Height;
        Parallel.For(0, height, row =>
        // for (int row = 0; row < height; row++)
        {
            // Inside the vignette area 
            float topFactor = 0.0f;
            float deltaTop = topRow - row;
            if (deltaTop > 0)
            {
                // Inside the top vignette area, calculate the factor based on distance from the top edge
                topFactor = deltaTop / topRow;
            }

            float bottomFactor = 0.0f;
            float deltaBottom = row - bottomRow;
            if (deltaBottom > 0)
            {
                // Inside the bottom vignette area, calculate the factor based on distance from the bottom edge
                bottomFactor = deltaBottom / (image.Height - bottomRow);
            }

            // Get a span for the current row for fast, safe access
            Span<HalfVector4> rowSpan = image.DangerousGetPixelRowMemory(row).Span;
            for (int col = 0; col < rowSpan.Length; col++)
            {
                if (row > topRow && row < bottomRow && col > leftCol && col < rightCol)
                {
                    // Outside the vignette area, do nothing 
                    continue;
                }

                float leftFactor = 0.0f;
                float deltaLeft = leftCol - col;
                if (deltaLeft > 0)
                {
                    // Inside the left vignette area, calculate the factor based on distance from the left edge
                    leftFactor = deltaLeft / leftCol;
                }

                float rightFactor = 0.0f;
                float deltaRight = col - rightCol;
                if (deltaRight > 0)
                {
                    // Inside the right vignette area, calculate the factor based on distance from the right edge
                    rightFactor = deltaRight / (image.Width - rightCol);
                }

                var pixel = rowSpan[col].ToScaledVector4();
                float r = pixel.X;
                float g = pixel.Y;
                float b = pixel.Z;

                float vignetteFactor = MathF.Max(MathF.Max(topFactor, bottomFactor), MathF.Max(leftFactor, rightFactor));
                if (darkVignette)
                {
                    // Convert to HSL
                    ColorUtilities.RgbToHsl(r, g, b, out float hue, out float saturation, out float pixelLightness);

                    // Darken the pixel based on the lightness factor and the vignette factor
                    pixelLightness -= vignetteFactor * lightnessFactor;
                    pixelLightness = ClipF(pixelLightness);

                    // Convert back to float RGB 
                    ColorUtilities.HslToRgb(hue, saturation, pixelLightness, out float tr, out float tg, out float tb);
                    pixel.X = ClipF(tr);
                    pixel.Y = ClipF(tg);
                    pixel.Z = ClipF(tb);
                }
                else
                {
                    // Lighten the pixel based on the lightness factor and the vignette factor
                    // Do NOT convert to HSL, just scale the RGB values directly to avoid color shifts
                    float scale = 1.0f + vignetteFactor * lightnessFactor;
                    r *= scale;
                    g *= scale;
                    b *= scale;
                    pixel.X = ClipF(r);
                    pixel.Y = ClipF(g);
                    pixel.Z = ClipF(b);
                }
            }
            // } // 'classic' for
        }); // Parallel For 
    }

    #endregion Vignette

    #region LUT 

    public static void Lut(this Image<HalfVector4> image, LutMetadata lutMetadata)
    {
        if (lutMetadata == LutMetadata.Empty)
        {
            return;
        }

        if (!LutsManager.TryLoadLut(lutMetadata, out Lut? lut))
        {
            // Failed to load LUT ? 
            if (Debugger.IsAttached) { Debugger.Break(); }
            return;
        }

        int height = image.Height;
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast, safe access
            Span<HalfVector4> pixelRow = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < pixelRow.Length; x++)
            {
                var pixel = pixelRow[x].ToScaledVector4();
                var lutColor = LutColor.FromRgbFloat(pixel.X, pixel.Y, pixel.Z);
                var transformed = lut.Lookup(lutColor);
                pixel.X = ClipF(transformed.B);
                pixel.Y = ClipF(transformed.G);
                pixel.Z = ClipF(transformed.R);
            }
        });
    }

    #endregion LUT 
}