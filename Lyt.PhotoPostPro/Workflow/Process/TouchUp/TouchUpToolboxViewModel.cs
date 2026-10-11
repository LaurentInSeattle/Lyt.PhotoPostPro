namespace Lyt.PhotoPostPro.Workflow.Process.TouchUp;

using SixLabors.ImageSharp.ColorProfiles;

public sealed partial class TouchUpToolboxViewModel :
    ToolboxViewModel<TouchUpToolboxView, TouchUpStep>,
    IRecipient<ImageClickedMessage>
{
    private enum TouchUpPhase
    {
        SpotSelection,
        SizeAdjustment,
        PatchSelection,
        Validation,
    }

    private bool doNotUpdateModel;
    private TouchUpPhase phase;
    private int spotPixelX;
    private int spotPixelY;
    private int cleanPixelX;
    private int cleanPixelY;
    private double radius;

    [ObservableProperty]
    public partial string RadiusString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double RadiusSliderValue { get; set; }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
        this.Subscribe<ImageClickedMessage>();
        this.Clear();
    }

    public override void Deactivate()
    {
        this.Unregister<ImageClickedMessage>();
        base.Deactivate();
    }

    public void Receive(ImageClickedMessage message)
    {
        if (!this.IsActivated)
        {
            return;
        }

        if (this.phase == TouchUpPhase.SpotSelection)
        {
            this.spotPixelX = message.PixelX;
            this.spotPixelY = message.PixelY;

            var vm = App.GetRequiredService<TouchUpViewModel>();
            if (vm is not null && vm.IsBound)
            {
                var bitmapSize = message.WriteableBitmap.PixelSize;
                int largestDimension = Math.Max(bitmapSize.Height, bitmapSize.Width);
                this.radius = largestDimension / 70.0;
                double x = this.spotPixelX - this.radius / 2.0;
                double y = this.spotPixelY - this.radius / 2.0;
                vm.View.DrawTargetSpot("clickedPixel", x, y, this.radius);
                this.RadiusSliderValue = this.radius;

                // TODO VAlidate
                this.phase = TouchUpPhase.PatchSelection;
            }
        }
        else if (this.phase == TouchUpPhase.PatchSelection)
        {
            this.cleanPixelX = message.PixelX;
            this.cleanPixelY = message.PixelY;
        }
    }

    private void Clear()
    {
        this.spotPixelX = -1;
        this.spotPixelY = -1;
        this.cleanPixelX = -1;
        this.cleanPixelY = -1;
        this.radius = -1;

        this.phase = TouchUpPhase.SpotSelection;
        var vm = App.GetRequiredService<TouchUpViewModel>();
        if (vm is not null && vm.IsBound)
        {
            vm.View.ClearTargetSpot("clickedPixel");
        }
    }

    partial void OnRadiusSliderValueChanged(double value)
    {
        // Slider sends -100.0 to +100.0, fine for the model  
        this.radius = value;
        this.RadiusString = (int)Math.Round(value) + " px";
        var vm = App.GetRequiredService<TouchUpViewModel>();
        if (vm is not null && vm.IsBound)
        {
            double x = this.spotPixelX - this.radius / 2.0;
            double y = this.spotPixelY - this.radius / 2.0;
            vm.View.DrawTargetSpot("clickedPixel", x, y, this.radius);
        }
    }

    private void UpdateModel()
    {
        if (this.doNotUpdateModel)
        {
            return;
        }

        if (this.phase == TouchUpPhase.Validation)
        {
            // Model uses normalized values 
            this.model.SpotRemoval(
                this.spotPixelX, this.spotPixelY,
                this.cleanPixelX, this.cleanPixelY,
                (int)Math.Round(this.radius));
        }
    }

    public override void OnBeforeReset()
    {
        base.OnBeforeReset();
        this.Clear();
    }

#pragma warning disable CA1822 // Mark members as static
    // RelayCommand's cannot be static 

    [RelayCommand]
    public void OnPatch()
    {
        if (this.spotPixelX < 0 || this.spotPixelY < 0 ||
            this.cleanPixelX < 0 || this.cleanPixelY < 0 ||
            this.radius < 0)
        {
            // Not ready
            return;
        }

        // TODO : Make sure that the two areas do not overlap

        this.model.SpotRemoval(
            this.spotPixelX, this.spotPixelY,
            this.cleanPixelX, this.cleanPixelY,
            (int)Math.Round(this.radius));

        this.Clear();
    }

#pragma warning restore CA1822
}
