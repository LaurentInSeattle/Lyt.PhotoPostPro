namespace Lyt.PhotoPostPro.Workflow.Documentation;

public sealed record class DocPageNavigateMessage(
    DocPageNavigateMessage.NavigateTo Navigate, int PageNumber = 1)
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

public sealed partial class DocumentationToolbarViewModel :
    ViewModel<DocumentationToolbarView>,
    IRecipient<PdfPageInViewMessage>,
    IRecipient<PdfLoadedStatusMessage>
{
    [ObservableProperty]
    public partial string CurrentPage { get; set; } = string.Empty;

    public DocumentationToolbarViewModel()
    {
        this.Subscribe<PdfPageInViewMessage>();
        this.Subscribe<PdfLoadedStatusMessage>();
    }

#pragma warning disable CA1822 // Mark members as static
    // RelayCommand's cannot be static 

    [RelayCommand]
    public void OnFirst() =>
        new DocPageNavigateMessage(DocPageNavigateMessage.NavigateTo.First).Publish();

    [RelayCommand]
    public void OnPrevious() =>
        new DocPageNavigateMessage(DocPageNavigateMessage.NavigateTo.Previous).Publish();

    [RelayCommand]
    public void OnNext() =>
        new DocPageNavigateMessage(DocPageNavigateMessage.NavigateTo.Next).Publish();

    [RelayCommand]
    public void OnLast() =>
        new DocPageNavigateMessage(DocPageNavigateMessage.NavigateTo.Last).Publish();

    [RelayCommand]
    public void OnFullscreen() =>
        new ToolbarCommandMessage(ToolbarCommandMessage.ToolbarCommand.GoFullscreen).Publish();

#pragma warning restore CA1822

    public void Receive(PdfPageInViewMessage message)
        => this.CurrentPage = string.Format("{0} / {1}", message.PageNumber, message.PageCount);

    public void Receive(PdfLoadedStatusMessage message)
        // Need to wait because the activation system will make the toolbar visible 
        => Schedule.OnUiThread(60, () =>
            {
                this.View.IsVisible = message.Complete; 
            }, DispatcherPriority.Background); 
}
