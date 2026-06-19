namespace Lyt.PhotoPostPro.Model.PostProcessors;

public class OrientationStep() : PostProcessStep(PostProcessStep.OrientationStepName)
{
    public int RotationAngle { get; set; } // Degrees

    public bool IsMirrored { get; set; }

    public override void Initialize(Image<Rgb48> _) => this.Clear();

    public override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    internal Frame? Rotate(bool isClockwise)
    {
        int angle = isClockwise ? 90 : -90;
        this.RotationAngle += angle;
        this.Normalize();
        return this.Transform();
    }

    // Mirror : AKA: Horizontal Flip 
    // Reverse : AKA: Vertical Flip 
    internal Frame? Flip(bool isMirror)
    {
        if (isMirror)
        {
            this.IsMirrored = !this.IsMirrored;
        }
        else
        {
            this.RotationAngle += 180;
            this.Normalize();
        }

        return this.Transform();
    }

    public override Frame? Transform(bool withFrame = true)
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        RotateMode rotateMode =
            !this.IsRotated ?
            RotateMode.None :
                this.RotationAngle == -90 ?
                    RotateMode.Rotate270 :
                    this.RotationAngle == 90 ? RotateMode.Rotate90 : RotateMode.Rotate180;
        FlipMode flipMode = this.IsMirrored ? FlipMode.Horizontal : FlipMode.None;

        var clone = this.SourceImage.Clone();
        bool isChanged = flipMode != FlipMode.None || rotateMode != RotateMode.None;
        if (isChanged)
        {
            clone.Mutate(x => x.RotateFlip(rotateMode, flipMode));
        }

        this.ResultImage = isChanged ? clone : this.SourceImage;
        return withFrame ? clone.ToFrame() : null;
    }

    private bool IsRotated => this.RotationAngle != 0;

    private void Clear ()
    {
        this.RotationAngle = 0;
        this.IsMirrored = false;
    }

    private void Normalize()
    {
        // In C#, the % operator is a remainder operator, not a true mathematical modulo operator.
        // Because of this, the result of the operation always takes the sign of the left-hand
        // operand (the dividend).
        // So... first add 360 as a preventive measure 
        this.RotationAngle += 360;
        this.RotationAngle = ((this.RotationAngle + 180) % 360) - 180;
    }
}
