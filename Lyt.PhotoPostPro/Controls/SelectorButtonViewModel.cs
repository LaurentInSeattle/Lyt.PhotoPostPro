namespace Lyt.PhotoPostPro.Controls;

public sealed partial class SelectorButtonViewModel(
    string buttonText, double buttonWidth, Action<object?> onSelect, object? tag = null) :
    ViewModel<SelectorButtonView>
{
    private readonly Action<object?> onSelect = onSelect;
    private readonly object? tag = tag;

    public bool IsSelected
    {
        get
        {
            if (!this.IsBound)
            {
                return false;
            }

            return this.View.IsSelected;
        }
    }

    [ObservableProperty]
    public partial string ButtonText { get; set; } = buttonText;

    [ObservableProperty]
    public partial double ButtonWidth { get; set; } = buttonWidth;

    [RelayCommand]
    public void OnSelect()
    {
        if (!this.IsBound)
        {
            return;
        }

        if (this.View.IsSelected)
        {
            return;
        }

        this.Select();
    }

    public void Select()
    {
        if (!this.IsBound)
        {
            return;
        }

        this.View.BringIntoView();
        this.View.OnSelect();
        this.onSelect(this.tag);
    }
}
