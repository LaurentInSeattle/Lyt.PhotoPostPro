namespace Lyt.PhotoPostPro.Panes.MetadataPane; 

public partial class EditKeywordsDialog : View 
{
    public EditKeywordsDialog() => this.Loaded += (_, _) => this.KeywordsTextBox.Focus(); 
} 
