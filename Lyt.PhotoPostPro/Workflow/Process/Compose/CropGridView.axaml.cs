namespace Lyt.PhotoPostPro.Workflow.Process.Compose;

public partial class CropGridView : View
{
    private bool doNotUpdateOnLayoutUpdated;
    private double zoomFactor = 1.0; 

    public void Activate()
    {
        this.LayoutUpdated -= this.OnLayoutUpdated;
        this.LayoutUpdated += this.OnLayoutUpdated;
    }

    public void Deactivate() => this.LayoutUpdated -= this.OnLayoutUpdated;
    
    internal void ClearCrop()
    {
        if (this.DataContext is not CropGridViewModel cropGridViewModel)
        {
            return;
        }

        With.Flag(ref this.doNotUpdateOnLayoutUpdated, () =>
        {
            var zero = new GridLength(0, GridUnitType.Pixel);
            var cols = this.CropGrid.ColumnDefinitions;
            var left = cols[0];
            left.Width = zero;
            var right = cols[4];
            right.Width = zero;
            var rows = this.CropGrid.RowDefinitions;
            var top = rows[0];
            top.Height = zero;
            var bottom = rows[4];
            bottom.Height = zero;

            double dx = this.Bounds.Width;
            double dy = this.Bounds.Height;
            cropGridViewModel.OnCropRectangleChanged(0, 0, dx, dy);
        });
    }

    internal void SetCrop(int x, int y, int dx, int dy)
    {
        if (this.DataContext is not CropGridViewModel cropGridViewModel)
        {
            return;
        }
        try
        {
            With.Flag(ref this.doNotUpdateOnLayoutUpdated, () =>
            {
                double w = this.Bounds.Width;
                double h = this.Bounds.Height;
                if (w > 0.0 && h > 0.0)
                {
                    double rightWidth = w - x - dx; 
                    double bottomHeight = h - y - dy;
                    if ( rightWidth < 0.0 || bottomHeight < 0.0)
                    {
                        return; 
                        // if (Debugger.IsAttached) Debugger.Break();
                    }

                    var cols = this.CropGrid.ColumnDefinitions;
                    var left = cols[0];
                    left.Width = new GridLength(x, GridUnitType.Pixel);
                    var right = cols[4];
                    right.Width = new GridLength(rightWidth, GridUnitType.Pixel);
                    var rows = this.CropGrid.RowDefinitions;
                    var top = rows[0];
                    top.Height = new GridLength(y, GridUnitType.Pixel);
                    var bottom = rows[4];
                    bottom.Height = new GridLength(bottomHeight, GridUnitType.Pixel);

                    cropGridViewModel.OnCropRectangleChanged(x, y, dx, dy);
                }
            });
        }
        catch ( Exception ex) 
        {
            Debug.WriteLine(ex);
            if ( Debugger.IsAttached )  Debugger.Break() ;
        }
    }

    internal void Left(int delta)
    {
        With.Flag(ref this.doNotUpdateOnLayoutUpdated, () =>
        {
            var cols = this.CropGrid.ColumnDefinitions;
            var left = cols[0];
            GridLength width = left.Width;
            if (width.IsAbsolute)
            {
                double newValue = width.Value + delta;
                if (newValue < 0.0)
                {
                    newValue = 0.0;
                }

                left.Width = new GridLength(newValue, GridUnitType.Pixel);
                this.UpdateCropRectangleChanged();
            }
        });
    }

    internal void Right(int delta)
    {
        double dx = this.Bounds.Width;
        With.Flag(ref this.doNotUpdateOnLayoutUpdated, () =>
        {
            var cols = this.CropGrid.ColumnDefinitions;
            var right = cols[4];
            GridLength width = right.Width;
            if (width.IsAbsolute)
            {
                double newValue = width.Value + delta;
                if (newValue < 0.0)
                {
                    newValue = 0.0;
                }

                if (newValue > dx)
                {
                    newValue = dx;
                }

                right.Width = new GridLength(newValue, GridUnitType.Pixel);
                this.UpdateCropRectangleChanged();
            }
        });
    }

    internal void Top(int delta)
    {
        With.Flag(ref this.doNotUpdateOnLayoutUpdated, () =>
        {
            var rows = this.CropGrid.RowDefinitions;
            var top = rows[0];
            GridLength height = top.Height;
            if (height.IsAbsolute)
            {
                double newValue = height.Value + delta;
                if (newValue < 0.0)
                {
                    newValue = 0.0;
                }

                top.Height = new GridLength(newValue, GridUnitType.Pixel);
                this.UpdateCropRectangleChanged();
            }
        });
    }

    internal void Bottom(int delta)
    {
        double dy = this.Bounds.Height;
        With.Flag(ref this.doNotUpdateOnLayoutUpdated, () =>
        {
            var rows = this.CropGrid.RowDefinitions;
            var bottom = rows[4];
            GridLength height = bottom.Height;
            if (height.IsAbsolute)
            {
                double newValue = height.Value + delta;
                if (newValue < 0.0)
                {
                    newValue = 0.0;
                }
                if (newValue > dy)
                {
                    newValue = dy;
                }

                bottom.Height = new GridLength(newValue, GridUnitType.Pixel);
                this.UpdateCropRectangleChanged();
            }
        });
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        Viewbox? viewbox = this.FindParentControl<Viewbox>();
        if (viewbox is not null)
        {
            double imageWidth = this.Bounds.Width;
            double imageHeight = this.Bounds.Height;
            double zoom = 1.0; 
            if ( imageWidth > imageHeight )
            {
                // Landscape: use width for zoom factor 
                double controlWidth = viewbox.Bounds.Width;
                zoom = controlWidth / imageWidth;
            }
            else
            {
                // portrait : use height for zoom factor 
                double controlHeight = viewbox.Bounds.Height;
                zoom = controlHeight / imageHeight;
            }

            if (double.IsNormal(zoom))
            {
                // Do not try to update if we get NaN's or infinities 
                this.zoomFactor = zoom;
                double lineThickness = 2.0 / zoom;
                this.VerticalLeftSplitter.Width = lineThickness;
                this.VerticalRightSplitter.Width = lineThickness;
                this.HorizontalTopSplitter.Height = lineThickness;
                this.HorizontalBottomSplitter.Height = lineThickness;

                if (this.DataContext is CropGridViewModel cropGridViewModel)
                {
                    cropGridViewModel.CompositionSize = 1.0 / zoom;
                }
            } 
        }

        if (this.doNotUpdateOnLayoutUpdated)
        {
            return;
        }

        this.UpdateCropRectangleChanged();
    }

    private void UpdateCropRectangleChanged()
    {
        if (this.DataContext is not CropGridViewModel cropGridViewModel)
        {
            return;
        }

        var cols = this.CropGrid.ColumnDefinitions;
        var left = cols[0];
        var right = cols[4];
        var rows = this.CropGrid.RowDefinitions;
        var top = rows[0];
        var bottom = rows[4];
        double x = left.ActualWidth;
        double y = top.ActualHeight;
        double imageWidth = this.Bounds.Width;
        double imageHeight = this.Bounds.Height;

        // Note: Needs minus one! 
        double dx = imageWidth - right.ActualWidth - x - 1;
        double dy = imageHeight-  bottom.ActualHeight - y - 1;

        cropGridViewModel.OnCropRectangleChanged(x, y, dx, dy);
    }
}
