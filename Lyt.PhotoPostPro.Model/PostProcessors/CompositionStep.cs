namespace Lyt.PhotoPostPro.Model.PostProcessors;

internal class CompositionStep() : PostProcessStep(PostProcessStep.CompositionStepName)
{
    private int x; // pixels 
    private int y; // pixels 
    private int dx; // pixels 
    private int dy; // pixels 

    public override void Initialize() => this.Clear() ;


    internal Frame? ClearCrop()
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        this.Clear() ;
        this.ResultImage = this.SourceImage;
        return this.SourceImage.ToFrame();
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

        var clone = this.SourceImage.Clone();
        var cropRectangle = new Rectangle(this.x, this.y, this.dx, this.dy); 
        bool isChanged = cropRectangle.Height != 0 || cropRectangle.Width != 0;
        if (isChanged)
        {
            clone.Mutate( x=> x.Crop(cropRectangle)); 
        }

        this.ResultImage = isChanged ? clone : this.SourceImage;
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
