namespace Lyt.PhotoPostPro.Model.PostProcessors;

internal class CompositionStep() : PostProcessStep(PostProcessStep.CompositionStepName)
{
    private int x; // pixels 
    private int y; // pixels 
    private int dx; // pixels 
    private int dy; // pixels 

    public override void Initialize() => this.Clear() ;

    public override Frame? Reset()
    {
        this.Clear();
        return base.Reset () ;
    }

    internal Frame? Crop(int x, int y, int dx, int dy)
    {
        this.x = x;
        this.y = y;
        this.dx = dx;
        this.dy = dy;
        return this.Transform();
    }

    public override Frame? Transform(bool withFrame = true)
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        var cropRectangle = new Rectangle(this.x, this.y, this.dx, this.dy);
        bool isChanged = 
            ( this.dx != 0 && this.dy != 0)  && 
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
        this.x = 0;
        this.y = 0;
        this.dx = 0;
        this.dy = 0;
    }
}
