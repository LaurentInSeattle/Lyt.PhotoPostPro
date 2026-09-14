namespace Lyt.PhotoPostPro.Panes.MetadataPane;

public sealed partial class LibraryAddKeywordsDialogModel :
    DialogViewModel<LibraryAddKeywordsDialog, object>, 
    ICanDeleteKeyword
{
    [ObservableProperty]
    public partial string Message { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string KeywordsText { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<KeywordViewModel> Keywords { get; set; } = [];

    public LibraryAddKeywordsDialogModel(HashSet<string> sourceKeywords)
    {
        this.CanEnter = true;
        this.CanEscape = true;
        this.Title = this.Localize("LibraryAdd.Dialog.Keywords.Title");
        this.Message = this.Localize("LibraryAdd.Dialog.Keywords.Message");
        this.KeywordsText = string.Empty;

        List<KeywordViewModel> keywords = [];
        foreach (string keyword in sourceKeywords)
        {
            keywords.Add(new KeywordViewModel(this, keyword.Capitalize()));
        }

        this.Keywords = new(keywords);
    }

    [RelayCommand]
    public void OnCancel() => this.Cancel();

    [RelayCommand]
    public void OnAdd()
    {
        string keywordsText = this.KeywordsText.Trim();
        if (string.IsNullOrWhiteSpace(keywordsText))
        {
            return;
        }

        char[] separators = [' ', '\n', '\r', ',', ';'];
        string[] tokens =
            keywordsText.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens is null || tokens.Length == 0)
        {
            return;
        }

        foreach (string token in tokens)
        {
            // Check if already there 
            var foundVm =
                (from vm in this.Keywords
                 where vm.Keyword.Equals(token, StringComparison.CurrentCultureIgnoreCase)
                 select vm).FirstOrDefault();
            if ( foundVm is null)
            {
                // Not there: add it 
                this.Keywords.Add(new KeywordViewModel(this, token.Capitalize()));
            }
            // Else: do not add and continue
        }

        this.KeywordsText = string.Empty;
    }

    public void DeleteKeyword(string keyword)
    {
        var foundVm =
            (from vm in this.Keywords
             where vm.Keyword.Equals(keyword, StringComparison.CurrentCultureIgnoreCase)
             select vm).FirstOrDefault();
        if (foundVm is not null)
        {
            this.Keywords.Remove(foundVm);
        }
    }
}
