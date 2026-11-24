namespace Lyt.PhotoPostPro.Controls;

using global::Avalonia.Controls.Primitives;
using global::Avalonia.Controls.Templates;
using global::Avalonia.Input;
using global::Avalonia.Layout;
using global::Avalonia.Media;

using Lyt.PhotoPostPro.Model.PhotoBook.Slots;

public class ResizeAdorner : Grid
{
    private const double HandleSize = 24;
    private const double MinSize = 50;

    private readonly Control target;
    private readonly Thumb topLeft;
    private readonly Thumb bottomRight;
    private readonly double pageWidth;
    private readonly double pageHeight;
    private readonly int snaps;

    private double top;
    private double left;
    private double width;
    private double height;

    private Vector vector;
    private double x;
    private double y;
    private double dx;
    private double dy;

    public ResizeAdorner(Control target, double pageWidth, double pageHeight, int snaps = 5)
    {
        this.target = target;
        this.pageWidth = pageWidth;
        this.pageHeight = pageHeight;
        this.snaps = snaps;

        // Prevent the AdornerLayer from clipping handles that extend past the target's bounds
        AdornerLayer.SetIsClipEnabled(this, false);

        // Add two corner handles
        this.topLeft =
            this.AddHandle(
                HorizontalAlignment.Left, VerticalAlignment.Top, StandardCursorType.SizeAll, this.ResizeTopLeft);
        this.bottomRight =
            this.AddHandle(
                HorizontalAlignment.Right, VerticalAlignment.Bottom, StandardCursorType.Cross, this.ResizeBottomRight);
    }

    private Thumb AddHandle(
        HorizontalAlignment hAlign,
        VerticalAlignment vAlign,
        StandardCursorType cursor,
        Action<Vector> onDrag)
    {
        var thumb = new Thumb
        {
            Width = HandleSize,
            Height = HandleSize,
            HorizontalAlignment = hAlign,
            VerticalAlignment = vAlign,
            Cursor = new Cursor(cursor),
            // Offset margin by half the handle size so it centers on the corner
            Margin = new Thickness(
                hAlign == HorizontalAlignment.Left ? -HandleSize / 2 : 0,
                vAlign == VerticalAlignment.Top ? -HandleSize / 2 : 0,
                hAlign == HorizontalAlignment.Right ? -HandleSize / 2 : 0,
                vAlign == VerticalAlignment.Bottom ? -HandleSize / 2 : 0),
            // Explicitly define the visual template for the corner handle
            Template = new FuncControlTemplate<Thumb>((_, _) => new Border
            {
                Background = Brushes.Transparent,
                BorderBrush = Brushes.DodgerBlue,
                BorderThickness = new Thickness(2.0),
                CornerRadius = new CornerRadius(2) // slightly rounded corners
            })
        };

        thumb.DragStarted += (_, _) =>
        {
            Debug.WriteLine(" DragStarted");
            this.left = this.target.GetValue(Canvas.LeftProperty);
            this.top = this.target.GetValue(Canvas.TopProperty);
            this.width = this.target.Bounds.Width;
            this.height = this.target.Bounds.Height;
            this.vector = new();
            this.x = this.left;
            this.y = this.top;
            this.dx = this.width;
            this.dy = this.height;
            var sp = new SlotPosition((float)x, (float)y, (float)dx, (float)dy);
            Debug.WriteLine(sp.ToString());
            this.UpdatePositionable();
        };

        thumb.DragDelta += (_, e) => onDrag(e.Vector);

        thumb.DragCompleted += (_, _) =>
        {
            this.UpdatePositionable(isFinal: true);
        };

        this.Children.Add(thumb);
        return thumb;
    }

    private void ResizeTopLeft(Vector delta)
    {
        this.vector += delta;
        this.x = this.left + vector.X;
        this.y = this.top + vector.Y;
        this.Constrain();
        this.UpdatePositionable();
    }

    private void ResizeBottomRight(Vector delta)
    {
        this.vector += delta;
        this.dx = Math.Max(MinSize, this.width + vector.X);
        this.dy = Math.Max(MinSize, this.height + vector.Y);
        this.Constrain();
        this.UpdatePositionable();
    }

    private void UpdatePositionable(bool isFinal = false)
    {
        int RoundToSnaps(double dbl) => this.snaps * (int)Math.Round(dbl / this.snaps);

        if (this.target.DataContext is IPositionable positionable)
        {
            if (isFinal)
            {
                // Snap only on the final move to avoid the slot border to jerk
                int ix = RoundToSnaps(this.x);
                int iy = RoundToSnaps(this.y);
                int idx = RoundToSnaps(this.dx);
                int idy = RoundToSnaps(this.dy);
                var position = new SlotPosition(ix, iy, idx, idy);
                // Debug.WriteLine(position.ToString());
                positionable.SetFinalPosition(position);
            }
            else
            {
                var position = new SlotPosition((float)this.x, (float)this.y, (float)this.dx, (float)this.dy);
                positionable.SetPosition(position);
            }
        }
    }

    private void Constrain()
    {
        if (this.x < 0.0)
        {
            this.x = 0.0;
            this.vector = new Vector(0.0, this.vector.Y); 
        }

        if (this.y < 0.0)
        {
            this.y = 0.0;
            this.vector = new Vector(this.vector.X, 0.0);
        }

        if (this.x + this.dx > this.pageWidth)
        {
            double excess = Math.Ceiling(this.x + this.dx - this.pageWidth); 
            this.dx = this.dx - excess; 
        }

        if (this.y + this.dy > this.pageHeight)
        {
            double excess = Math.Ceiling(this.y + this.dy - this.pageHeight);
            this.dy = this.dy - excess;
        }
    }
}
