namespace Lyt.PhotoPostPro.Workflow.Process.Export;

public sealed partial class ExportToolboxViewModel : ToolboxViewModel<ExportToolboxView, ExportStep> 
{
    public ExportToolboxViewModel()
    {
        this.SpinViewModel = new SpinViewModel()
        {
            IsVisible = false,
            IsActive = false,
        };
    }

    protected override string Title => this.Localize("Workflow.Export.Title");

    [ObservableProperty]
    public partial SpinViewModel SpinViewModel { get; set; }

    private void SpinWait(bool start = true)
    {
        this.SpinViewModel.IsVisible = start;
        this.SpinViewModel.IsActive = start;
    }

    [RelayCommand]
    public void OnExport()
    {
        // TODO: Collect parameters 
        ExportParameters exportParameters = new();

        // Always launch a spinner for big or small files 
        this.SpinWait(start: true);
        Task.Run(() => 
        {
            try
            {
                this.model.Export(exportParameters);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            } 
            finally
            {
                // Error or not: stop the spinner 
                Dispatch.OnUiThread(() => { this.SpinWait(start: false); });
            }
        });
    }

    [RelayCommand]
    public void OnNavigate() => this.model.NavigateToExport();

    [RelayCommand]
    public void OnFinish()
    {
        // TODO: Warn if nothing exported 
        new ToolbarCommandMessage(ToolbarCommandMessage.ToolbarCommand.BackToWindowed).Publish();
        this.model.Finish();
    }
}
