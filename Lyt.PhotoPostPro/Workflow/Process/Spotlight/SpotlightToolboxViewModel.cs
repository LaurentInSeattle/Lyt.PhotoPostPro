namespace Lyt.PhotoPostPro.Workflow.Process.Spotlight;

public sealed partial class SpotlightToolboxViewModel :
    ToolboxViewModel<SpotlightToolboxView, SpotlightStep>,
    IRecipient<ImageClickedMessage>

{
    private bool doNotUpdateModel;
    private float centerX;
    private float centerY;
    private float axisX = 120.0f;
    private float axisY = 60.0f;
    private float rotation;
    private double thickness;
    private float lightness;
    private int bitmapHeight;
    private int bitmapWidth;

    [ObservableProperty]
    public partial string AxisXString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double AxisXSliderValue { get; set; }

    [ObservableProperty]
    public partial string AxisYString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double AxisYSliderValue { get; set; }

    [ObservableProperty]
    public partial string RotationString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double RotationSliderValue { get; set; }

    [ObservableProperty]
    public partial string LightnessString { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double LightnessSliderValue { get; set; }

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

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();

        if (!this.isFirstLoad)
        {
            return;
        }

        this.Clear();
        this.isFirstLoad = false;
    }

    public override void OnModelStepUpdated(SpotlightStep step) => this.UpdateSliders(step);

    private void UpdateSliders(SpotlightStep step)
    {
        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Here we need to undo the operations done reading the sliders 
            //this.TopSliderValue = step.Top;
            //this.BottomSliderValue = step.Bottom;
            //this.LeftSliderValue = step.Left;
            //this.RightSliderValue = step.Right;
            this.LightnessSliderValue = step.Lightness;
        });
    }

    private void Clear()
    {
        this.centerX = -1;
        this.centerY = -1;
        this.axisX = 420;
        this.axisY = 420;

        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Force property changed 
            this.RotationSliderValue = 45.0;

            // proper values 
            this.AxisXSliderValue = this.axisX;
            this.AxisYSliderValue = this.axisY;
            this.rotation = 0;
            this.RotationSliderValue = this.rotation;
            this.LightnessSliderValue = this.lightness;
        });

        var vm = App.GetRequiredService<SpotlightViewModel>();
        if (vm is not null && vm.IsBound)
        {
            vm.View.ClearTarget();
        }
    }

    public void Receive(ImageClickedMessage message)
    {
        if (!this.IsActivated)
        {
            return;
        }

        var vm = App.GetRequiredService<SpotlightViewModel>();
        if (vm is null || !vm.IsBound || vm.View is null)
        {
            return;
        }

        // Regular click: Select spot 
        this.centerX = message.PixelX;
        this.centerY = message.PixelY;
        var bitmapSize = message.WriteableBitmap.PixelSize;
        this.bitmapWidth = bitmapSize.Width;
        this.bitmapHeight = bitmapSize.Height;
        int largestDimension = Math.Max(bitmapHeight, bitmapWidth);
        if ((this.axisX < 0.0f) || (this.axisY < 0.0f))
        {
            this.axisX = largestDimension / 20.0f;
            this.axisY = largestDimension / 30.0f;
        }

        this.thickness = Math.Max(2.0f, largestDimension / 640.0f);
        double x = this.centerX - this.axisX / 2.0;
        double y = this.centerY - this.axisY / 2.0;
        vm.View.DrawTarget(x, y, this.axisX, this.axisY, this.rotation, this.thickness);
        this.AxisXSliderValue = this.axisX;
        this.AxisXSliderValue = this.axisX;
    }

    partial void OnAxisXSliderValueChanged(double value)
    {
        // Slider sends 20 to 2000
        this.axisX = (float)value;
        int intValue = (int)(this.axisX);
        this.AxisXString = intValue.ToString("D") + " px";
        this.UpdateEllipse();
    }

    partial void OnAxisYSliderValueChanged(double value)
    {
        // Slider sends 20 to 2000
        this.axisY = (float)value;
        int intValue = (int)(this.axisY);
        this.AxisYString = intValue.ToString("D") + " px";
        this.UpdateEllipse();
    }


    partial void OnRotationSliderValueChanged(double value)
    {
        // Slider sends -45 to 45
        this.rotation = (float)value;
        this.RotationString = this.rotation.ToString("F1") + " \u00B0";
        this.UpdateEllipse();
    }

    private void UpdateEllipse()
    {
        var vm = App.GetRequiredService<SpotlightViewModel>();
        if (vm is not null && vm.IsBound)
        {
            var view = vm.View;
            if (view.HasTarget())
            {
                double x = this.centerX - this.axisX / 2.0;
                double y = this.centerY - this.axisY / 2.0;
                view.DrawTarget(x, y, this.axisX, this.axisY, this.rotation, this.thickness);
            }
        }
    }

    partial void OnLightnessSliderValueChanged(double value)
    {
        // Slider sends -0.8 to +0.8
        this.lightness = (float)value;
        this.LightnessString = value.ToString("+0.00;-0.00;0.00") + " %";
        this.UpdateModel();
    }

    public override void OnBeforeReset()
    {
        base.OnBeforeReset();
        this.Clear();
    }

    private void UpdateModel()
    {
        if (this.doNotUpdateModel)
        {
            return;
        }

        if ((this.centerX <= 0) || (this.centerY <= 0))
        {
            return;
        }

        this.ThrottleModelUpdate(() =>
        {
            this.model.Spotlight(
                this.centerX, this.centerY,
                this.axisX, this.axisY,
                this.rotation, this.lightness);
        });
    }
}
