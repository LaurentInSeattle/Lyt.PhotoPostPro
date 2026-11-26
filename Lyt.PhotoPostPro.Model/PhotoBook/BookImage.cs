namespace Lyt.PhotoPostPro.Model.PhotoBook;

public class BookImage
{
    public enum SuitableForPrint
    {
        Unknown,
        Optimal,
        Minimal,
        Unsuitable,
    }

    public const int OptimalTargetDpiForPrint = 300;

    public const int MinimalTargetDpiForPrint = 275;

    public BookImage() { }

    public Guid Id { get; set; } = Guid.NewGuid();

    // For final PDF for print 
    public string HighResPath { get; set; } = string.Empty;

    // For final PDF for display 
    public string MediumResPath { get; set; } = string.Empty;

    // Thumbnail for editing 
    public string LowResPath { get; set; } = string.Empty;

    public int HighResWidth { get; set; } = 0;

    public int HighResHeight { get; set; } = 0;

    public SuitableForPrint PrintableLevel(float slotWidth, float slotHeight)
    {
        if (slotWidth == 0 || slotHeight == 0)
        {
            return BookImage.SuitableForPrint.Unknown;
        }

        if (this.HighResWidth == 0 || this.HighResHeight == 0)
        {
            return BookImage.SuitableForPrint.Unknown;
        }

        float widthInches = slotWidth / 72.0f;
        float heightInches = slotHeight / 72.0f;
        if ((this.HighResWidth / widthInches > OptimalTargetDpiForPrint) &&
            (this.HighResHeight / heightInches > OptimalTargetDpiForPrint))
        {
            return BookImage.SuitableForPrint.Optimal;
        }

        if ((this.HighResWidth / widthInches > MinimalTargetDpiForPrint) &&
            (this.HighResHeight / heightInches > MinimalTargetDpiForPrint))
        {
            return BookImage.SuitableForPrint.Minimal;
        }

        return BookImage.SuitableForPrint.Unsuitable;
    }
}
