namespace Lyt.PhotoPostPro.Model.Algorithms;

using static ImagingUtilities;
using static System.MathF;

public static partial class ImagingAlgorithms
{
    #region Brightness / Gamma 

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

    #endregion Brightness / Gamma 

    #region White Balance 

    // Tanner Helland Algorithm 
    // See: 	https://tannerhelland.com/2012/09/18/convert-temperature-rgb-algorithm-code.html
    //
    // NOT Working so well 
    // 
    public static void AdjustColorTemperature(this Image<Rgb48> image, float kelvin)
    {
        ushort[] rgb = GetRgbFromTemperature(kelvin);
        ushort red = rgb[0];
        ushort green = rgb[1];
        ushort blue = rgb[2];

        // Parallelize the loop over the rows
        int height = image.Height;
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast, safe access
            Span<Rgb48> row = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < row.Length; x++)
            {
                // Apply the temperature scaling
                Rgb48 pixel = row[x];
                row[x].R = (ushort)Math.Min(65535.0, pixel.R * (blue / 65535.0));
                row[x].G = (ushort)Math.Min(65535.0, pixel.G * (green / 65535.0));
                row[x].B = (ushort)Math.Min(65535.0, pixel.B * (red / 65535.0));
            }
        });
    }

    public static ushort[] GetRgbFromTemperature(double temperature)
    {
        const int pixelMax = 65535;

        // Temperature must fit between 1000 and 40000 degrees.
        temperature = Math.Clamp(temperature, 1000, 40000);

        // All calculations require temperature / 100, so only do the conversion once.
        temperature /= 100;

        // Compute each color in turn.
        int red, green, blue;

        // First: red.
        if (temperature <= 66)
        {
            red = pixelMax;
        }
        else
        {
            // Note: the R-squared value for this approximation is 0.988.
            red = (int)(255.0 * 329.698727446 * Math.Pow(temperature - 60, -0.1332047592));
            red = Math.Clamp(red, 0, pixelMax);
        }

        // Second: green.
        if (temperature <= 66)
        {
            // Note: the R-squared value for this approximation is 0.996.
            green = (int)(255.0 * 99.4708025861 * Math.Log(temperature) - 161.1195681661);
        }
        else
        {
            // Note: the R-squared value for this approximation is 0.987.
            green = (int)(255.0 * 288.1221695283 * (Math.Pow(temperature - 60, -0.0755148492)));
        }

        green = Math.Clamp(green, 0, pixelMax);

        // Third: blue.
        if (temperature >= 66)
        {
            blue = pixelMax;
        }
        else if (temperature <= 19)
        {
            blue = 0;
        }
        else
        {
            // Note: the R-squared value for this approximation is 0.998.
            blue = (int)(255.0 * 138.5177312231 * Math.Log(temperature - 10) - 305.0447927307);
            blue = Math.Clamp(blue, 0, pixelMax);
        }

        return [(ushort)red, (ushort)green, (ushort)blue];
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

    public static void WhitePatchWhiteBalance(this Image<Rgb48> image, float r, float g, float b)
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
    }

    #endregion  White Balance 

    #region Highlights and Shadows

    public static void HighlightsShadows(this Image<Rgb48> image, float highlight, float shadow)
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
            Span<Rgb48> row = image.DangerousGetPixelRowMemory(rowIndex).Span;
            for (int x = 0; x < row.Length; x++)
            {
                Rgb48 pixel = row[x];
                bool pixelChanged = false;

                // Normalize RGB to [0, 1] float range
                float r = pixel.R / 65535.0f;
                float g = pixel.G / 65535.0f;
                float b = pixel.B / 65535.0f;
                ColorUtilities.RgbToYiq(r, g, b, out float y, out float i, out float q);

                // No blur yet , use same for now 
                float yBlur = y;
                float iBlur = i;
                float qBlur = q;

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
                            la_abs > low_approximation ? 1.0f / la_abs : 1.0f / low_approximation,  la);
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
                    row[x].R = DeNormalizeClip16(r);
                    row[x].G = DeNormalizeClip16(g);
                    row[x].B = DeNormalizeClip16(b);
                } 
            }
        });
    }

    // To eliminate sharp edges and halos, pre-apply a Gaussian Blur to a working luminance mask before integrating
    // the adjustments

    //  Implementing a highlights and shadows adjustment requires extracting the pixel data and running 
    //  a non-linear luminance alteration formula. it calculates the perceived luminance of each pixel, 
    //  then maps highlights and shadows independently using a power curve(gamma mapping).
    // 
    public static void BAD_HighlightsShadows(this Image<Rgb48> image, float highlightAmount, float shadowAmount)
    {
        const float luminanceLowThreshold = 0.35f;
        const float luminanceHighThreshold = 0.65f;
        const float clipFactor = 4.5f;

        // shadowAmount and highlightAmount range from -1 to 1
        // Convert parameters to gamma modifiers
        float shadowGamma = 1.0f - shadowAmount;
        float highlightGamma = 1.0f - highlightAmount;

        // Parallelize the loop over the rows
        int height = image.Height;
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast, safe access
            Span<Rgb48> row = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < row.Length; x++)
            {
                Rgb48 pixel = row[x];

                // Normalize RGB to [0, 1] float range
                float r = pixel.R / 65535.0f;
                float g = pixel.G / 65535.0f;
                float b = pixel.B / 65535.0f;

                // Calculate perceived luminance
                float luminance = (float)MathF.Sqrt(0.299f * (r * r) + 0.587f * (g * g) + 0.114f * (b * b));
                float factor = 1.0f;
                if (luminance < luminanceLowThreshold && shadowAmount != 0)
                {
                    // Shadow adjustment: targets dark areas 
                    factor = (float)MathF.Pow(2.0f * luminance, shadowGamma) / (2.0f * luminance);
                }
                else if (luminance >= luminanceHighThreshold && highlightAmount != 0)
                {
                    // Highlight adjustment: targets bright areas 
                    factor = (float)MathF.Pow(2.0f * (1.0f - luminance), highlightGamma) / (2.0f * (1.0f - luminance));
                }
                else
                {
                    // No change in the midtones 
                    continue;
                }

                // Clip factor to prevent anomalies
                factor = Math.Max(0.0f, Math.Min(clipFactor, factor));

                // Denormalize 
                factor *= 65535.0f;

                // Apply factor selectively while preserving hues
                row[x].R = (ushort)Math.Min(65535, r * factor);
                row[x].G = (ushort)Math.Min(65535, g * factor);
                row[x].B = (ushort)Math.Min(65535, b * factor);
            }
        });
    }

    // Does not work better than the above 
    // Keep for now 
    public static void ALT_HighlightsShadows(this Image<Rgb48> image, float highlightAmount, float shadowAmount)
    {
        const float lightnessLowThreshold = 0.30f;
        const float lightnessHighThreshold = 0.70f;

        // Parallelize the loop over the rows
        int height = image.Height;
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast, safe access
            Span<Rgb48> row = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < row.Length; x++)
            {
                Rgb48 pixel = row[x];

                // Normalize RGB to [0, 1] float range
                float r = pixel.R / 65535.0f;
                float g = pixel.G / 65535.0f;
                float b = pixel.B / 65535.0f;

                // Convert normalize RGB to HSL color space
                ColorUtilities.RgbToHsl(r, g, b, out float hue, out float saturation, out float lightness);

                if (lightness <= 0.0f)
                {
                    Debugger.Break();
                }

                if (lightness < lightnessLowThreshold)
                {
                    // Scale the adjustment factor based on how dark the pixel is
                    // Modify shadows (Lightness is low, e.g., below 40%)
                    float shadowWeight = (lightnessLowThreshold - lightness) / lightnessLowThreshold;
                    lightness += shadowAmount * shadowWeight;
                }
                else if (lightness > lightnessHighThreshold)
                {
                    // Modify highlights: Lightness is high: above 60%
                    // Scale the adjustment factor based on how bright the pixel is
                    float highlightWeight = (lightness - lightnessHighThreshold) / lightnessHighThreshold;
                    lightness += highlightAmount * highlightWeight;
                }

                // Keep lightness bound within 0.0 to 1.0 limits
                lightness = ClipF(lightness);

                // Convert back to RGB space and update pixel 
                ColorUtilities.HslToRgb(hue, saturation, lightness, out ushort newR, out ushort newG, out ushort newB);
                row[x].R = newR;
                row[x].G = newG;
                row[x].B = newB;
            }
        });
    }

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
    public static void Vibrance(this Image<Rgb48> image, float redAmount, float greenAmount, float blueAmount)
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
            Span<Rgb48> row = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < row.Length; x++)
            {
                Rgb48 pixel = row[x];

                // Normalize RGB to [0, 1] float range
                float r = pixel.R / 65535.0f;
                float g = pixel.G / 65535.0f;
                float b = pixel.B / 65535.0f;

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

                row[x].R = DeNormalizeClip16(r);
                row[x].G = DeNormalizeClip16(g); ;
                row[x].B = DeNormalizeClip16(b); ;
            }
        });
    }

    #endregion Vibrance 

    #region SCurves Contrast

    // Adjusting the multiplier will alter contrast intensity
    private static ushort[] CreateSCurveLUT(float contrastMultiplier)
    {
        ushort[] lut = new ushort[pixRangeI];
        for (int i = 0; i < pixRangeI; ++i)
        {
            // Normalize the 0-65535 value to a 0.0 - 1.0 range
            float normalizedVal = i / pixMaxF;

            // Mathematical S-Curve (Sigmoid function)
            float sCurveValue = 1.0f / (1.0f + (float)Math.Exp(-contrastMultiplier * (normalizedVal - 0.5f)));

            // Denormalize back to 0-65535
            lut[i] = DeNormalizeClip16(sCurveValue);
        }

        return lut;
    }

    public static void ApplySCurveContrast(this Image<Rgb48> image, float redAmount, float greenAmount, float blueAmount)
    {
        // Only one table should change between calls, consider caching 
        ushort[] redLut = CreateSCurveLUT(redAmount);
        ushort[] greenLut = CreateSCurveLUT(greenAmount);
        ushort[] blueLut = CreateSCurveLUT(blueAmount);

        // Parallelize the loop over the rows
        int height = image.Height;
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast, safe access
            Span<Rgb48> row = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < row.Length; x++)
            {
                Rgb48 pixel = row[x];

                row[x].R = redLut[pixel.R];
                row[x].G = greenLut[pixel.G];
                row[x].B = blueLut[pixel.B];
            }
        });
    }

    #endregion SCurves Contrast
}
