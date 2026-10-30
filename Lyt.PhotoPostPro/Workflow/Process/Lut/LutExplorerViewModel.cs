namespace Lyt.PhotoPostPro.Workflow.Process.Lut;

public sealed partial class LutExplorerViewModel :
    ViewModel<LutExplorerView>,
    ISelectListener,
    IRecipient<WorkflowUpdateMessage>,
    IRecipient<ExploreLutImageGeneratedMessage>,
    IRecipient<ExploreLutImageCompleteMessage>
{
    private LutViewModel? lutViewModel;

    public LutExplorerViewModel()
    {
        this.Clear();
        this.Subscribe<WorkflowUpdateMessage>();
        this.Subscribe<ExploreLutImageGeneratedMessage>();
        this.Subscribe<ExploreLutImageCompleteMessage>();
    }

    [ObservableProperty]
    public partial LutImageViewModel? Original { get; set; }

    [ObservableProperty]
    public partial LutImageViewModel? Selected { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<LutImageViewModel> LutImageViews { get; set; } = [];

    internal void Launch(LutViewModel lutViewModel, LutStep lutStep)
    {
        this.lutViewModel = lutViewModel;
        Debug.WriteLine(" Launching Explore Luts ");
        this.Clear();
        lutStep.LaunchExploreLuts();
    }

    public void Receive(WorkflowUpdateMessage message)
        => Dispatch.OnUiThread(() => { this.ReceiveOnUiThread(message); }, DispatcherPriority.Background);

    private void ReceiveOnUiThread(WorkflowUpdateMessage message)
    {
        if (message.Kind == WorkflowUpdateKind.Begin || message.Kind == WorkflowUpdateKind.Finish)
        {
            this.Clear();
        }
    }

    private void Clear()
    {
        this.LutImageViews.Clear();
        this.Original = null;
        this.Selected = null;
    }

    public void Receive(ExploreLutImageGeneratedMessage message)
        => Dispatch.OnUiThread(() => { this.ReceiveOnUiThread(message); }, DispatcherPriority.Background);

    private void ReceiveOnUiThread(ExploreLutImageGeneratedMessage message)
    {
        var bitmap = message.Frame.ToWriteableBitmap();
        var metadata = message.LutMetadata;
        Debug.WriteLine(" Image received ");
        if (metadata.IsEmpty)
        {
            Debug.WriteLine(" Original ");
            this.Original = new LutImageViewModel(this, metadata, bitmap);
        }
        else
        {
            Debug.WriteLine(" " + metadata.FriendlyName);
            this.LutImageViews.Add(new LutImageViewModel(this, metadata, bitmap));
        }
    }

    public void Receive(ExploreLutImageCompleteMessage message)
        => Dispatch.OnUiThread(this.ReceiveCompleteOnUiThread, DispatcherPriority.Background);

    private void ReceiveCompleteOnUiThread()
    {
        var sorted = (from lutImage in this.LutImageViews orderby lutImage.Title ascending select lutImage).ToList();
        this.LutImageViews = new ObservableCollection<LutImageViewModel>(sorted); 
    }

    public void OnSelect(object selectedObject)
    {
        if (selectedObject is not LutImageViewModel selectedLutViewModel)
        {
            return;
        }

        this.Selected = selectedLutViewModel;
    }

    [RelayCommand]
    public void OnNoLut()
    {
        if (this.lutViewModel is null)
        {
            return;
        }

        this.lutViewModel.NoLut();
    }

    [RelayCommand]
    public void OnUseThisLut()
    {
        if (this.lutViewModel is null || this.Selected is null)
        {
            return;
        }

        this.lutViewModel.UseThisLut(this.Selected.Metadata);
    }
}
