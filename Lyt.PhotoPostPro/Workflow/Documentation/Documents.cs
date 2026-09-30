namespace Lyt.PhotoPostPro.Workflow.Documentation;

public sealed class Document
{
    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string ResourcePath { get; set; } = string.Empty;

    public Bitmap? FirstPage { get; set; }
}

public sealed class Documents
{
#pragma warning disable CA2211 
    // Non-constant fields should not be visible
    
    public static List<Document> List =
    [
        new Document()
        {
            Title = "Photo Editing",
            Summary = "A comprehensive guide to Photo Editing",
            ResourcePath = "Editing.pdf",
        },

        new Document()
        {
            Title = "User Guide",
            Summary = "This is the User Guide",
            ResourcePath = "Doc_en-US.pdf",
        },
    ];
#pragma warning restore CA2211 
}
