namespace Lyt.PhotoPostPro.Workflow.Process;

using global::Avalonia.LogicalTree;

public partial class StepViewModel<TView> :
    StepViewModel
    where TView : View, new()
{
}

public partial class StepViewModel :
    ViewModel,
    IRecipient<SourceImageGeneratedMessage>,
    IRecipient<ResultImageGeneratedMessage>
{
    protected readonly PhotoPostProModel model;

    private Frame? sourceImageFrame;

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

        if (this.IsActivated)
        {
            Dispatch.OnUiThread(() =>
            {
                this.SourceImageIsVisible = false;
                this.ResultImageIsVisible = false;

                // Do NOT dispose
                // This creates Bitmaps to fail... Especially when going Full Screen or transitioning back
                //
                // this.SourceImage?.Dispose();
                // this.ResultImage?.Dispose();

                // Clear the Source Image Frame in case we have not received a Result Image 
                if ((this.sourceImageFrame is not null) && (this.sourceImageFrame.Data is not null))
                {
                    this.sourceImageFrame.Dispose();
                }
            }, DispatcherPriority.ApplicationIdle);
        }

        base.Deactivate();
    }

    public void Receive(SourceImageGeneratedMessage message)
    {
        Dispatch.OnUiThread(() =>
        {
            if (this.IsActivated)
            {
                var bitmap = message.Frame.ToWriteableBitmap();
                this.sourceImageFrame = message.Frame;

                // do NOT dispose the frame just yet, because the same frame instance could be given 
                // in the result image message: See below
                var size = bitmap.PixelSize;
                this.IsPortrait = size.Height >= size.Width;
                this.SourceImageIsVisible = true;
                this.SourceImage = bitmap;
                this.ZoomToFit();
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
                message.Frame.Dispose();

                // Now we can dispose the source image frame, if not already done in the line just above 
                if ((this.sourceImageFrame is not null) && (this.sourceImageFrame.Data is not null))
                {
                    this.sourceImageFrame.Dispose();
                }

                var size = bitmap.PixelSize;
                this.IsPortrait = size.Height >= size.Width;
                this.ResultImageIsVisible = true;
                this.ResultImage = bitmap;
                this.ZoomToFit();
                this.OnResultImageReceived(bitmap);
            }
        }, DispatcherPriority.ApplicationIdle);
    }

    // Derived view models MUST call this base method 
    // public because base is ViewModel 
    public override void Initialize()
    {
        this.SourceImageIsVisible = false;
        this.ResultImageIsVisible = false;
        this.SourceImage?.Dispose();
        this.ResultImage?.Dispose();
        this.ResultImage = null;
        this.SourceImage = null;
    }

    protected virtual void OnSourceImageReceived(WriteableBitmap bitmap) { }

    protected virtual void OnResultImageReceived(WriteableBitmap bitmap) { }

    private void ZoomToFit()
    {
        Schedule.OnUiThread(66, () =>
        {
            if (this.IsActivated)
            {
                if (this.ViewBase is View view)
                {
                    var baBiew = view.GetLogicalDescendants().OfType<BeforeAfterView>().FirstOrDefault();
                    baBiew?.ZoomToFit();
                } 
            }
        }, DispatcherPriority.Background);
    }
}