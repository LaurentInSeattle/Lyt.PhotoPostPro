namespace Lyt.PhotoPostPro.Workflow.Language;

public sealed partial class LanguageToolbarViewModel : ViewModel<LanguageToolbarView>
{
#pragma warning disable CA1822 // Mark members as static
    // RelayCommand's cannot be static 

    [RelayCommand]
    public void OnNext()
    {
        var model = App.GetRequiredService<PhotoPostProModel>();
        var shell = App.GetRequiredService<ShellViewModel>();
        if (model.IsFirstRun)
        {
            shell.EnableAndSelect(ActivatedView.FirstRun);
        }
        else
        {
            shell.EnableAndSelect(ActivatedView.Library);
        }
    }

#pragma warning restore CA1822
}
