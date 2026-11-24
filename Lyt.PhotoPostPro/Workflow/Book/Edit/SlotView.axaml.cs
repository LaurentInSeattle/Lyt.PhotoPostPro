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

    private void OnSlotTextBoxLostFocus(object? sender, global::Avalonia.Input.FocusChangedEventArgs e)
    {
        if (this.DataContext is SlotViewModel slotViewModel)
        {
            slotViewModel.OnTextBoxLostFocus();
            e.Handled = true;
        }
    }
}