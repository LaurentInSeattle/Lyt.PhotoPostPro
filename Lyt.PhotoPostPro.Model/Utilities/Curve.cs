namespace Lyt.PhotoPostPro.Model.Utilities;

public sealed class Curve
{
    public enum CurveKind
    {
        Unknown = 0,

        GammaLut,
    }

    public CurveKind Kind { get; private set; }

    public float[] Points { get; private set; }

    public Curve(ushort[] gammaLut)
    {
        this.Kind = CurveKind.GammaLut;
        if (gammaLut.Length != 65536)
        {
            throw new ArgumentException("Not a gamma LUT.");
        }

        this.Points = new float[512];
        const int stepK = 128;
        int k = 0;
        for (int i = 0; i < 512; ++i) 
        {
            ushort value = gammaLut[k];
            k += stepK;
            this.Points[i] = (float) value / 65536.0f;
        }
    }
}
