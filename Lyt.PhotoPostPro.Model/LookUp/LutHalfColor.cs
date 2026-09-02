namespace Lyt.PhotoPostPro.Model.LookUp;

using static ImagingUtilities; 

/// <summary> A struct that represents a color using half floating point values.  </summary>
public struct LutHalfColor
{
    public Half R;
    public Half G;
    public Half B;

    public static LutHalfColor FromRgbInt(int red, int gre, int blu, float maxValue)
    {
        var lutColor = new LutHalfColor()
        {
            R = (Half)LutHalfColor.Remap(red, maxValue),
            G = (Half)LutHalfColor.Remap(gre, maxValue),
            B = (Half)LutHalfColor.Remap(blu, maxValue),
        };

        lutColor.Validate();
        return lutColor;
    }

    public static LutHalfColor FromRgbFloat(float red, float gre, float blu)
    {
        var lutColor = new LutHalfColor()
        {
            R = (Half)ClipF(red),
            G = (Half)ClipF(gre),
            B = (Half)ClipF(blu),
        };


        return lutColor;
    }

    public uint ToRgba()
    {
        uint red = (uint)(this.R * (Half)255.0f);
        uint gre = (uint)(this.G * (Half)255.0f);
        uint blu = (uint)(this.B * (Half)255.0f);
        uint alp = 255;
        return (alp << 24) | (red << 16) | (gre << 8) | blu;
    }

    public static LutHalfColor Lerp(LutHalfColor c1, LutHalfColor c2, Half alpha)
    {
        LutHalfColor.ValidateInterpolator(alpha);

        if (alpha < (Half)0.001f)
        {
            return c1;
        }
        else if (alpha > (Half)0.999f)
        {
            return c2;
        }

        Half deltaR = c2.R - c1.R;
        Half deltaG = c2.G - c1.G;
        Half deltaB = c2.B - c1.B;
        var lutColor = new LutHalfColor()
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
        if ((this.R > (Half)1.0f) || (this.G > (Half)1.0f) || (this.B > (Half)1.0f))
        {
            throw new Exception("invalid color");
        }

        if ((this.R < (Half)0.0f) || (this.G < (Half)0.0f) || (this.B < (Half)0.0f))
        {
            throw new Exception("invalid color");
        }
    }

    [Conditional("DEBUG")]
    private static void ValidateInterpolator(Half alpha)
    {
        if ((alpha > (Half)1.0f) || (alpha < (Half)0.0f))
        {
            throw new Exception("invalid interpolator");
        }
    }
}
