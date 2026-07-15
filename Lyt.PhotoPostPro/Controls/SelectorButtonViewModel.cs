namespace Lyt.PhotoPostPro.Controls;

public sealed partial class SelectorButtonViewModel : ViewModel<SelectorButtonView>
{
    private readonly Action<object?> onSelect;
    private readonly object? tag = null;

    public SelectorButtonViewModel(string buttonText, double buttonWidth, Action<object?> onSelect, object? tag = null)
    {
        this.onSelect = onSelect;
        this.tag = tag;
        this.ButtonText = buttonText;
        this.ButtonWidth = buttonWidth; 
    }

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
    public partial string ButtonText { get; set; }

    [ObservableProperty]
    public partial double ButtonWidth { get; set; }

    [RelayCommand]
    public void OnSelect()
    {
        if ( !this.IsBound)
        {
            return; 
        }

        if ( this.View.IsSelected )
        {
            return; 
        }

        this.View.OnSelect();
        this.onSelect(this.tag);
    }

    public void Select()
    {
        this.View.OnSelect();
        this.onSelect(this.tag);
    }
}
