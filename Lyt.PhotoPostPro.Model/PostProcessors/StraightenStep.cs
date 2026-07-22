namespace Lyt.PhotoPostPro.Model.PostProcessors;

public class StraightenStep(PostProcessWorkflow postProcessWorkflow) : 
    PostProcessStep(postProcessWorkflow, PostProcessStep.StraightenStepName)
{
    public float RotationAngle { get ; set ; } // Degrees

    public override void Initialize(Image<HalfVector4> _) => this.Clear();

    public override Frame? Reset()
    {
        this.Clear();
        return base.Reset ();
    }

    internal Frame? Rotate(bool isClockwise, float angle)
    {
        float angleDelta = isClockwise ? angle : -angle;
        this.RotationAngle += angleDelta;
        this.Normalize();
        return this.Transform();
    }

    public override Frame? Transform(bool withFrame =true )
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        var clone = this.SourceImage.Clone();
        bool isChanged = Math.Abs(this.RotationAngle) > 0.05;
        if (isChanged)
        {
            clone.Mutate(x => x.Rotate(this.RotationAngle));
        }

        this.ResultImage = isChanged ? clone : this.SourceImage;
        return withFrame ? clone.ToFrame() : null;
    }

    private void Clear() => this.RotationAngle = 0.0f;
    
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
