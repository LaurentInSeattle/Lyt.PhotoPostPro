namespace Lyt.PhotoPostPro.Workflow.Process;

public partial class StepViewModel<TView> :
    ViewModel<TView>,
    IRecipient<ImageGeneratedMessage>
    where TView : View, new()
{
    protected readonly PhotoPostProModel model;

    private bool isLoaded;

    public StepViewModel() => this.model = App.GetRequiredService<PhotoPostProModel>();

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
        this.Subscribe<ImageGeneratedMessage>();
        this.model.GetStepImage();
    }

    public override void Deactivate()
    {
        this.Unregister<ImageGeneratedMessage>();
        base.Deactivate();
    }

    public void Receive(ImageGeneratedMessage message)
    {
        Dispatch.OnUiThread(() => 
        {
            if (this.IsActivated)
            {
                var bitmap = message.Frame.ToWriteableBitmap();
                if (!isLoaded)
                {
                    this.ResultImageIsVisible = false;
                    this.SourceImage = bitmap;
                    this.SourceImageIsVisible = true;
                    this.isLoaded = true;
                }
                else
                {
                    this.ResultImage = bitmap;
                    this.ResultImageIsVisible = true;
                    this.SourceImageIsVisible = false;
                }

                this.OnImageReceived(bitmap);
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
        this.isLoaded = true;
    }

    protected virtual void OnImageReceived (WriteableBitmap bitmap) { }
}