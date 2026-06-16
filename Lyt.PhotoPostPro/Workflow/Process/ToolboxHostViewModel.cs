namespace Lyt.PhotoPostPro.Workflow.Process;

public sealed partial class ToolboxHostViewModel : ViewModel<ToolboxHostView>
{
    private readonly PhotoPostProModel model;

    public ToolboxHostViewModel() => this.model = App.GetRequiredService<PhotoPostProModel>();

    [ObservableProperty]
    public partial string Title { get; set; } = " - ? ? ? -";

    [ObservableProperty]
    public partial bool BackIsDisabled { get; set; }

    [ObservableProperty]
    public partial bool ResetIsDisabled { get; set; }

    [ObservableProperty]
    public partial bool NextIsDisabled { get; set; }

    [RelayCommand]
    public void OnBack() => this.model.Workflow.GoBack();

    [RelayCommand]
    public void OnReset() => this.model.Workflow.Reset();

    [RelayCommand]
    public void OnNext()
    {
        this.OnBeforeNext();
        this.model.Workflow.SaveAndNext();
    }

    public  void OnBeforeNext() { }
}
