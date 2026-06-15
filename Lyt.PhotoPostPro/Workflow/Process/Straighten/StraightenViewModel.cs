namespace Lyt.PhotoPostPro.Workflow.Process.Straighten;

public sealed partial class StraightenViewModel : StepViewModel<StraightenView>
{
    [ObservableProperty]
    public partial GuidelineViewModel HorizontalGuideLineViewModel { get; set; }

    [ObservableProperty]
    public partial GuidelineViewModel VerticalGuideLineViewModel { get; set; }

    public StraightenViewModel() : base() 
    {
        this.HorizontalGuideLineViewModel = new GuidelineViewModel(isVertical: false);
        this.VerticalGuideLineViewModel = new GuidelineViewModel(isVertical: true);
    }
}
