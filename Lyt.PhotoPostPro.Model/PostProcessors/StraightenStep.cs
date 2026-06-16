namespace Lyt.PhotoPostPro.Model.PostProcessors;

internal class StraightenStep() : PostProcessStep(PostProcessStep.StraightenStepName)
{
    private float rotationAngle; // Degrees 

    public override void Initialize() => this.rotationAngle = 0.0f;

    public override Frame? Reset()
    {
        this.Initialize();
        return base.Reset ();
    }

    internal Frame? Rotate(bool isClockwise, float angle)
    {
        float angleDelta = isClockwise ? angle : -angle;
        this.rotationAngle += angleDelta;
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
        bool isChanged = Math.Abs(this.rotationAngle) > 0.05;
        if (isChanged)
        {
            clone.Mutate(x => x.Rotate(this.rotationAngle));
        }

        this.ResultImage = isChanged ? clone : this.SourceImage;
        return withFrame ? clone.ToFrame() : null;
    }

    private void Normalize()
    {
        // In C#, the % operator is a remainder operator, not a true mathematical modulo operator.
        // Because of this, the result of the operation always takes the sign of the left-hand
        // operand (the dividend).
        // So... first add 360 as a preventive measure 
        this.rotationAngle += 360.0f;
        this.rotationAngle = ((this.rotationAngle + 180.0f) % 360.0f) - 180.0f;
    }
}
