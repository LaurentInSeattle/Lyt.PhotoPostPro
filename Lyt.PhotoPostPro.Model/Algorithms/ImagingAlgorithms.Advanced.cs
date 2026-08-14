namespace Lyt.PhotoPostPro.Model.Algorithms;

using static ImagingUtilities;
using static System.MathF;

public static partial class ImagingAlgorithms
{
    #region Brightness / Gamma 

    public const int LutSize = 1024;

    /// <summary> Creates a Look-Up Table for fast gamma correction. </summary>
    /// <param name="gamma">Gamma value (e.g., 2.2 to brighten midtones, 0.45 to darken).</param>
    public static Half[] CreateGammaLUT(float gamma)
    {
        // Prevent potential division by zero
        if (gamma <= 0.0f)
        {
            gamma = 1.0f;
        }

        float inverseGamma = 1.0f / gamma;
        var lut = new Half[LutSize];
        Parallel.For(0, LutSize, i =>
        {
            // Normalize to 0.0 - 1.0  and apply power curve
            float normalized = (float)i / (LutSize - 1);
            float corrected = MathF.Pow(normalized, inverseGamma);

            // Scale back to 0 - 1 and round safely
            lut[i] = ClipH((Half)corrected);
        });

        return lut;
    }

    public static Half LutLookup(Half[] lut, Half value)
    {
        int low = (int)Math.Floor((float)value * LutSize);
        float mid = (float)value * LutSize;
        int high = low + 1;
        if ((low < 0) || (high >= LutSize))
        {
            return value;
        }

        Half vLow = lut[low];
        Half vHigh = lut[high];
        float ratio = (mid - low) / (high - low);
        Half alpha = hOne - (Half)ratio;
        alpha = ClipH(alpha);
        Half lerp = (hOne - alpha) * vLow + alpha * vHigh;

        return lerp;
    }

