namespace Lyt.PhotoPostPro.Model.PhotoBook;

public class BookImage
{
    public BookImage() { }

    public BookImage(string path) 
    {
        this.HighResPath = path;
        this.MediumResPath = path;
        this.LowResPath = path;
    }

    public Guid Id { get; set; } = Guid.NewGuid();

    // For final PDF for print 
    public string HighResPath { get; set; } = string.Empty;

    // For final PDF for display 
    public string MediumResPath { get; set; } = string.Empty;

    // Thumbnail for editing 
    public string LowResPath { get; set; } = string.Empty;
}
