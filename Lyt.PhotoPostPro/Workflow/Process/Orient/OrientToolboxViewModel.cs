namespace Lyt.PhotoPostPro.Workflow.Process.Orient;

public sealed partial class OrientToolboxViewModel : ToolboxViewModel<OrientToolboxView>
{
    protected override string Title => this.Localize("Workflow.Orient.Title") ;

    [RelayCommand]
    public void OnRotateClockwise() => base.model.Rotate(isClockwise: true);

    [RelayCommand]
    public void OnRotateCounterClockwise() => base.model.Rotate(isClockwise: false);

    [RelayCommand]
    public void OnMirror() => base.model.Flip(isMirror: true);

    [RelayCommand]
    public void OnReverse() => base.model.Flip(isMirror: false);
}
