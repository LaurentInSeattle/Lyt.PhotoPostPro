namespace Lyt.PhotoPostPro.Workflow.Process.Compose;

public partial class CropGridView : View
{
    private bool doNotUpdate;

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

        this.doNotUpdate = true;
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
        }
        this.doNotUpdate = false;
    }

    internal void SetCrop(int x, int y, int dx, int dy)
    {
        if (this.DataContext is not CropGridViewModel cropGridViewModel)
        {
            return;
        }

        this.doNotUpdate = true;
        {
            double w = this.Bounds.Width;
            double h = this.Bounds.Height;
            if (w > 0.0 && h > 0.0)
            {
                var cols = this.CropGrid.ColumnDefinitions;
                var left = cols[0];
                left.Width = new GridLength(x, GridUnitType.Pixel);
                var right = cols[4];
                right.Width = new GridLength(w - x - dx, GridUnitType.Pixel);
                var rows = this.CropGrid.RowDefinitions;
                var top = rows[0];
                top.Height = new GridLength(x, GridUnitType.Pixel);
                var bottom = rows[4];
                bottom.Height = new GridLength(h - y - dy, GridUnitType.Pixel); ;

                cropGridViewModel.OnCropRectangleChanged(x, y, dx, dy);
            } 
        }

        this.doNotUpdate = false;
    }


    internal void Left(int delta)
    {
        this.doNotUpdate = true;
        {
            var cols = this.CropGrid.ColumnDefinitions;
            var left = cols[0];
            GridLength width = left.Width;
            if (width.IsAbsolute)
            {
                double newValue = width.Value + delta; 
                if( newValue < 0.0)
                {
                    newValue = 0.0;
                }

                left.Width = new GridLength(newValue, GridUnitType.Pixel);
                this.Update();
            }
        }

        this.doNotUpdate = false;
    }

    internal void Right(int delta)
    {
        double dx = this.Bounds.Width;
        this.doNotUpdate = true;
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
                this.Update();
            }
        }

        this.doNotUpdate = false;
    }

    internal void Top(int delta)
    {
        this.doNotUpdate = true;
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
                this.Update();
            }
        }

        this.doNotUpdate = false;
    }

    internal void Bottom(int delta)
    {
        double dy = this.Bounds.Height;
        this.doNotUpdate = true;
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
                this.Update();
            }
        }

        this.doNotUpdate = false;
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        // Debug.WriteLine(" CropGrid View: OnLayoutUpdated"); 

        if (this.doNotUpdate)
        {
            return;
        }

        this.Update();
    }

    private void Update()
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
        double dx = imageWidth - right.ActualWidth - x ;
        double dy = imageHeight-  bottom.ActualHeight - y ;

        cropGridViewModel.OnCropRectangleChanged(x, y, dx, dy);
    }
}
