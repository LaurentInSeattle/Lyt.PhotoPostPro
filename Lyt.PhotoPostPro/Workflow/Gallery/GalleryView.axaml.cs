namespace Lyt.PhotoPostPro.Workflow.Gallery;

public partial class GalleryView : View
{
    public GalleryView()
    {
        this.Loaded += (s, e) =>
        {
            //var animator = App.GetRequiredService<IAnimationService>();
            //new AppearsOnMouseOverBehavior(animator).Attach(this.NextButton);
            //new AppearsOnMouseOverBehavior(animator).Attach(this.BackButton);
        };
    }

    protected override void OnDataContextChanged(object? sender, EventArgs e) { }
}