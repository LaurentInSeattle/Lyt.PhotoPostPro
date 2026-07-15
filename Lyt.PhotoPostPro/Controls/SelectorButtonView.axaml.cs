namespace Lyt.PhotoPostPro.Controls;

using global::Avalonia.VisualTree;

public partial class SelectorButtonView : View
{
    public SelectorButtonView() => this.InitializeComponent();

    public bool IsSelected => this.NavigationIndicator.IsVisible; 

    public void OnSelect()
    {
        this.NavigationIndicator.IsVisible = true;
        this.NavigationButton.IsSelected = true;
        
        // Find parent ItemsControl
        var itemsControl = this.FindParentControl<ItemsControl>();
        if (itemsControl is not null)
        {
            // Find other SelectorButtonView's
            var descendants = itemsControl.GetVisualDescendants().OfType<SelectorButtonView>();
            // and deselect them  
            foreach (var descendant in descendants)
            {
                if (descendant is SelectorButtonView selectorButtonView)
                {
                    // should be 
                    if ( selectorButtonView != this)
                    {
                        selectorButtonView.OnUnselect();
                    }
                } 
            } 
        }
    }

    public void OnUnselect()
    {
        this.NavigationIndicator.IsVisible = false;
        this.NavigationButton.IsSelected = false;
        this.NavigationButton.UpdateVisualState(); 
    } 
}