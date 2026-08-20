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
            this.Flip(isMirror: true, withFrame: false);
        }

        int angle = ppp.OrientationRotationAngle;
        if (angle != 0)
        {
            this.Rotate(isClockwise: angle > 0, withFrame: false);
        }
    }

    protected override void SetIdentity()
        => base.IsIdentity = this.RotationAngle == 0 && !this.IsMirrored;

    internal Frame? Rotate(bool isClockwise, bool withFrame = true)
    {
        int angle = isClockwise ? 90 : -90;
        this.RotationAngle += angle;
        this.Normalize();
        this.SetIdentity();
        return this.Transform(withFrame);
    }

    // Mirror : AKA: Horizontal Flip 
    // Reverse : AKA: Vertical Flip 
    internal Frame? Flip(bool isMirror, bool withFrame = true)
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
        return this.Transform(withFrame);
    }

    internal override Frame? Transform(bool withFrame = true)
        => base.DoTransform((clone) =>
        {
            RotateMode rotateMode =
                this.RotationAngle == 0 ?
                RotateMode.None :
                    this.RotationAngle == -90 ?
                        RotateMode.Rotate270 :
                        this.RotationAngle == 90 ? RotateMode.Rotate90 : RotateMode.Rotate180;
            FlipMode flipMode = this.IsMirrored ? FlipMode.Horizontal : FlipMode.None;
            clone.Mutate(x => x.RotateFlip(rotateMode, flipMode));
        }, withFrame);


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
