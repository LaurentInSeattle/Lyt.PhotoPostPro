namespace Lyt.PhotoPostPro.Workflow.FirstRun;

public sealed partial class FirstRunViewModel : ViewModel<FirstRunView>
{
    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    private ViewSelector<ActivatedView>? viewSelector;

    public FirstRunViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
    }

    public override void OnViewLoaded()
    {
        var shellViewModel = App.GetRequiredService<ShellViewModel>(); 
        this.viewSelector = shellViewModel.ViewSelector;
    }

    [RelayCommand]
    public void OnLibrary() => this.viewSelector?.SelectView(ActivatedView.Library);

    [RelayCommand]
    public void OnCamera() => this.viewSelector?.SelectView(ActivatedView.Camera);

    [RelayCommand]
    public void OnGallery() => this.viewSelector?.SelectView(ActivatedView.Gallery);

    [RelayCommand]
    public void OnImport() => this.viewSelector?.SelectView(ActivatedView.Import);

    [RelayCommand]
    public void OnSettings() => this.viewSelector?.SelectView(ActivatedView.Settings);

    [RelayCommand]
    public void OnDocumentation() => this.viewSelector?.SelectView(ActivatedView.Documentation);

    [RelayCommand]
    public void OnLanguage() => this.viewSelector?.SelectView(ActivatedView.Language);

    [RelayCommand]
    public void OnTools() => this.viewSelector?.SelectView(ActivatedView.Tools);
}
