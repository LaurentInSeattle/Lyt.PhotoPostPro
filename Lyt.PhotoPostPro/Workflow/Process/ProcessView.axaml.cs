namespace Lyt.PhotoPostPro.Workflow.Process;

public partial class ProcessView : View
{
    public ProcessView()
    {
        this.KeyDown += this.OnKeyDown;    
    }

    protected override void OnDataContextChanged(object? sender, EventArgs e) { }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            new ToolbarCommandMessage(ToolbarCommandMessage.ToolbarCommand.BackToWindowed).Publish();
        }
        else if ((e.Key == Key.PageUp) ||
                (e.Key == Key.PageDown) ||
                (e.Key == Key.Home) ||
                (e.Key == Key.End) ||
                (e.Key == Key.Pause))
        {
            if (this.DataContext is null || this.DataContext is not ProcessViewModel processViewModel)
            {
                return;
            }

            processViewModel.HandleShortcut(e.Key);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

}