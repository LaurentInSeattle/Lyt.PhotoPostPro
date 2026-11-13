namespace Lyt.PhotoPostPro.Model;

[JsonSourceGenerationOptions(
    WriteIndented = true, 
    AllowTrailingCommas = true, 
    IgnoreReadOnlyProperties = true, 
    IgnoreReadOnlyFields = true 
    )]

// Library
[JsonSerializable(typeof(PhotoPostProModel))]
[JsonSerializable(typeof(Metadata))]
[JsonSerializable(typeof(ProcessParameters))]

// Tools
[JsonSerializable(typeof(LibraryFolder))]
[JsonSerializable(typeof(Signature))]
[JsonSerializable(typeof(Watermark))]
[JsonSerializable(typeof(ImageExport))]

[JsonSerializable(typeof(LibraryFolderCollection))]
[JsonSerializable(typeof(SignaturesCollection))]
[JsonSerializable(typeof(WatermarksCollection))]
[JsonSerializable(typeof(ImageExportsCollection))]

[JsonSerializable(typeof(ExportAction))]
[JsonSerializable(typeof(OutputFormat))]
[JsonSerializable(typeof(ImageBorderStyle))]
[JsonSerializable(typeof(ImageBorderThickness))]
[JsonSerializable(typeof(SignatureLocation))]
[JsonSerializable(typeof(PppFontStyle))]

// PhotoBook
[JsonSerializable(typeof(Book))]
[JsonSerializable(typeof(Leaf))]
[JsonSerializable(typeof(Page))]
[JsonSerializable(typeof(PageBase))]
[JsonSerializable(typeof(TextSlot))]
[JsonSerializable(typeof(ImageSlot))]
[JsonSerializable(typeof(Slot))]
[JsonSerializable(typeof(SlotPosition))]
[JsonSerializable(typeof(SlotTextStyle))]
[JsonSerializable(typeof(FrontCover))]
[JsonSerializable(typeof(BackCover))]
[JsonSerializable(typeof(BookFormat))]
[JsonSerializable(typeof(PageBackground))]


public partial class AppJsonContext : JsonSerializerContext { }