    public static Half[] Gamma(this Image<RgbaHalf> image, float gamma, float gain, float shift)
    {
        // TODO : Optimize if gamma is zero 

        // Will return the LUT for use in the UI 
        Half[] lut = ImagingAlgorithms.CreateGammaLUT(gamma);
        var halfGain = (Half)gain;
        var halfShift = (Half)shift;

        // Parallelize the loop over the rows
        int height = image.Height;
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast access
            Span<RgbaHalf> row = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < row.Length; x++)
            {
                // pixel manipulation
                var pixel = row[x];
                Half r = LutLookup(lut, pixel.R);
                Half g = LutLookup(lut, pixel.G);
                Half b = LutLookup(lut, pixel.B);
                row[x].R = ClipH(halfGain * (r + halfShift));
                row[x].G = ClipH(halfGain * (g + halfShift));
                row[x].B = ClipH(halfGain * (b + halfShift));
                //row[x] = new RgbaHalf(r, g, b, hOne);
            }
        });

        return lut;
    }

    #endregion Brightness / Gamma 

    #region White Balance 

    //// Tanner Helland Algorithm 
    //// See: 	https://tannerhelland.com/2012/09/18/convert-temperature-rgb-algorithm-code.html
    ////
    //// NOT Working so well 
    //// 
    //public static void AdjustColorTemperature(this Image<RgbaVector> image, float kelvin)
    //{
    //    float[] rgb = GetRgbFromTemperature(kelvin);
    //    float red = rgb[0];
    //    float green = rgb[1];
    //    float blue = rgb[2];

    //    // Parallelize the loop over the rows
    //    int height = image.Height;
    //    Parallel.For(0, height, y =>
    //    {
    //        // Get a span for the current row for fast access
    //        Span<RgbaVector> row = image.DangerousGetPixelRowMemory(y).Span;
    //        for (int x = 0; x < row.Length; x++)
    //        {
    //            // Apply the temperature scaling
    //            var pixel = row[x];
    //            float r = ClipF(pixel.R * red);
    //            float g = ClipF(pixel.G * green);
    //            float b = ClipF(pixel.B * blue);
    //            row[x] = new RgbaVector(r, g, b, 1.0f);
    //        }
    //    });
    //}

    //private static float[] GetRgbFromTemperature(float temperature)
    //{
    //    // Temperature must fit between 1000 and 40000 degrees.
    //    // All calculations require temperature / 100, so only do the conversion once.
    //    temperature = Math.Clamp(temperature, 1000, 40000);
    //    temperature /= 100;

    //    // Compute each color in turn.
    //    float red, green, blue;

    //    // First: red.
    //    if (temperature <= 66)
    //    {
    //        red = 1.0f;
    //    }
    //    else
    //    {
    //        // Note: the R-squared value for this approximation is 0.988.
    //        red = 329.698727446f * MathF.Pow(temperature - 60.0f, -0.1332047592f) / 255.0f;
    //        red = ClipF(red);
    //    }

    //    // Second: green.
    //    if (temperature <= 66)
    //    {
    //        // Note: the R-squared value for this approximation is 0.996.
    //        green = (99.4708025861f * MathF.Log(temperature) - 161.1195681661f) / 255.0f;
    //    }
    //    else
    //    {
    //        // Note: the R-squared value for this approximation is 0.987.
    //        green = 288.1221695283f * MathF.Pow(temperature - 60.0f, -0.0755148492f) / 255.0f;
    //    }

    //    green = ClipF(green);

    //    // Third: blue.
    //    if (temperature >= 66)
    //    {
    //        blue = 1.0f;
    //    }
    //    else if (temperature <= 19)
    //    {
    //        blue = 0.0f;
    //    }
    //    else
    //    {
    //        // Note: the R-squared value for this approximation is 0.998.
    //        blue = (138.5177312231f * MathF.Log(temperature - 10.0f) - 305.0447927307f) / 255.0f;
    //        blue = ClipF(blue);
    //    }

    //    return [red, green, blue];
    //}

    // By setting the saturationThreshold to 0.4, any pixel that is more than 40 % saturated gets skipped. 
    // The algorithm now looks at the neutral sidewalks, stones, gray tree trunks, or white clothing in the photo
    // to find the true color cast.
    public static bool FilteredGrayWorldAWB(this Image<RgbaHalf> image, float saturationThreshold = 0.4f)
    {
        float totalR = 0, totalG = 0, totalB = 0;
        long validPixelCount = 0;

        // Sum up only the low-saturation pixels
        int height = image.Height;
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast, safe access
            Span<RgbaHalf> pixelRow = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < pixelRow.Length; x++)
            {
                // var pixel = pixelRow[x].ToScaledVector4();
                var pixel = pixelRow[x];
                float r = (float)pixel.R;
                float g = (float)pixel.G;
                float b = (float)pixel.B;

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
        var rGain = (Half)(targetGray / avgR);
        var gGain = (Half)(targetGray / avgG);
        var bGain = (Half)(targetGray / avgB);

        // Apply the gains to EVERY pixel in the image
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast access
            Span<RgbaHalf> pixelRow = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < pixelRow.Length; x++)
            {
                var pixel = pixelRow[x];
                pixelRow[x].R = ClipH(pixel.R * rGain);
                pixelRow[x].G = ClipH(pixel.G * gGain);
                pixelRow[x].B = ClipH(pixel.B * bGain);
            }
        });

        return true;
    }

    public static void WhitePatchWhiteBalance(this Image<RgbaHalf> image, float r, float g, float b)
    {
        float luminance = (float)MathF.Sqrt(0.299f * (r * r) + 0.587f * (g * g) + 0.114f * (b * b));
        var rGain = (Half)(r < 0.001f ? 1.0f : luminance / r);
        var gGain = (Half)(g < 0.001f ? 1.0f : luminance / g);
        var bGain = (Half)(b < 0.001f ? 1.0f : luminance / b);

        // Apply the gains to all pixels in the image
        int height = image.Height;
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast, safe access
            Span<RgbaHalf> pixelRow = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < pixelRow.Length; x++)
            {
                var pixel = pixelRow[x];
                pixelRow[x].R = ClipH(pixel.R * rGain);
                pixelRow[x].G = ClipH(pixel.G * gGain);
                pixelRow[x].B = ClipH(pixel.B * bGain);
            }
        });
    }

    #endregion  White Balance 

    #region Highlights and Shadows

    public static void HighlightsShadows(this Image<RgbaHalf> image, float highlight, float shadow)
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
            // Get a span for the current row 
            Span<RgbaHalf> row = image.DangerousGetPixelRowMemory(rowIndex).Span;
            for (int x = 0; x < row.Length; x++)
            {
                bool pixelChanged = false;

                var pixel = row[x];
                float r = (float)pixel.R;
                float g = (float)pixel.G;
                float b = (float)pixel.B;
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
                    row[x].R = ClipH((Half)r);
                    row[x].G = ClipH((Half)g);
                    row[x].B = ClipH((Half)b);
                }
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
    public static void Vibrance(this Image<RgbaHalf> image, float redAmount, float greenAmount, float blueAmount)
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
            // Get a span for the current row for fast access
            Span<RgbaHalf> row = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < row.Length; x++)
            {
                var pixel = row[x];
                float r = (float)pixel.R;
                float g = (float)pixel.G;
                float b = (float)pixel.B;

                // Calculate perceived luminance
                float luminance = MathF.Sqrt(0.299f * (r * r) + 0.587f * (g * g) + 0.114f * (b * b));

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

                row[x].R = ClipH((Half)r);
                row[x].G = ClipH((Half)g);
                row[x].B = ClipH((Half)b);
            }
        });
    }

    #endregion Vibrance 

    #region SCurves Contrast

    // Adjusting the multiplier will alter contrast intensity
    private static Half[] CreateSCurveLUT(float contrastMultiplier)
    {
        Half[] lut = new Half[LutSize];
        Parallel.For(0, LutSize, i =>
        {
            // Normalize to 0.0 - 1.0  and apply power curve
            float normalized = (float)i / (LutSize - 1);

            // Mathematical S-Curve (Sigmoid function)
            float sCurveValue = 1.0f / (1.0f + MathF.Exp(-contrastMultiplier * (normalized - 0.5f)));
            lut[i] = ClipH((Half)sCurveValue);
        });

        return lut;
    }

    public static void ApplySCurveContrast(
        this Image<RgbaHalf> image, float redAmount, float greenAmount, float blueAmount)
    {
        // Only one table should change between calls, consider caching 
        Half[] redLut = CreateSCurveLUT(redAmount);
        Half[] greenLut = CreateSCurveLUT(greenAmount);
        Half[] blueLut = CreateSCurveLUT(blueAmount);

        // Parallelize the loop over the rows
        int height = image.Height;
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast, safe access
            Span<RgbaHalf> row = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < row.Length; x++)
            {
                // pixel manipulation
                var pixel = row[x];
                Half r = LutLookup(redLut, pixel.R);
                Half g = LutLookup(greenLut, pixel.G);
                Half b = LutLookup(blueLut, pixel.B);
                row[x].R = ClipH(r);
                row[x].G = ClipH(g);
                row[x].B = ClipH(b);
            }
        });
    }

    #endregion SCurves Contrast

    #region Vignette

    public static void Vignette(
        this Image<RgbaHalf> image, float top, float bottom, float left, float right, float lightness)
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
            Span<RgbaHalf> rowSpan = image.DangerousGetPixelRowMemory(row).Span;
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

                var pixel = rowSpan[col];
                float r = (float)pixel.R;
                float g = (float)pixel.G;
                float b = (float)pixel.B;

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
                    rowSpan[col].R = ClipH((Half)tr);
                    rowSpan[col].G = ClipH((Half)tg);
                    rowSpan[col].B = ClipH((Half)tb);
                }
                else
                {
                    // Lighten the pixel based on the lightness factor and the vignette factor
                    // Do NOT convert to HSL, just scale the RGB values directly to avoid color shifts
                    float scale = 1.0f + vignetteFactor * lightnessFactor;
                    r *= scale;
                    g *= scale;
                    b *= scale;
                    rowSpan[col].R = ClipH((Half)r);
                    rowSpan[col].G = ClipH((Half)g);
                    rowSpan[col].B = ClipH((Half)b);
                }
            }
        }); // Parallel For 
    }

    #endregion Vignette

    #region LUT 

    public static void Lut(this Image<RgbaHalf> image, Lut lut)
    {
        int height = image.Height;
        Parallel.For(0, height, y =>
        {
            // Get a span for the current row for fast, safe access
            Span<RgbaHalf> pixelRow = image.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < pixelRow.Length; x++)
            {
                var pixel = pixelRow[x];
                var transformed = lut.LookupTetrahedral(pixel.R, pixel.G, pixel.B);
                pixelRow[x].R = (Half)transformed.B;
                pixelRow[x].G = (Half)transformed.G;
                pixelRow[x].B = (Half)transformed.R;
            }
        });
    }

    #endregion LUT 
}