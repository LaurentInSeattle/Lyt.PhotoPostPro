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

    public Half[] Points { get; private set; }

    public Curve(Half[] gammaLut)
    {
        this.Kind = CurveKind.GammaLut;
        if (gammaLut.Length != ImagingAlgorithms.LutSize)
        {
            throw new ArgumentException("Not a gamma LUT.");
        }

        this.Points = new Half[CurveSize];
        for (int i = 0; i < CurveSize; ++i)
        {
            Half x = ( Half ) i / (Half) ( CurveSize - 1);
            Half y = ImagingAlgorithms.LutLookup(gammaLut, x);
            this.Points[i] = y;
        }
    }
}
