namespace Lyt.PhotoPostPro.Utilities;

internal sealed class HotKeys
{
    public static readonly HotKeys Instance = new();

    public void Set(Control control)
    {
        var topLevel = TopLevel.GetTopLevel(control);
        if (topLevel is not null)
        {
            topLevel.KeyDown += this.OnTopLevelKeyDown;
        }
    }

    public void Clear(Control control)
    {
        var topLevel = TopLevel.GetTopLevel(control);
        if (topLevel is not null)
        {
            topLevel.KeyDown -= this.OnTopLevelKeyDown;
        }
    }

    private void OnTopLevelKeyDown(object? sender, KeyEventArgs e)
    {
        if ((e.Key == Key.PageUp) ||
            (e.Key == Key.PageDown) ||
            (e.Key == Key.Home) ||
            (e.Key == Key.End) ||
            (e.Key == Key.Pause))
        {
            new HotKeyMessage(e.Key).Publish();
            e.Handled = true;
        }
    }
}
