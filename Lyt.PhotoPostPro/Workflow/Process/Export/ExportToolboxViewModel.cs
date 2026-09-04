namespace Lyt.PhotoPostPro.Workflow.Process.Export;

public sealed partial class ExportToolboxViewModel : ToolboxViewModel<ExportToolboxView, ExportStep>
{
    private bool isInitializing;

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
    public partial int Rating { get; set; }

    [ObservableProperty]
    public partial bool IsExporting { get; set; }

    [ObservableProperty]
    public partial SpinViewModel SpinViewModel { get; set; }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
        With.Flag(ref this.isInitializing, () =>
        {
            var postProcess = this.model.Workflow;
            var metadata = postProcess.Metadata;
            this.Rating = metadata.Rating;
        });
    }

    private void SpinWait(bool start = true)
    {
        this.SpinViewModel.IsVisible = start;
        this.SpinViewModel.IsActive = start;
    }

    [RelayCommand]
    public void OnExport()
    {
        // TODO: Collect parameters 
        ImageExportsCollection imageExports = new();

        // Always launch a spinner for big or small files 
        this.IsExporting = true;
        this.SpinWait(start: true);

        Task.Run(() =>
        {
            try
            {
                this.model.Export(imageExports);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                // Error or not: stop the spinner and enable the buttons again
                Dispatch.OnUiThread(() =>
                {
                    this.SpinWait(start: false);
                    this.IsExporting = false;
                });
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

    partial void OnRatingChanged(int value)
    {
        if (this.isInitializing)
        {
            return;
        }

        if (value <= 0 && value > 5)
        {
            return;
        }

        // Save new value to model and to disk 
        var metadata = this.model.Workflow.Metadata;
        metadata.Rating = value;
        this.model.LibraryManager.SaveMetadata(metadata);
    }
}
