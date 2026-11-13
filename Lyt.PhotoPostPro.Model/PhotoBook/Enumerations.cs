namespace Lyt.PhotoPostPro.Model.PhotoBook;

//      public static PageSize Letter { get; } = new(612, 792);
//      public static PageSize A4 { get; } = new PageSize(595f, 842f);
//      public const int PointsPerInch = 72;

[JsonConverter(typeof(JsonStringEnumConverter<BookFormat>))]
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
}

