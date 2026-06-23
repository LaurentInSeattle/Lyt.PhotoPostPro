namespace Lyt.PhotoPostPro.Model.Algorithms;

using static ImagingUtilities;
using static System.Math;

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

				// Normalize RGB to [0, 1] float range
				float r = pixel.R / 65535.0f;
				float g = pixel.G / 65535.0f;
				float b = pixel.B / 65535.0f;

				// Calculate perceived luminance
				float luminance = (float)Sqrt(0.299 * (r * r) + 0.587 * (g * g) + 0.114 * (b * b));

#if OLD_ALGORITHM
				// Compute adjustment factors
				float h = highlightAmount * 0.05f * ((float)Math.Pow(8.0, luminance) - 1.0f);
				float s = shadowAmount * 0.05f * ((float)Math.Pow(8.0, 1.0f - luminance) - 1.0f);
				float hs = h + s;

				// Apply shifts, denormalize, clip and update pixel
				row[x].R = DeNormalizeClip16(r + hs);
				row[x].G = DeNormalizeClip16(g + hs);
				row[x].B = DeNormalizeClip16(b + hs);
#else
				// Create shadow and highlight masks
				float shadowMask = ClipF((0.5f - luminance) * 2.0f);
				float highlightMask = ClipF((luminance - 0.5f) * 2.0f);

				// Calculate adjustments 
				float shadowFactor = (float)Pow(luminance, 1.0f - shadowAmount);
				float highlightFactor = (float)Pow(luminance, 1.0f + highlightAmount);

				// Combine factors with masks to apply changes
				float adjustedLuminance = luminance;
				adjustedLuminance += (shadowFactor - luminance) * shadowMask * shadowAmount;
				adjustedLuminance -= (luminance - highlightFactor) * highlightMask * highlightAmount;

				// Preserve chromaticity (prevent color shifting)
				float finalFactor = adjustedLuminance / Max(luminance, 0.001f);

				// Apply shifts, denormalize, clip and update pixel
				row[x].R = DeNormalizeClip16(r * finalFactor);
				row[x].G = DeNormalizeClip16(g * finalFactor);
				row[x].B = DeNormalizeClip16(b * finalFactor);
#endif
			}
		});
	}

	public static void ApplyColorTemperature(this Image<Rgb48> image, float temperature)
	{
		// Clamp the temperature value to a reasonable range (-100 to 100)
		temperature = Math.Clamp(temperature, -100f, 100f);

		// Scale the temperature to a fractional shift
		float tempShift = temperature / 100f;
		float blueShift = tempShift * 0.5f;

		// Build the 5x4 Color Matrix
		// Columns: R, G, B, A, Offset
		var matrix = new SixLabors.ImageSharp.ColorMatrix(
			1f + tempShift, 0f, 0f, 0f,
			0f, 1f + tempShift, 0f, 0f,
			0f, 0f, 1f - blueShift, 0f,
			0f, 0f, 0f, 1f,
			0f, 0f, 0f, 0f);

		// Apply the matrix as a filter
		image.Mutate(ctx => ctx.Filter(matrix));
	}

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

	// contrastAmount == from 1.0 to 2.5  -- 1.0 -> No Change 
	// blurAmount == sigma from 0.0 to 1.5 - 0.0 -> No blur 
	public static bool ApplyGlobalContrast(this Image<Rgb48> image, float contrastAmount, float blurAmount)
	{
		if (Math.Abs(contrastAmount - 1.0) > 0.01)
		{
			image.Mutate(x => x.Contrast(contrastAmount));
		}

		if (Math.Abs(blurAmount) > 0.05)
		{
			image.Mutate(x => x.GaussianBlur(blurAmount));
		}

		return true;
	}

	public static bool ApplySCurveContrast(this Image<Rgb48> image, float redAmount, float greenAmount, float blueAmount )
	{
		// PLACEHOLDER 
		return true;
	}


	//     A value of 0 is completely un-saturated. A value of 1 leaves the input unchanged.
	//     Other values are linear multipliers on the effect. Values of amount over 1 are
	//     allowed, providing super-saturated results
	public static bool ApplyGlobalSaturation(this Image<Rgb48> image, float saturationAmount)
	{
		if (Math.Abs(saturationAmount - 1.0) > 0.01)
		{
			image.Mutate(x => x.Saturate(saturationAmount));
		}

		return true;
	}

    //   sharpenAmoun: sigma: The 'sigma' value representing the weight of the blur.
    public static bool ApplyGlobalSharpen(this Image<Rgb48> image, float sharpenAmount)
    {
        if (Math.Abs(sharpenAmount) > 0.05)
        {
            image.Mutate(x => x.GaussianSharpen(sharpenAmount));
        }

        return true;
    }
}


/*

public void ApplySCurveContrast(Image<Rgba32> image)
{
	// Build an S-curve Lookup Table (LUT) from 0 to 255
	byte[] lut = CreateSCurveLut();

	image.Mutate(ctx => ctx.ProcessPixelRowsAsMemory(row =>
	{
		for (int i = 0; i < row.Length; i++)
		{
			ref Rgba32 pixel = ref row.Span[i];
			
			// Apply the S-Curve to R, G, and B independently 
			// but uniformly to avoid color shifts
			pixel.R = lut[pixel.R];
			pixel.G = lut[pixel.G];
			pixel.B = lut[pixel.B];
		}
	}));
}

private byte[] CreateSCurveLut()
{
	byte[] lut = new byte[256];
	for (int i = 0; i < 256; i++)
	{
		// Normalize the 0-255 value to a 0.0 - 1.0 range
		double normalizedVal = i / 255.0;
		
		// Mathematical S-Curve (Sigmoid function)
		// Adjust the multiplier (e.g., 2.5) to alter contrast intensity
		double contrastMultiplier = 2.5;
		double sCurveValue = 1.0 / (1.0 + Math.Exp(-contrastMultiplier * (normalizedVal - 0.5)));

		// Denormalize back to 0-255
		lut[i] = (byte)Math.Clamp(sCurveValue * 255, 0, 255);
	}
	return lut;
}
*/