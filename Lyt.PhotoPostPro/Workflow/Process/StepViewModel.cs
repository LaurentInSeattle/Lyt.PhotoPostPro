namespace Lyt.PhotoPostPro.Workflow.Process;

public partial class StepViewModel<TView> :
    ViewModel<TView>,
    IRecipient<SourceImageGeneratedMessage>,
    IRecipient<ResultImageGeneratedMessage>
    where TView : View, new()
{
    protected readonly PhotoPostProModel model;

    public StepViewModel() => this.model = App.GetRequiredService<PhotoPostProModel>();

    [ObservableProperty]
    public partial bool IsPortrait { get; set; }

    [ObservableProperty]
    public partial WriteableBitmap? SourceImage { get; set; }

    [ObservableProperty]
    public partial bool SourceImageIsVisible { get; set; }

    [ObservableProperty]
    public partial WriteableBitmap? ResultImage { get; set; }

    [ObservableProperty]
    public partial bool ResultImageIsVisible { get; set; }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
        this.Subscribe<SourceImageGeneratedMessage>();
        this.Subscribe<ResultImageGeneratedMessage>();
        this.model.GetStepSourceImage();
        this.model.GetStepResultImage();
    }

    public override void Deactivate()
    {
        this.Unregister<SourceImageGeneratedMessage>();
        this.Unregister<ResultImageGeneratedMessage>();
        base.Deactivate();
    }

    public void Receive(SourceImageGeneratedMessage message)
    {
        Dispatch.OnUiThread(() =>
        {
            if (this.IsActivated)
            {
                var bitmap = message.Frame.ToWriteableBitmap();
                this.SourceImage = bitmap;
                this.SourceImageIsVisible = true;
                this.OnSourceImageReceived(bitmap);
            }
        }, DispatcherPriority.ApplicationIdle);
    }

    public void Receive(ResultImageGeneratedMessage message)
    {
        Dispatch.OnUiThread(() => 
        {
            if (this.IsActivated)
            {
                var bitmap = message.Frame.ToWriteableBitmap();
                this.ResultImage = bitmap;
                this.ResultImageIsVisible = true;
                this.OnResultImageReceived(bitmap);
            }
        }, DispatcherPriority.ApplicationIdle); 
    }

    // Derived view models MUST call this base method 
    // public because base is ViewModel 
    public override void Initialize ()
    {
        this.ResultImage = null;
        this.ResultImageIsVisible = false;
        this.SourceImage = null;
        this.SourceImageIsVisible = false;
    }

    protected virtual void OnSourceImageReceived (WriteableBitmap bitmap) 
    {
        var size = bitmap.PixelSize;
        this.IsPortrait = size.Height >= size.Width;
        this.SourceImageIsVisible = true; 
    }

    protected virtual void OnResultImageReceived(WriteableBitmap bitmap) { }
}