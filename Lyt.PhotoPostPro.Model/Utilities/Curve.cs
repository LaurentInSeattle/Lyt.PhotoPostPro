namespace Lyt.PhotoPostPro.Model.Utilities;

public sealed class Curve
{
    public const int CurveSize = 320;

    public enum CurveKind
    {
        Unknown = 0,

        GammaLut,
    }

    public CurveKind Kind { get; private set; }

    public float[] Points { get; private set; }

    public Curve(float[] gammaLut)
    {
        this.Kind = CurveKind.GammaLut;
        if (gammaLut.Length != ImagingAlgorithms.LutSize)
        {
            throw new ArgumentException("Not a gamma LUT.");
        }

        this.Points = new float[CurveSize];
        for (int i = 0; i < CurveSize; ++i)
        {
            float x = ( float ) i / ( CurveSize - 1);
            float y = ImagingAlgorithms.LutLookup(gammaLut, x);
            this.Points[i] = y;
        }
    }
}
