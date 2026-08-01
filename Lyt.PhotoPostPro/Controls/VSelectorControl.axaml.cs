namespace Lyt.PhotoPostPro.Controls;

public partial class VSelectorControl : UserControl
{
    public static readonly StyledProperty<List<SelectorButtonViewModel>> SelectorButtonsProperty =
            AvaloniaProperty.Register<VSelectorControl, List<SelectorButtonViewModel>>(
                nameof(SelectorButtons),
                defaultValue: [],
                inherits: false,
                defaultBindingMode: BindingMode.OneWay,
                validate: null,
                coerce: CoerceSelectorButtons,
                enableDataValidation: false);

    public List<SelectorButtonViewModel> SelectorButtons
    {
        get => this.GetValue(SelectorButtonsProperty);
        set
        {
            this.SetValue(SelectorButtonsProperty, value);
            this.SelectorItemsControl.ItemsSource = value;
        }
    }

    /// <summary> Coerces the SelectorButtons value. </summary>
    private static List<SelectorButtonViewModel> CoerceSelectorButtons(
        AvaloniaObject sender, List<SelectorButtonViewModel> value)
    {
        if (sender is VSelectorControl vSelectorControl)
        {
            vSelectorControl.SelectorItemsControl.ItemsSource = value;
            if( value is not null && value.Count > 1 )
            {
                var first = value[0];
                first.OnSelect(); 
            }

        }

        // ! Avalonia 
        return value!;
    }

    public VSelectorControl()
    {
        this.InitializeComponent();
    }
}