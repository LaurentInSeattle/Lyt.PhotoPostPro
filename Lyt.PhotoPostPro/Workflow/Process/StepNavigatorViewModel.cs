namespace Lyt.PhotoPostPro.Workflow.Process;

public sealed partial class StepNavigatorViewModel(ProcessViewModel parent) : ViewModel<StepNavigatorView>
{
    private readonly ProcessViewModel parent = parent;

    [ObservableProperty]
    public partial List<SelectorButtonViewModel> Steps { get; set; } = [];

    public override void OnViewLoaded()
    {
        int width = 90;
        int height = 36; 
        double fontSize = 16;
        List<SelectorButtonViewModel> steps = [];
        steps.Add(new SelectorButtonViewModel("Orient", width, height, this.OnStepsChanged, 1, fontSize));
        steps.Add(new SelectorButtonViewModel("Straighten", width, height, this.OnStepsChanged, 2, fontSize));
        steps.Add(new SelectorButtonViewModel("Crop", width, height, this.OnStepsChanged, 3, fontSize));
        steps.Add(new SelectorButtonViewModel("Denoise", width, height, this.OnStepsChanged, 4, fontSize));
        steps.Add(new SelectorButtonViewModel("Exposure", width, height, this.OnStepsChanged, 5, fontSize));
        steps.Add(new SelectorButtonViewModel("Recovery", width, height, this.OnStepsChanged, 6, fontSize));
        steps.Add(new SelectorButtonViewModel("Gradients", width, height, this.OnStepsChanged, 7, fontSize));
        steps.Add(new SelectorButtonViewModel("Whites", width, height, this.OnStepsChanged, 8, fontSize));
        steps.Add(new SelectorButtonViewModel("Contrast", width, height, this.OnStepsChanged, 9, fontSize));
        steps.Add(new SelectorButtonViewModel("LUT's", width, height, this.OnStepsChanged, 10, fontSize));
        steps.Add(new SelectorButtonViewModel("Colors", width, height, this.OnStepsChanged, 11, fontSize));
        steps.Add(new SelectorButtonViewModel("Desaturate", width, height, this.OnStepsChanged, 12, fontSize));
        steps.Add(new SelectorButtonViewModel("Sharpen", width, height, this.OnStepsChanged, 13, fontSize));
        steps.Add(new SelectorButtonViewModel("Filters", width, height, this.OnStepsChanged, 14, fontSize));
        steps.Add(new SelectorButtonViewModel("Exports", width, height, this.OnStepsChanged, 15, fontSize));
        this.Steps = steps; 
    }

    void OnStepsChanged(object? tag)
    {
        if ( tag is int index )
        {
            Debug.WriteLine(index);
        }
    }
}
