namespace Lyt.PhotoPostPro.Workflow.Tools.Statistics;

public sealed partial class StatisticsViewModel : ViewModel<StatisticsView>
{
    private readonly PhotoPostProModel model;


    public StatisticsViewModel(PhotoPostProModel model)
    {
        this.model = model;
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
    }

}
