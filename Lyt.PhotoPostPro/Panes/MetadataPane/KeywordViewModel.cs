namespace Lyt.PhotoPostPro.Panes.MetadataPane;

public interface ICanDeleteKeyword
{
    void DeleteKeyword(string keyword);
}

public sealed partial class KeywordViewModel : ViewModel<KeywordView>
{
    private readonly ICanDeleteKeyword canDeleteKeyword; 

    public KeywordViewModel(ICanDeleteKeyword canDeleteKeyword, string keyword)
    {
        this.canDeleteKeyword = canDeleteKeyword;
        this.Keyword = keyword;
    }

    [ObservableProperty]
    public partial string Keyword { get; set; } = string.Empty;

    [RelayCommand]
    public void OnDelete() => this.canDeleteKeyword.DeleteKeyword(this.Keyword);
}
