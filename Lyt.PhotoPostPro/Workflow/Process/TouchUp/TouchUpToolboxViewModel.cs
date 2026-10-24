namespace Lyt.PhotoPostPro.Workflow.Process.TouchUp;

public sealed partial class TouchUpToolboxViewModel :
    ToolboxViewModel<TouchUpToolboxView, TouchUpStep>,
    IRecipient<ImageClickedMessage>
{
    // private bool doNotUpdateModel;
    private int spotPixelX;
    private int spotPixelY;
    private int patchPixelX;
    private int patchPixelY;
    private double diameter;
    private double thickness;

    [ObservableProperty]
    public partial string DiameterString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double DiameterSliderValue { get; set; }

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

        var vm = App.GetRequiredService<TouchUpViewModel>();
        if (vm is null || !vm.IsBound || vm.View is null)
        {
            return;
        }

        if (message.IsShifted)
        {
            if (!vm.View.HasTarget(isSpot: true))
            {
                // No spot target: bail out 
                return;
            }

            // Shift click: Select patch
            this.patchPixelX = message.PixelX;
            this.patchPixelY = message.PixelY;
            double x = this.patchPixelX - this.diameter / 2.0;
            double y = this.patchPixelY - this.diameter / 2.0;
            vm.View.DrawTarget(isSpot: false, x, y, this.diameter, this.thickness);
        }
        else
        {
            // Regular click: Select spot 
            this.spotPixelX = message.PixelX;
            this.spotPixelY = message.PixelY;
            var bitmapSize = message.WriteableBitmap.PixelSize;
            int largestDimension = Math.Max(bitmapSize.Height, bitmapSize.Width);
            this.diameter = largestDimension / 70.0;
            this.thickness = this.diameter / 8.0;
            double x = this.spotPixelX - this.diameter / 2.0;
            double y = this.spotPixelY - this.diameter / 2.0;
            vm.View.DrawTarget(isSpot: true, x, y, this.diameter, this.thickness);
            this.DiameterSliderValue = this.diameter;
        }
    }

    private void Clear()
    {
        this.spotPixelX = -1;
        this.spotPixelY = -1;
        this.patchPixelX = -1;
        this.patchPixelY = -1;
        this.diameter = -1;

        var vm = App.GetRequiredService<TouchUpViewModel>();
        if (vm is not null && vm.IsBound)
        {
            vm.View.ClearTarget(isSpot: true);
            vm.View.ClearTarget(isSpot: false);
        }
    }

    partial void OnDiameterSliderValueChanged(double value)
    {
        // Slider sends -100.0 to +100.0, fine for the model  
        this.diameter = value;
        this.DiameterString = (int)Math.Round(value) + " px";
        var vm = App.GetRequiredService<TouchUpViewModel>();
        if (vm is not null && vm.IsBound)
        {
            var view = vm.View;
            if (view.HasTarget(isSpot: true))
            {
                double x = this.spotPixelX - this.diameter / 2.0;
                double y = this.spotPixelY - this.diameter / 2.0;
                view.DrawTarget(isSpot: true, x, y, this.diameter, this.thickness);
            }

            if (view.HasTarget(isSpot: false))
            {
                double x = this.patchPixelX - this.diameter / 2.0;
                double y = this.patchPixelY - this.diameter / 2.0;
                view.DrawTarget(isSpot: false, x, y, this.diameter, this.thickness);
            }
        }
    }

    private void UpdateModel()
    {
        //if (this.doNotUpdateModel)
        //{
        //    return;
        //}

        var vm = App.GetRequiredService<TouchUpViewModel>();
        if (vm is not null && vm.IsBound)
        {
            var view = vm.View;
            if (view.HasTarget(isSpot: true) && view.HasTarget(isSpot: false) && this.diameter > 0.0)
            {
                // Model uses normalized values 
                this.model.SpotRemoval(
                    this.spotPixelX, this.spotPixelY,
                    this.patchPixelX, this.patchPixelY,
                    (int)Math.Round(1.5 + this.diameter / 2.0));

                // Clear both targets so that we can see what has been done 
                view.ClearTarget(isSpot: true);
                view.ClearTarget(isSpot: false);
            }
        }
    }

    public override void OnBeforeReset()
    {
        base.OnBeforeReset();
        this.Clear();
    }

    [RelayCommand]
    public void OnPatch() => this.UpdateModel();
}
