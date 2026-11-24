namespace Lyt.PhotoPostPro.Workflow.Book.Edit;

using Lyt.PhotoPostPro.Model.PhotoBook.Slots;

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
            e.Handled = true;
        }
    }

    //public void SetPosition(SlotPosition slotPosition)
    //{
    //    Debug.WriteLine(" SetPosition: " + slotPosition.ToString());

    //    Canvas.SetLeft(this, slotPosition.X);
    //    Canvas.SetTop(this, slotPosition.Y);
    //    this.Width = slotPosition.Dx;
    //    this.Height = slotPosition.Dy;
    //}
}