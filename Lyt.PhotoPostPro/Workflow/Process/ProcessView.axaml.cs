namespace Lyt.PhotoPostPro.Workflow.Process;

public partial class ProcessView : View
{
    protected override void OnDataContextChanged(object? sender, EventArgs e) { }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            new ToolbarCommandMessage(ToolbarCommandMessage.ToolbarCommand.BackToWindowed).Publish();
        }

        base.OnKeyDown(e);
    }
}