namespace Lyt.PhotoPostPro.Panes.MetadataPane;

public sealed partial class KeywordViewModel : ViewModel<KeywordView>
{
    [ObservableProperty]
    public partial string Keyword { get; set; } = string.Empty;

    [RelayCommand]
    public async Task OnDelete()
    {
    }
}
