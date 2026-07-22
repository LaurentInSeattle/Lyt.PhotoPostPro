namespace Lyt.PhotoPostPro.Model.Algorithms;

public static partial class ImagingAlgorithms
{
	public static void ApplyColorTemperature(this Image<HalfVector4> image, float temperature)
	{
		// Clamp the temperature value to a reasonable range (-100 to 100)
		temperature = Math.Clamp(temperature, -100f, 100f);

		// Scale the temperature to a fractional shift
		float tempShift = temperature / 100f;
		float blueShift = tempShift * 0.5f;

		// Build the 5x4 Color Matrix
		var matrix = new SixLabors.ImageSharp.ColorMatrix(
			1f + tempShift, 0f, 0f, 0f,
			0f, 1f + tempShift, 0f, 0f,
			0f, 0f, 1f - blueShift, 0f,
			0f, 0f, 0f, 1f,
			0f, 0f, 0f, 0f);

		// Apply the matrix as a filter
		image.Mutate(ctx => ctx.Filter(matrix));
	}

	// contrastAmount == from 1.0 to 2.5  -- 1.0 -> No Change 
	// blurAmount == sigma from 0.0 to 1.5 - 0.0 -> No blur 
	// brightnessAmount comes from 0.0 to 0.5 => Add one for Img# 
	public static bool ApplyGlobalContrast(
		this Image<HalfVector4> image, float contrastAmount, float blurAmount, float brightnessAmount )
	{
		if (Math.Abs(contrastAmount - 1.0) > 0.01)
		{
			image.Mutate(x => x.Contrast(contrastAmount));
		}

		if (Math.Abs(blurAmount) > 0.01)
		{
			image.Mutate(x => x.GaussianBlur(blurAmount));
		}

		if (Math.Abs(brightnessAmount) > 0.01)
		{
			image.Mutate(x => x.Brightness(1.0f + brightnessAmount));
		}

		return true;
	}

	//     A value of 0 is completely un-saturated. A value of 1 leaves the input unchanged.
	//     Other values are linear multipliers on the effect. Values of amount over 1 are
	//     allowed, providing super-saturated results
	public static bool ApplyGlobalSaturation(this Image<HalfVector4> image, float saturationAmount)
	{
		if (Math.Abs(saturationAmount - 1.0) > 0.01)
		{
			image.Mutate(x => x.Saturate(saturationAmount));
		}

		return true;
	}

	//   sharpenAmount: sigma: The 'sigma' value representing the weight of the blur.
	public static bool ApplyGlobalSharpen(this Image<HalfVector4> image, float sharpenAmount)
	{
		if (Math.Abs(sharpenAmount) > 0.01)
		{
			image.Mutate(x => x.GaussianSharpen(sharpenAmount));
		}

		return true;
	}
}
