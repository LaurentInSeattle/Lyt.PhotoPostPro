namespace Lyt.PhotoPostPro.Panes.MetadataPane;

public interface ICanDeleteKeyword
{
    void DeleteKeyword(string keyword);
}

public sealed partial class KeywordViewModel(ICanDeleteKeyword canDeleteKeyword, string keyword) 
    : ViewModel<KeywordView>
{
    private readonly ICanDeleteKeyword canDeleteKeyword = canDeleteKeyword;

    [ObservableProperty]
    public partial string Keyword { get; set; } = keyword;

    [RelayCommand]
    public void OnDelete() => this.canDeleteKeyword.DeleteKeyword(this.Keyword);
}
