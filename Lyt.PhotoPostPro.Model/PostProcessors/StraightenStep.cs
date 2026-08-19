namespace Lyt.PhotoPostPro.Model.PostProcessors;

public class StraightenStep(PostProcessWorkflow postProcessWorkflow) :
    PostProcessStep(postProcessWorkflow, PostProcessStep.StraightenStepName)
{
    public float RotationAngle { get; set; } // Degrees

    public override void Initialize(Image<RgbaHalf> _) => this.Clear();

    protected override void SetIdentity()
        => base.IsIdentity = MathF.Abs(this.RotationAngle) < 0.001f;

    public override Frame? Reset()
    {
        this.Clear();
        return base.Reset();
    }

    public override void PerformStep(PostProcessParameters ppp)
    {
        float angle = ppp.StraightenRotationAngle;
        float absAngle = MathF.Abs(angle);
        if (absAngle > 0.000_1f)
        {
            this.Rotate(isClockwise: angle > 0, absAngle);
        }
    }

    internal Frame? Rotate(bool isClockwise, float angle)
    {
        float angleDelta = isClockwise ? angle : -angle;
        this.RotationAngle += angleDelta;
        this.Normalize();
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
            var clone = this.SourceImage.Clone();
            clone.Mutate(x => x.Rotate(this.RotationAngle));
            this.ResultImage = clone;
        }

        return withFrame ? this.ResultImage.ToFrame() : null;
    }

    private void Clear()
    {
        this.RotationAngle = 0.0f;
        this.SetIdentity();
    }

    private void Normalize()
    {
        // In C#, the % operator is a remainder operator, not a true mathematical modulo operator.
        // Because of this, the result of the operation always takes the sign of the left-hand
        // operand (the dividend).
        // So... first add 360 as a preventive measure 
        this.RotationAngle += 360.0f;
        this.RotationAngle = ((this.RotationAngle + 180.0f) % 360.0f) - 180.0f;
    }
}
