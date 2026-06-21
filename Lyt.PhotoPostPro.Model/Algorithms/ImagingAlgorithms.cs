namespace Lyt.PhotoPostPro.Model.Algorithms;

using static System.Math;
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
			1f + tempShift, 0f,             0f,             0f,
			0f,             1f + tempShift, 0f,             0f,
			0f,             0f,             1f - blueShift, 0f,
			0f,             0f,             0f,             1f,
			0f,             0f,             0f,             0f);

		// Apply the matrix as a filter
		image.Mutate(ctx => ctx.Filter(matrix));
	}

	//public static int[] GetRgbFromTemperature(double temperature)
	//{
		//// Temperature must fit between 1000 and 40000 degrees.
		//temperature = MathUtils.Clamp(temperature, 1000, 40000);

		//// All calculations require temperature / 100, so only do the conversion once.
		//temperature /= 100;

		//// Compute each color in turn.
		//int red, green, blue;

		//// First: red.
		//if (temperature <= 66)
		//{
		//    red = 255;
		//}
		//else
		//{
		//    // Note: the R-squared value for this approximation is 0.988.
		//    red = (int)(329.698727446 * (Math.Pow(temperature - 60, -0.1332047592)));
		//    red = MathUtils.Clamp(red, 0, 255);
		//}

		//// Second: green.
		//if (temperature <= 66)
		//{
		//    // Note: the R-squared value for this approximation is 0.996.
		//    green = (int)(99.4708025861 * Math.Log(temperature) - 161.1195681661);
		//}
		//else
		//{
		//    // Note: the R-squared value for this approximation is 0.987.
		//    green = (int)(288.1221695283 * (Math.Pow(temperature - 60, -0.0755148492)));
		//}

		//green = MathUtils.Clamp(green, 0, 255);

		//// Third: blue.
		//if (temperature >= 66)
		//{
		//    blue = 255;
		//}
		//else if (temperature <= 19)
		//{
		//    blue = 0;
		//}
		//else
		//{
		//    // Note: the R-squared value for this approximation is 0.998.
		//    blue = (int)(138.5177312231 * Math.Log(temperature - 10) - 305.0447927307);
		//    blue = MathUtils.Clamp(blue, 0, 255);
		//}

		//return new[] { red, green, blue };
	//}
	/*
	https://tannerhelland.com/2012/09/18/convert-temperature-rgb-algorithm-code.html


	Start with a temperature, in Kelvin, somewhere between 1000 and 40000.  (Other values may work,
	 but I can't make any promises about the quality of the algorithm's estimates above 40000 K.)
	Note also that the temperature and color variables need to be declared as floating-point.

	Set Temperature = Temperature \ 100

	Calculate Red:

	If Temperature <= 66 Then
		Red = 255
	Else
		Red = Temperature - 60
		Red = 329.698727446 * (Red ^ -0.1332047592)
		If Red < 0 Then Red = 0
		If Red > 255 Then Red = 255
	End If

	Calculate Green:

	If Temperature <= 66 Then
		Green = Temperature
		Green = 99.4708025861 * Ln(Green) - 161.1195681661
		If Green < 0 Then Green = 0
		If Green > 255 Then Green = 255
	Else
		Green = Temperature - 60
		Green = 288.1221695283 * (Green ^ -0.0755148492)
		If Green < 0 Then Green = 0
		If Green > 255 Then Green = 255
	End If

	Calculate Blue:

	If Temperature >= 66 Then
		Blue = 255
	Else

		If Temperature <= 19 Then
			Blue = 0
		Else
			Blue = Temperature - 10
			Blue = 138.5177312231 * Ln(Blue) - 305.0447927307
			If Blue < 0 Then Blue = 0
			If Blue > 255 Then Blue = 255
		End If

	End If

	'Given a temperature (in Kelvin), estimate an RGB equivalent
	Private Sub getRGBfromTemperature(ByRef r As Long, ByRef g As Long, ByRef b As Long, ByVal tmpKelvin As Long)

		Static tmpCalc As Double

		'Temperature must fall between 1000 and 40000 degrees
		If tmpKelvin < 1000 Then tmpKelvin = 1000
		If tmpKelvin > 40000 Then tmpKelvin = 40000

		'All calculations require tmpKelvin \ 100, so only do the conversion once
		tmpKelvin = tmpKelvin \ 100

		'Calculate each color in turn

		'First: red
		If tmpKelvin <= 66 Then
			r = 255
		Else
			'Note: the R-squared value for this approximation is .988
			tmpCalc = tmpKelvin - 60
			tmpCalc = 329.698727446 * (tmpCalc ^ -0.1332047592)
			r = tmpCalc
			If r < 0 Then r = 0
			If r > 255 Then r = 255
		End If

		'Second: green
		If tmpKelvin <= 66 Then
			'Note: the R-squared value for this approximation is .996
			tmpCalc = tmpKelvin
			tmpCalc = 99.4708025861 * Log(tmpCalc) - 161.1195681661
			g = tmpCalc
			If g < 0 Then g = 0
			If g > 255 Then g = 255
		Else
			'Note: the R-squared value for this approximation is .987
			tmpCalc = tmpKelvin - 60
			tmpCalc = 288.1221695283 * (tmpCalc ^ -0.0755148492)
			g = tmpCalc
			If g < 0 Then g = 0
			If g > 255 Then g = 255
		End If

		'Third: blue
		If tmpKelvin >= 66 Then
			b = 255
		ElseIf tmpKelvin <= 19 Then
			b = 0
		Else
			'Note: the R-squared value for this approximation is .998
			tmpCalc = tmpKelvin - 10
			tmpCalc = 138.5177312231 * Log(tmpCalc) - 305.0447927307

			b = tmpCalc
			If b < 0 Then b = 0
			If b > 255 Then b = 255
		End If

	End Sub


		using System;
	using System.Drawing;
	using System.Drawing.Imaging;

	public static class ColorTemperatureFilter
	{
		public static Bitmap AdjustTemperature(Bitmap sourceImage, float kelvin)
		{
			// 1. Convert Kelvin to a 0-255 scaling factor for color channels
			float temp = kelvin / 100f;
			float red, green, blue;

			// Calculate Red
			if (temp <= 66)
			{
				red = 255;
			}
			else
			{
				red = temp - 60f;
				red = (float)(329.698727446 * Math.Pow(red, -0.1332047592));
			}

			// Calculate Green
			if (temp <= 66)
			{
				green = temp;
				green = (float)(99.4708025861 * Math.Log(green) - 161.1195681661);
			}
			else
			{
				green = temp - 60f;
				green = (float)(288.1221695283 * Math.Pow(green, -0.0755148492));
			}

			// Calculate Blue
			if (temp >= 66)
			{
				blue = 255;
			}
			else if (temp <= 19)
			{
				blue = 0;
			}
			else
			{
				blue = temp - 10f;
				blue = (float)(138.5177312231 * Math.Log(blue) - 305.0447927307);
			}

			// Clamp values to [0, 255]
			red = Math.Max(0, Math.Min(255, red));
			green = Math.Max(0, Math.Min(255, green));
			blue = Math.Max(0, Math.Min(255, blue));

			// 2. Process image with LockBits for fast pixel manipulation
			Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);
			Rectangle rect = new Rectangle(0, 0, resultImage.Width, resultImage.Height);

			BitmapData srcData = sourceImage.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			BitmapData dstData = resultImage.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			int bytesPerPixel = 4;
			int stride = srcData.Stride;
			IntPtr srcScan0 = srcData.Scan0;
			IntPtr dstScan0 = dstData.Scan0;

			unsafe
			{
				byte* src = (byte*)(void*)srcScan0;
				byte* dst = (byte*)(void*)dstScan0;

				for (int y = 0; y < sourceImage.Height; y++)
				{
					for (int x = 0; x < sourceImage.Width; x++)
					{
						int index = (y * stride) + (x * bytesPerPixel);

						// src[index + 0] = Blue, [index + 1] = Green, [index + 2] = Red, [index + 3] = Alpha

						// Apply the temperature scaling
						dst[index + 0] = (byte)Math.Min(255, src[index + 0] * (blue / 255.0));
						dst[index + 1] = (byte)Math.Min(255, src[index + 1] * (green / 255.0));
						dst[index + 2] = (byte)Math.Min(255, src[index + 2] * (red / 255.0));
						dst[index + 3] = src[index + 3]; // Maintain alpha
					}
				}
			}

			sourceImage.UnlockBits(srcData);
			resultImage.UnlockBits(dstData);

			return resultImage;
		}
	}
		*/

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