namespace Lyt.PhotoPostPro.Workflow.Documentation;


public sealed record class NavigateMessage (NavigateMessage.NavigateTo Navigate, int PageNumber = 1)
{
    public enum NavigateTo
    {
        First, 
        Previous, 
        Next, 
        Last, 
        PageNumber, 
    }
}

public sealed partial class DocumentationToolbarViewModel : ViewModel<DocumentationToolbarView>
{
#pragma warning disable CA1822 // Mark members as static
    // RelayCommand's cannot be static 

    [RelayCommand]
    public void OnFirst() =>
        new NavigateMessage(NavigateMessage.NavigateTo.First).Publish();

    [RelayCommand]
    public void OnPrevious() =>
        new NavigateMessage(NavigateMessage.NavigateTo.Previous).Publish();

    [RelayCommand]
    public void OnNext() =>
        new NavigateMessage(NavigateMessage.NavigateTo.Next).Publish();

    [RelayCommand]
    public void OnLast() =>
        new NavigateMessage(NavigateMessage.NavigateTo.Last).Publish();



    [RelayCommand]
    public void OnFullscreen() =>
        new ToolbarCommandMessage(ToolbarCommandMessage.ToolbarCommand.GoFullscreen).Publish();

#pragma warning restore CA1822
}
