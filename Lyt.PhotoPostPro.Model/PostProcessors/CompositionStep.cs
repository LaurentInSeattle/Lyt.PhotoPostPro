namespace Lyt.PhotoPostPro.Model.PostProcessors;

internal class CompositionStep() : PostProcessStep(PostProcessStep.CompositionStepName)
{
    public int X { get ; set ; }
 
    public int Y { get; set; }

    public int Dx { get; set; }

    public int Dy { get; set; }

    public override void Initialize() => this.Clear() ;

    public override Frame? Reset()
    {
        this.Clear();
        return base.Reset () ;
    }

    internal Frame? Crop(int x, int y, int dx, int dy)
    {
        this.X = x;
        this.Y = y;
        this.Dx = dx;
        this.Dy = dy;
        return this.Transform();
    }

    public override Frame? Transform(bool withFrame = true)
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        var cropRectangle = new Rectangle(this.X, this.Y, this.Dx, this.Dy);
        bool isChanged = 
            ( this.Dx != 0 && this.Dy != 0)  && 
            ( cropRectangle.Height != this.SourceImage.Height || cropRectangle.Width != this.SourceImage.Width ) ;
        if (isChanged)
        {
            var clone = this.SourceImage.Clone();
            clone.Mutate( x=> x.Crop(cropRectangle));
            this.ResultImage = clone;

            // So that we do not crop again the cropped image when going back and forth in the workflow 
            this.Clear();
        }
        else
        {
            this.ResultImage = this.SourceImage;
        }

        return withFrame ? this.ResultImage.ToFrame() : null;
    }

    private void Clear()
    {
        this.X = 0;
        this.Y = 0;
        this.Dx = 0;
        this.Dy = 0;
    }
}
