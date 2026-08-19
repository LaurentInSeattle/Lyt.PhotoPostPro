namespace Lyt.PhotoPostPro.Model.PostProcessors;

public class OrientationStep(PostProcessWorkflow postProcessWorkflow) :
    PostProcessStep(postProcessWorkflow, PostProcessStep.OrientationStepName)
{
    public int RotationAngle { get; set; } // Degrees

    public bool IsMirrored { get; set; }

    public override void Initialize(Image<RgbaHalf> _) => this.Clear();

    public override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    public override void PerformStep(PostProcessParameters ppp)
    {
        if (ppp.OrientationIsMirrored)
        {
            this.Flip(isMirror: true);
        }

        int angle = ppp.OrientationRotationAngle;
        if (angle != 0)
        {
            this.Rotate(isClockwise: angle > 0);
        }
    }

    protected override void SetIdentity()
        => base.IsIdentity = this.RotationAngle == 0 && !this.IsMirrored;

    internal Frame? Rotate(bool isClockwise)
    {
        int angle = isClockwise ? 90 : -90;
        this.RotationAngle += angle;
        this.Normalize();
        this.SetIdentity();
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

        this.SetIdentity();
        return this.Transform();
    }

    internal override Frame? Transform(bool withFrame = true)
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        if (this.IsIdentity)
        {
            this.ResultImage = this.SourceImage;
        }
        else
        {
            RotateMode rotateMode =
                this.RotationAngle == 0 ?
                RotateMode.None :
                    this.RotationAngle == -90 ?
                        RotateMode.Rotate270 :
                        this.RotationAngle == 90 ? RotateMode.Rotate90 : RotateMode.Rotate180;
            FlipMode flipMode = this.IsMirrored ? FlipMode.Horizontal : FlipMode.None;
            var clone = this.SourceImage.Clone();
            clone.Mutate(x => x.RotateFlip(rotateMode, flipMode));
            this.ResultImage = clone;
        }

        return withFrame ? this.ResultImage.ToFrame() : null;
    }

    private void Clear()
    {
        this.RotationAngle = 0;
        this.IsMirrored = false;
        this.SetIdentity();
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
