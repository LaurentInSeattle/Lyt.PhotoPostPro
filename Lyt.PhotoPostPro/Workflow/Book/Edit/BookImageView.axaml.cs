namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

public partial class BookImageView : View
{
    protected override void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (this.DataContext is BookImageViewModel bookImageViewModel)
        {
            if (!bookImageViewModel.IsGhost)
            {
                // Don't do that for the ghost view 
                base.OnDataContextChanged(sender, e);
            }
        }
    }
}
