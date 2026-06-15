namespace Lyt.PhotoPostPro.Workflow.Process.Compose.CompositionGuides;

public partial class FrameCG : UserControl
{
    public FrameCG()
    {
        this.InitializeComponent();
        this.LayoutUpdated += this.OnLayoutUpdated;
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        var bounds = this.MainGrid.Bounds;
        double length = 0.12 * Math.Min(bounds.Width, bounds.Height);
        var gridLength = new GridLength(length, GridUnitType.Pixel);
        var cols = this.MainGrid.ColumnDefinitions;
        cols[0].Width = gridLength;
        cols[4].Width = gridLength;
        var rows = this.MainGrid.RowDefinitions;
        rows[0].Height = gridLength;
        rows[4].Height = gridLength;
    }
}