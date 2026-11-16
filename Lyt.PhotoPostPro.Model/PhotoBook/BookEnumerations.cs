namespace Lyt.PhotoPostPro.Model.PhotoBook;

//      public static PageSize Letter { get; } = new(612, 792);
//      public static PageSize A4 { get; } = new PageSize(595f, 842f);
//      public const int PointsPerInch = 72;

[JsonConverter(typeof(JsonStringEnumConverter<BookPage>))]
public enum BookPage
{
    UsLetter,
    EuroA4,
}


[JsonConverter(typeof(JsonStringEnumConverter<BookFormat>))]
public enum BookFormat
{
    Landscape,
    Portrait,

    // Maybe later 
    // Square,
}

// New enum required for serialization 
[JsonConverter(typeof(JsonStringEnumConverter<PageBackground>))]
public enum PageBackground
{
    White,
    Black,

    // Maybe later 
    // Ivory,  // #FEFEF2
    // Licorice, // #1A_11_10
}

[JsonConverter(typeof(JsonStringEnumConverter<PageLayout>))]
public enum PageLayout
{
    Empty,
    FullPageImage,
    SingleImage,
    SingleImageWithLegend,
}

// New enum required for serialization 
[JsonConverter(typeof(JsonStringEnumConverter<BookTextAlignment>))]
public enum BookTextAlignment
{
    Left, 
    Center, 
    Right,
}

// New enum required for serialization 
[JsonConverter(typeof(JsonStringEnumConverter<BookImageStretch>))]
public enum BookImageStretch
{
    Fit,
    Stretch,
}

[JsonConverter(typeof(JsonStringEnumConverter<BookFontStyle>))]
public enum BookFontStyle
{
    Regular = 0,
    Bold = 1,
    Italic = 2,
    BoldItalic = 3
}

[JsonConverter(typeof(JsonStringEnumConverter<BookResolution>))]
public enum BookResolution
{
    Print = 0,
    Display = 1,
    Thumbnails = 2,
}
