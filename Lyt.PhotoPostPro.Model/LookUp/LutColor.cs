namespace Lyt.PhotoPostPro.Model.LookUp;

using static ImagingUtilities; 

/// <summary> A class that represents a color using floating point values.  </summary>
public sealed class LutColor
{
    public float R;
    public float G;
    public float B;

    public static LutColor FromRgbInt(int red, int gre, int blu, float maxValue)
    {
        var lutColor = new LutColor()
        {
            R = LutColor.Remap(red, maxValue),
            G = LutColor.Remap(gre, maxValue),
            B = LutColor.Remap(blu, maxValue),
        };

        lutColor.Validate();
        return lutColor;
    }

    public static LutColor FromRgbFloat(float red, float gre, float blu)
    {
        var lutColor = new LutColor()
        {
            R = ClipF(red),
            G = ClipF(gre),
            B = ClipF(blu),
        };


        return lutColor;
    }

    public uint ToRgba()
    {
        uint red = (uint)(this.R * 255.0f);
        uint gre = (uint)(this.G * 255.0f);
        uint blu = (uint)(this.B * 255.0f);
        uint alp = 255;
        return (alp << 24) | (red << 16) | (gre << 8) | blu;
    }

    public static LutColor Lerp(LutColor c1, LutColor c2, float alpha)
    {
        LutColor.ValidateInterpolator(alpha);

        if (alpha < 0.001f)
        {
            return c1;
        }
        else if (alpha > 0.999f)
        {
            return c2;
        }

        float deltaR = c2.R - c1.R;
        float deltaG = c2.G - c1.G;
        float deltaB = c2.B - c1.B;
        var lutColor = new LutColor()
        {
            R = c1.R + deltaR * alpha,
            G = c1.G + deltaG * alpha,
            B = c1.B + deltaB * alpha,
        };

        lutColor.Validate();
        return lutColor;
    }

    public override string ToString() => string.Format(" {0:F2} , {1:F2} , {2:F2} ", this.R, this.G, this.B);

    private static float Remap(int value, float maxValue) => (float)value / maxValue;

    [Conditional("DEBUG")]
    public void Validate()
    {
        if ((this.R > 1.0f) || (this.G > 1.0f) || (this.B > 1.0f))
        {
            throw new Exception("invalid color");
        }

        if ((this.R < 0.0f) || (this.G < 0.0f) || (this.B < 0.0f))
        {
            throw new Exception("invalid color");
        }
    }

    [Conditional("DEBUG")]
    private static void ValidateInterpolator(float alpha)
    {
        if ((alpha > 1.0f) || (alpha < 0.0f))
        {
            throw new Exception("invalid interpolator");
        }
    }
}
