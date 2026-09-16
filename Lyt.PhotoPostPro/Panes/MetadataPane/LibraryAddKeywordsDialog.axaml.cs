namespace Lyt.PhotoPostPro.Panes.MetadataPane; 

public partial class LibraryAddKeywordsDialog : View 
{
    public LibraryAddKeywordsDialog() => this.Loaded += (_, _) => this.KeywordsTextBox.Focus();
}
