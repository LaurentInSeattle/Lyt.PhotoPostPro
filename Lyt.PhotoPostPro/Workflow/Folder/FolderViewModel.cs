namespace Lyt.PhotoPostPro.Workflow.Folder;

using Lyt.PhotoPostPro.Model.LibraryModels;

public sealed partial class FolderViewModel : ViewModel<FolderView>, IDropPathHandler
{
    private const double YearButtonWidth = 90.0;
    private const double MonthButtonWidth = 110.0;
    private const double DayButtonWidth = 134.0;


    private readonly PhotoPostProModel model;

    // Will NEED to localize
    private static readonly string[] MonthString =
    [
        "January",
        "February",
        "March",
        "April",
        "May",
        "June",
        "July",
        "August",
        "September",
        "October",
        "November",
        "December",
    ];

    // Will NEED to localize
    private static readonly string[] DayString =
    [
        // Order MUST match the DayOfWeek enumeration, so Sunday comes first 
        "Sunday",
        "Monday",
        "Tuesday",
        "Wednesday",
        "Thursday",
        "Friday",
        "Saturday",
    ];

    private YearFolder? selectedYear;
    private MonthFolder? selectedMonth;
    private DayFolder? selectedDay;

    public FolderViewModel(PhotoPostProModel photoPostProModel) 
    {
        this.model = photoPostProModel;
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);

        var folderTree = this.model.LibraryManager.FolderTree;
        if (folderTree is null)
        {
            return ;
        }

        List<SelectorButtonViewModel> listYears = [];
        foreach (var year in folderTree.YearFolders)
        {
            var vm = new SelectorButtonViewModel(year.Year.ToString(), YearButtonWidth, this.OnSelectYear, year);
            listYears.Add(vm); 
        } 

        this.Years = listYears;
        Schedule.OnUiThread(66, this.ActivateUi, DispatcherPriority.Background); 
    }

    [ObservableProperty]
    public partial List<SelectorButtonViewModel> Years { get; set; } = [];

    [ObservableProperty]
    public partial List<SelectorButtonViewModel> Months { get; set; } = [];

    [ObservableProperty]
    public partial List<SelectorButtonViewModel> Days { get; set; } = [];

    private void OnSelectYear(object? tag )
    {
        if ( tag is not YearFolder year )
        {
            return; 
        }

        this.selectedYear = year;
        this.selectedMonth = null;
        this.selectedDay = null;

        List<SelectorButtonViewModel> listMonths = [];
        foreach (var month in year.MonthFolders)
        {
            string monthString = MonthString[month.Month-1]; 
            var vm = new SelectorButtonViewModel(monthString, MonthButtonWidth, this.OnSelectMonth, month);
            listMonths.Add(vm);
        }

        this.Months = listMonths;
        this.Days = [];
    }

    private void OnSelectMonth(object? tag)
    {
        if (tag is not MonthFolder month)
        {
            return;
        }

        this.selectedMonth = month;
        this.selectedDay = null;

        List<SelectorButtonViewModel> listDays = [];
        foreach (var day in month.DayFolders)
        {
            string dayString = DayString[day.DayOfWeek] + " " + day.Day.ToString();
            var vm = new SelectorButtonViewModel(dayString, DayButtonWidth, this.OnSelectDay, day);
            listDays.Add(vm);
        }

        this.Days = listDays;
    }

    private void OnSelectDay(object? tag)
    {
        if (tag is not DayFolder day)
        {
            return;
        }

        this.selectedDay = day;

        this.LoadImages ();
    } 

    private void ActivateUi ()
    {
        this.Years[0].Select();
    }

    private void LoadImages()
    {
        if ( this.selectedYear is null || this.selectedYear is null || this.selectedYear is null)
        {
            Debug.WriteLine(" No selection");
            return; 
        }
    }


    public void OnDropPath(string path, bool isDirectory)
    {
    }
}
