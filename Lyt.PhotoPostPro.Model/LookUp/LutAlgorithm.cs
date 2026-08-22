namespace Lyt.PhotoPostPro.Model.LookUp;

[JsonConverter(typeof(JsonStringEnumConverter<LutAlgorithm>))]
public enum LutAlgorithm
{
    Swizzle,
    TriLinear,
    Tetrahedral,
}