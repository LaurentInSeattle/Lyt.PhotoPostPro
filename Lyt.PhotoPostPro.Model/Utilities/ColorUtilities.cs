namespace Lyt.PhotoPostPro.Model.Utilities;

using static ImagingUtilities;
using static Math;

public static class ColorUtilities
{
    public static void RgbToYiq(float r, float g, float b, out float y, out float i, out float q)
    {
        y = 0.229f * r + 0.587f * g + 0.114f * b;
        i = 0.595716f * r - 0.274453f * g - 0.321263f * b;
        q = 0.211456f * r - 0.522591f * g + 0.311135f * b;
    }

    public static void YiqToRgb(float y, float i, float q, out float r, out float g, out float b)
    {
        r = y + 0.9563f * i + 0.6210f * q;
        g = y - 0.2721f * i - 0.6474f * q;
        b = y - 1.1070f * i + 1.7046f * q;
    }

    public static void RgbToHsl(float r, float g, float b , out float h, out float s, out float l)
    {
        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        float delta = max - min;

        l = (max + min) / 2f;

        if (delta == 0)
        {
            h = s = 0f; // Achromatic (gray)
        }
        else
        {
            s = l > 0.5f ? delta / (2f - max - min) : delta / (max + min);

            if (max == r)
            {
                h = (g - b) / delta + (g < b ? 6f : 0f);
            }
            else if (max == g)
            {
                h = (b - r) / delta + 2f;
            }
            else
            {
                h = (r - g) / delta + 4f;
            }

            h /= 6f;
        }
    }

    public static void HslToRgb(float h, float s, float l, out ushort r, out ushort g, out ushort b)
    {
        if (s == 0f)
        {
            r = g = b = (byte)Math.Round(l * pixMaxF );
        }
        else
        {
            float q = l < 0.5f ? l * (1f + s) : l + s - l * s;
            float p = 2f * l - q;

            r = (ushort)Math.Round(HueToRgb(p, q, h + 1.0f / 3.0f) * pixMaxF);
            g = (ushort)Math.Round(HueToRgb(p, q, h) * pixMaxF);
            b = (ushort)Math.Round(HueToRgb(p, q, h - 1.0f / 3.0f) * pixMaxF);
        }
    }

    private static float HueToRgb(float p, float q, float t)
    {
        if (t < 0f) t += 1f;
        if (t > 1f) t -= 1f;
        if (t < 1f / 6f) return p + (q - p) * 6f * t;
        if (t < 1f / 2f) return q;
        if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;

        return p;
    }
}
