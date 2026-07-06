namespace Lyt.PhotoPostPro.Shell;

public partial class ShellView : UserControl, IView
{
    public ShellView()
    {
        this.InitializeComponent();
        this.Loaded += (s, e) =>
        {
            if (this.DataContext is not null && this.DataContext is ViewModel viewModel)
            {
                viewModel.OnViewLoaded();
            }

            var topLevel = TopLevel.GetTopLevel(this);
            if ( topLevel is not null)
            {
                topLevel.KeyDown += this.OnTopLevelKeyDown;
            }
        };
    }

    private void OnTopLevelKeyDown(object? sender, KeyEventArgs e)
    {
        if (this.DataContext is null || this.DataContext is not ShellViewModel shellViewModel)
        {
            return;
        }

        if (( e.Key == Key.PageUp ) || 
            (e.Key == Key.PageDown) || 
            (e.Key == Key.Home) || 
            (e.Key == Key.End) ||
            (e.Key== Key.Pause))
        {
            shellViewModel.HandleShortcut(e.Key); 
            e.Handled = true; 
        }
    }
}
