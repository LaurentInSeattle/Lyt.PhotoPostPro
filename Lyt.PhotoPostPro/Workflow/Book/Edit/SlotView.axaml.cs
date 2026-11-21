namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

public partial class SlotView : View
{
    public SlotView()
    {
        this.InitializeComponent();
        this.OuterGrid.PointerPressed += this.OnPointerPressed;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (this.DataContext is SlotViewModel slotViewModel)
        {
            slotViewModel.OnClicked(false);
        }
    }
}