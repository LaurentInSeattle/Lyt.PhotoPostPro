namespace Lyt.PhotoPostPro.Workflow.Camera;

public sealed partial class CameraViewModel : ViewModel<CameraView>
{
    private readonly PhotoPostProModel photoPostProModel;

    public CameraViewModel(PhotoPostProModel photoPostProModel) 
    {
        this.photoPostProModel = photoPostProModel;
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
    }

    public override void Deactivate()
    {
        base.Deactivate();
    }
}
