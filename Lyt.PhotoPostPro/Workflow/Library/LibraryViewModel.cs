namespace Lyt.PhotoPostPro.Workflow.Library;

using SixLabors.ImageSharp.Drawing;

public sealed partial class LibraryViewModel :
    ViewModel<LibraryView>,
    IRecipient<LibraryLoadedMessage>,
    IRecipient<ThumbnailUpdatedMessage>,
    IDropPathHandler,
    ISelectListener
{
    private const double YearButtonWidth = 76.0;
    private const double MonthButtonWidth = 120.0;
    private const double DayButtonWidth = 160.0;

    // Will NEED to localize
    private static readonly string[] MonthString =
    [
        "Library.Month.January",
        "Library.Month.February",
        "Library.Month.March",
        "Library.Month.April",
        "Library.Month.May",
        "Library.Month.June",
        "Library.Month.July",
        "Library.Month.August",
        "Library.Month.September",
        "Library.Month.October",
        "Library.Month.November",
        "Library.Month.December",
    ];

    // Will NEED to localize
    private static readonly string[] DayString =
    [
        // Order MUST match the DayOfWeek enumeration, so Sunday comes first 
        "Library.Day.Sunday",
        "Library.Day.Monday",
        "Library.Day.Tuesday",
        "Library.Day.Wednesday",
        "Library.Day.Thursday",
        "Library.Day.Friday",
        "Library.Day.Saturday",
    ];

    private readonly PhotoPostProModel model;
    private readonly IDialogService dialogService;
    private readonly ShellViewModel shellViewModel;
    private readonly LibraryManager libraryMgr;

    private YearFolder? selectedYear;
    private MonthFolder? selectedMonth;
    private DayFolder? selectedDay;
    private LibraryThumbnailViewModel? selectedLibraryThumbnailViewModel;

    [ObservableProperty]
    public partial SpinViewModel SpinViewModel { get; set; }

    [ObservableProperty]
    public partial WriteableBitmap? SelectedThumbnail { get; set; }

    [ObservableProperty]
    public partial MetadataViewModel? SelectedThumnailMetadataViewModel { get; set; }

    [ObservableProperty]
    public partial LibraryThumbnailsPanelViewModel LibraryThumbnailsPanelViewModel { get; set; }

    [ObservableProperty]
    public partial List<SelectorButtonViewModel> Years { get; set; } = [];

    [ObservableProperty]
    public partial List<SelectorButtonViewModel> Months { get; set; } = [];

    [ObservableProperty]
    public partial List<SelectorButtonViewModel> Days { get; set; } = [];

    [ObservableProperty]
    public partial bool HasSelection { get; set; }

    public LibraryViewModel(
        PhotoPostProModel photoPostProModel,
        IDialogService dialogService,
        ShellViewModel shellViewModel)
    {
        this.model = photoPostProModel;
        this.libraryMgr = photoPostProModel.LibraryManager;
        this.dialogService = dialogService;
        this.shellViewModel = shellViewModel;
        this.LibraryThumbnailsPanelViewModel = new(this.model, this);
        this.SpinViewModel = new SpinViewModel()
        {
            IsVisible = false,
            IsActive = false,
        };

        this.HasSelection = false;
        this.Subscribe<LibraryLoadedMessage>();
        this.Subscribe<ThumbnailUpdatedMessage>();
    }

    public void Receive(ThumbnailUpdatedMessage message)
    {
        string path = message.Path; 
        var list = this.LibraryThumbnailsPanelViewModel.Thumbnails; 

        // Find old View model and remove it 
        var oldVm = (from vm in list where vm.Path == path select vm).FirstOrDefault();
        if ( oldVm is null)
        {
            return; 
        }

        list.Remove(oldVm);

        // Bring in the new one 
        if (this.model.LibraryManager.LoadedThumbnails.TryGetValue(path, out var thumbnail))
        {
            LibraryThumbnailViewModel libraryThumbnailViewModel =
                new(this, path, thumbnail.Metadata, thumbnail.ImageBytes);
            list.Add(libraryThumbnailViewModel);
        }

        // Adjust order 
        this.LibraryThumbnailsPanelViewModel.Sort(); 
    }

    public void Receive(LibraryLoadedMessage message)
    {
        // TODO
        // Show some wait spinner 

        Debug.WriteLine(" Loaded: " + message.ImageCount + "  - Errors: " + message.ErrorCount);

        var folderTree = this.libraryMgr.FolderTree;
        if (folderTree is null)
        {
            return;
        }

        if (message.ImageCount == 0)
        {
            // TODO 
            // Toast: Your library is empty 
            return;
        }

        List<SelectorButtonViewModel> listYears = [];
        foreach (var year in folderTree.YearFolders)
        {
            var vm = new SelectorButtonViewModel(year.Year.ToString(), YearButtonWidth, this.OnSelectYear, year);
            listYears.Add(vm);
        }

        this.Years = listYears;

        if (this.Years.Count > 0)
        {
            // Need to schedule so that the view is bound 
            Schedule.OnUiThread(
                66,
                () => { this.Years[0].Select(); },
                DispatcherPriority.Background);
        }
    }

    private void OnSelectYear(object? tag)
    {
        if (tag is not YearFolder year)
        {
            return;
        }

        this.selectedYear = year;
        this.selectedMonth = null;
        this.selectedDay = null;

        List<SelectorButtonViewModel> listMonths = [];
        foreach (var month in year.MonthFolders)
        {
            string monthString = this.Localize(MonthString[month.Month - 1]);
            var vm = new SelectorButtonViewModel(monthString, MonthButtonWidth, this.OnSelectMonth, month);
            listMonths.Add(vm);
        }

        this.Months = listMonths;
        this.Days = [];

        this.LoadImages();
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
            string dayOfWeek = this.Localize(DayString[day.DayOfWeek]);
            string dayString = dayOfWeek + " " + day.Day.ToString();
            var vm = new SelectorButtonViewModel(dayString, DayButtonWidth, this.OnSelectDay, day);
            listDays.Add(vm);
        }

        this.Days = listDays;

        this.LoadImages();
    }

    private void OnSelectDay(object? tag)
    {
        if (tag is not DayFolder day)
        {
            return;
        }

        this.selectedDay = day;

        this.LoadImages();
    }

    private void LoadImages()
    {
        if (this.selectedYear is null)
        {
            Debug.WriteLine(" No selection");
            return;
        }

        void AddFiles(List<string> files)
        {
            List<LibraryThumbnailViewModel> list = [];
            foreach (string path in files)
            {
                if (this.model.LibraryManager.LoadedThumbnails.TryGetValue(path, out var thumbnail))
                {
                    LibraryThumbnailViewModel libraryThumbnailViewModel =
                        new(this, path, thumbnail.Metadata, thumbnail.ImageBytes);
                    list.Add(libraryThumbnailViewModel);
                }
            }

            this.LibraryThumbnailsPanelViewModel.Thumbnails = new(list);

            var thumbnails = this.LibraryThumbnailsPanelViewModel.Thumbnails;
            if (thumbnails.Count == 0)
            {
                // The logic of the folder system should prevent an empty list 
                // but removing files from the library does not remove its directory 
                // therefore there could be empty slots 
            }
            else
            {
                this.OnSelect(thumbnails[0]);
            }
        }

        if (this.selectedDay is not null)
        {
            AddFiles(this.selectedDay.MetadataFiles);
        }
        else if (this.selectedMonth is not null)
        {
            AddFiles(this.selectedMonth.MetadataFiles());
        }
        else if (this.selectedYear is not null)
        {
            AddFiles(this.selectedYear.MetadataFiles());
        }
    }


    public void OnDropPath(string path, bool isDirectory)
    {
    }

    public void OnSelect(object selectedObject)
    {
        if (selectedObject is LibraryThumbnailViewModel libraryThumbnailViewModel)
        {
            this.HasSelection = true; 
            this.selectedLibraryThumbnailViewModel = libraryThumbnailViewModel;
            this.SelectedThumbnail = libraryThumbnailViewModel.Thumbnail;
            if (this.SelectedThumnailMetadataViewModel is null)
            {
                this.SelectedThumnailMetadataViewModel = new MetadataViewModel(libraryThumbnailViewModel.Metadata);
            }
            else
            {
                this.SelectedThumnailMetadataViewModel.Update(libraryThumbnailViewModel.Metadata);
            }
        }
    }

    private void SpinWait(bool start = true)
    {
        this.SpinViewModel.IsVisible = start;
        this.SpinViewModel.IsActive = start;
    }


    [RelayCommand]
    public void OnProcess()
    {
        if (this.selectedLibraryThumbnailViewModel is null ||
            this.selectedLibraryThumbnailViewModel.Metadata is null)
        {
            return;
        }

        var metadata = this.selectedLibraryThumbnailViewModel.Metadata;
        List<ExistingPostProcessParameters> parameters = this.model.LibraryManager.EnumerateExistingParameters(metadata);

        // Launch dialogs if needed 
        if (parameters.Count == 0)
        {
            // New processing : no dialog 
            this.LaunchSpinProcessing(isNew: true, metadata, new PostProcessParameters());
        }
        else
        {
            // Launch dialog for new processing
            if (this.dialogService is DialogService modalService)
            {
                modalService.RunViewModelModal(
                    this.shellViewModel.ModalHost, new SelectEditDialogModel(parameters), this.OnEditSelected);
            }
        }
    }

    private void OnEditSelected(object? obj, bool isValid)
    {
        if (!isValid || obj is not SelectEditDialogModel selectEditDialogModel)
        {
            return;
        } 

        if (this.selectedLibraryThumbnailViewModel is null ||
            this.selectedLibraryThumbnailViewModel.Metadata is null)
        {
            return;
        }

        var metadata = this.selectedLibraryThumbnailViewModel.Metadata;
        if (selectEditDialogModel.IsStartOver)
        {
            this.LaunchSpinProcessing(isNew: true, metadata, new PostProcessParameters()); 
        }
        else
        {
            // Grab info from dialog
            PostProcessParameters? parameters = selectEditDialogModel.PostProcessParameters; 
            string fileUid = this.model.FileUidString;
            if (parameters is null || string.IsNullOrWhiteSpace(fileUid))
            {
                return;
            }

            selectEditDialogModel.Cancel(); 

            // Continued process: isNew is false, recycled parameters 
            this.LaunchSpinProcessing(isNew: false, metadata, parameters);
        }
    }

    private void LaunchSpinProcessing(bool isNew, Metadata metadata, PostProcessParameters parameters)
    {
        // Always launch a spinner for big or small files 
        this.SpinWait(start: true);
        Task.Run(() =>
        {
            // New processing 
            this.model.ProcessImageFromMetadata(
                metadata, isNew, this.model.FileUidString, parameters);
            Dispatch.OnUiThread(
                () =>
                {
                    this.LaunchProcessing();
                    this.SpinWait(start: false);
                },
                DispatcherPriority.ApplicationIdle);
        });
    }
    private void LaunchProcessing()
    {
        var postProcess = this.model.CurrentPostProcess;
        if (postProcess is not null)
        {
            var shell = App.GetRequiredService<ShellViewModel>();
            shell.EnableAndSelect(ActivatedView.Process);
        }
        else
        {
            this.Logger.Warning("Failed to create post process from dropped file: ");
            // TODO : Show error message to user
        }

        var mainWindow = App.MainWindow;
        if (mainWindow.CanMaximize)
        {
            mainWindow.WindowState = WindowState.Maximized;
        }
    }

    [RelayCommand]
    public void OnNavigate()
    {
        if (this.selectedLibraryThumbnailViewModel is null ||
            this.selectedLibraryThumbnailViewModel.Metadata is null)
        {
            return;
        }

        PhotoPostProModel.NavigateToImageFolder(this.selectedLibraryThumbnailViewModel.Metadata);
    }

    [RelayCommand]
    public void OnRemove()
    {
        if (this.selectedLibraryThumbnailViewModel is null ||
            this.selectedLibraryThumbnailViewModel.Metadata is null)
        {
            return;
        }

        if (this.dialogService is DialogService modalService)
        {
            modalService.RunViewModelModal(
                this.shellViewModel.ModalHost, new ConfirmRemoveDialogModel(), this.OnRemoveConfirmed);
        }
    }

    private void OnRemoveConfirmed(object? obj, bool isValid)
    {
        if (isValid && obj is ConfirmRemoveDialogModel confirmRemoveDialogModel)
        {
            if (this.selectedLibraryThumbnailViewModel is null ||
                this.selectedLibraryThumbnailViewModel.Metadata is null)
            {
                return;
            }

            var metadata = this.selectedLibraryThumbnailViewModel.Metadata;
            if (!this.libraryMgr.Remove(metadata))
            {
                // Failed:
                // TODO : Message user 
                return; 
            } 

            // Clear this view 
            this.SelectedThumbnail = null;
            this.SelectedThumnailMetadataViewModel = null;

            // Clear the list 
            this.LibraryThumbnailsPanelViewModel.Thumbnails.Remove(this.selectedLibraryThumbnailViewModel);

            // Clear selection 
            this.selectedLibraryThumbnailViewModel = null;
            this.HasSelection = false;
        }
    }
}
