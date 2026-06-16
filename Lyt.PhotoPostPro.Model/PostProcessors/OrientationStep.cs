namespace Lyt.PhotoPostPro.Model.PostProcessors;

internal class OrientationStep (): PostProcessStep (PostProcessStep.OrientationStepName)
{
    private int rotationAngle; // Degrees 
    private bool isMirrored;

    public override void Initialize()
    {
        this.rotationAngle = 0;
        this.isMirrored = false;
    }

    public override Frame? Reset()
    {
        this.Initialize();
        return base.Reset();
    }

    internal Frame? Rotate(bool isClockwise)
    {
        int angle = isClockwise ? 90 : -90; 
        this.rotationAngle += angle;
        this.Normalize();
        return this.Transform();
    }

    // Mirror : AKA: Horizontal Flip 
    // Reverse : AKA: Vertical Flip 
    internal Frame? Flip(bool isMirror)
    {
        if (isMirror)
        {
            this.isMirrored = ! this.isMirrored;
        } 
        else
        {
            this.rotationAngle += 180; 
            this.Normalize();
        }

        return this.Transform();
    }

    public override Frame? Transform (bool withFrame = true)
    {
        if ( this.SourceImage is null )
        {
            return null;
        }

        RotateMode rotateMode =
            !this.IsRotated ?
            RotateMode.None :
                this.rotationAngle == -90 ?
                    RotateMode.Rotate270 :
                    this.rotationAngle == 90 ? RotateMode.Rotate90 : RotateMode.Rotate180;
        FlipMode flipMode = this.isMirrored ? FlipMode.Horizontal : FlipMode.None;

        var clone = this.SourceImage.Clone();
        bool isChanged = flipMode != FlipMode.None || rotateMode != RotateMode.None; 
        if (isChanged)
        {
            clone.Mutate(x => x.RotateFlip(rotateMode, flipMode));
        }

        this.ResultImage = isChanged ? clone : this.SourceImage;
        return withFrame ? clone.ToFrame() : null;
    }

    private bool IsRotated => this.rotationAngle != 0 ;

    private void Normalize()
    {
        // In C#, the % operator is a remainder operator, not a true mathematical modulo operator.
        // Because of this, the result of the operation always takes the sign of the left-hand
        // operand (the dividend).
        // So... first add 360 as a preventive measure 
        this.rotationAngle += 360; 
        this.rotationAngle = ((this.rotationAngle + 180) % 360) - 180;
    }
}
