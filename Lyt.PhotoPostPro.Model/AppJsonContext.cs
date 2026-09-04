namespace Lyt.PhotoPostPro.Model;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(PhotoPostProModel))]
[JsonSerializable(typeof(Metadata))]
[JsonSerializable(typeof(ProcessParameters))]

[JsonSerializable(typeof(Signature))]
[JsonSerializable(typeof(Watermark))]
// With ending S 
[JsonSerializable(typeof(SignaturesCollection))]
[JsonSerializable(typeof(WatermarksCollection))]

[JsonSerializable(typeof(ExportAction))]
[JsonSerializable(typeof(OutputFormat))]
[JsonSerializable(typeof(ImageBorderStyle))]
[JsonSerializable(typeof(ImageBorderThickness))]
[JsonSerializable(typeof(SignatureLocation))]

[JsonSerializable(typeof(PppFontStyle))]

public partial class AppJsonContext : JsonSerializerContext
{
}
