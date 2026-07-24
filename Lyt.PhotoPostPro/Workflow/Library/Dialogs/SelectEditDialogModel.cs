namespace Lyt.PhotoPostPro.Workflow.Library.Dialogs;

public sealed partial class SelectEditDialogModel : DialogViewModel<SelectEditDialog, object>
{
    private bool isInitializing;

    [ObservableProperty]
    public partial string? Message { get; set; }

    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial List<string> ParametersStrings { get; set; }

    public List<ExistingPostProcessParameters> PostProcessParametersList { get; private set; }

    public bool IsStartOver { get; private set; }

    public PostProcessParameters? PostProcessParameters { get; private set; }

    public string FileUidString { get; private set; } = string.Empty;

    public SelectEditDialogModel(List<ExistingPostProcessParameters> postProcessParametersList)
    {
        this.IsStartOver = true;
        this.CanEnter = false;
        this.CanEscape = true;
        this.Title = "Start Over or Continue ?";
        this.Message =
            "This image has already been edited. Do you want to start over or use previous settings?" +
            "\n\nIf so, select previously updated settings in the 'combo-box below and click 'Continue'." +
            "\n\nIf not, just click 'Start Over'.";
        var sortedList = (from ppp in postProcessParametersList
                          orderby ppp.PostProcessParameters.Updated descending
                          select ppp)
                          .ToList();
        var strings = (from ppp in sortedList
                       select string.Format(
                           "Last Updated:  {0}  at:  {1}", 
                           ppp.PostProcessParameters.Updated.ToLongDateString(),
                           ppp.PostProcessParameters.Updated.ToLongTimeString()))
                       .ToList();
        this.PostProcessParametersList = sortedList;

        With.Flag(ref this.isInitializing, () =>
            {
                this.ParametersStrings = strings;
                this.SelectedParametersIndex = 0;
            }); 
    }

    [ObservableProperty]
    public partial int SelectedParametersIndex { get; set; }

    partial void OnSelectedParametersIndexChanged(int value)
    {
        // Do not change when initializing 
        if (this.isInitializing)
        {
            return;
        }

        if (value < 0 || value >= this.PostProcessParametersList.Count)
        {
            return; 
        }

        var eppp = this.PostProcessParametersList[value]; 
        this.FileUidString = eppp.FileUidString;
        this.PostProcessParameters = eppp.PostProcessParameters;
    }

    [RelayCommand]
    public async Task OnCancel() => this.Cancel();

    [RelayCommand]
    public async Task OnStartOver()
    {
        this.IsStartOver = true;
        this.TrySaveAndClose();
    }

    [RelayCommand]
    public async Task OnContinue()
    {
        this.IsStartOver = false;
        this.TrySaveAndClose();
    }
}
