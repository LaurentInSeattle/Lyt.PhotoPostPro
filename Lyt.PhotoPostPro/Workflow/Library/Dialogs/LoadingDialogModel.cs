namespace Lyt.PhotoPostPro.Workflow.Library.Dialogs;

public sealed partial class LoadingDialogModel :
    DialogViewModel<LoadingDialog, object>,
    IRecipient<LibraryLoadedMessage>
{
    private DispatcherTimer? timer; 

    [ObservableProperty]
    public partial string? Message { get; set; }

    [ObservableProperty]
    public partial string? Title { get; set; }

    public LoadingDialogModel()
    {
        this.CanEnter = false;
        this.CanEscape = false;
        this.Title = this.Localize("Imaging.InProgress");
        this.Message = this.Localize("Imaging.InProgressHelp");
        this.Subscribe<LibraryLoadedMessage>();

        // This dialog MUST dismiss - no matter what is going on 
        this.timer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromSeconds(6),
            IsEnabled = true,
        };
        this.timer.Tick += this.OnTimerTick;
        this.timer.Start();
    }

    ~LoadingDialogModel()
    {
        this.timer?.Stop();
        this.timer = null;
    }

    private void OnTimerTick(object? sender, EventArgs e) => this.Dismiss();

    private void Dismiss()
    {
        this.timer?.Stop();
        this.timer = null;
        this.Cancel();
    }

    public void Receive(LibraryLoadedMessage message)
        => Dispatch.OnUiThread(this.Dismiss, DispatcherPriority.ApplicationIdle);
}
