namespace Lyt.PhotoPostPro.Workflow.Library;

public sealed partial class LibraryViewModel :
    ViewModel<LibraryView>,
    IRecipient<LibraryLoadedMessage>,
    IRecipient<ThumbnailUpdatedMessage>,
    IRecipient<FolderTreeUpdatedMessage>,
    ISelectListener
{
    public enum Viewing
    {
        Captured,
        Added,
        Edited,
    }

    private const double YearButtonWidth = 76.0;
    private const double MonthButtonWidth = 120.0;
    private const double DayButtonWidth = 160.0;
    private const double OptionButtonWidth = 160.0;

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

    private Viewing selectedViewing;
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
    public partial List<SelectorButtonViewModel> Options { get; set; } = [];

    [ObservableProperty]
    public partial List<SelectorButtonViewModel> Years { get; set; } = [];

    [ObservableProperty]
    public partial List<SelectorButtonViewModel> Months { get; set; } = [];

    [ObservableProperty]
    public partial List<SelectorButtonViewModel> Days { get; set; } = [];

    [ObservableProperty]
    public partial bool HasSelection { get; set; }

    [ObservableProperty]
    public partial bool IsAddedSelected { get; set; }

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
        this.selectedViewing = Viewing.Captured;
        this.Subscribe<LibraryLoadedMessage>();
        this.Subscribe<ThumbnailUpdatedMessage>();
        this.Subscribe<FolderTreeUpdatedMessage>();
    }

    public void Receive(FolderTreeUpdatedMessage message)
        => Dispatch.OnUiThread(
                () => { this.ReceiveOnUiThread(message); }, DispatcherPriority.Background);

    public void ReceiveOnUiThread(FolderTreeUpdatedMessage message)
    {
        switch (message.FolderTreeKind)
        {
            default:
            case FolderTreeKind.Captured:
                break;

            case FolderTreeKind.Added:
                break;

            case FolderTreeKind.Edited:
                this.Options[^1].Select();
                break;
        }
    } 

    public void Receive(ThumbnailUpdatedMessage message)
        => Dispatch.OnUiThread(
                () => { this.ReceiveOnUiThread(message); }, DispatcherPriority.Background);

    public void ReceiveOnUiThread(ThumbnailUpdatedMessage message)
    {
        string path = message.Path;
        var list = this.LibraryThumbnailsPanelViewModel.Thumbnails;

        // Find old View model and remove it 
        var oldVm = (from vm in list where vm.Path == path select vm).FirstOrDefault();
        if (oldVm is null)
        {
            return;
        }

        list.Remove(oldVm);

        // Bring in the new one 
        if (this.model.LibraryManager.LoadedThumbnails.TryGetValue(path, out var thumbnail))
        {
            // Add to list 
            LibraryThumbnailViewModel libraryThumbnailViewModel =
                new(this, path, thumbnail.Metadata, thumbnail.ImageBytes);
            list.Add(libraryThumbnailViewModel);

            // make it the current selection 
            this.OnSelect(libraryThumbnailViewModel);
        }

        // Adjust order 
        this.LibraryThumbnailsPanelViewModel.Sort();
    }

    public void Receive(LibraryLoadedMessage message)
        => Dispatch.OnUiThread(() => { this.ReceiveOnUiThread(message); }, DispatcherPriority.Background);

    public void ReceiveOnUiThread(LibraryLoadedMessage message)
    {
        Debug.WriteLine(" Loaded: " + message.ImageCount + "  - Errors: " + message.ErrorCount);

        List<SelectorButtonViewModel> listOptions = [];
        string captured = this.Localize("Library.Option.Captured");
        var vm1 = new SelectorButtonViewModel(captured, OptionButtonWidth, this.OnSelectOption, "Captured");
        listOptions.Add(vm1);
        string added = this.Localize("Library.Option.Added");
        var vm2 = new SelectorButtonViewModel(added, OptionButtonWidth, this.OnSelectOption, "Added");
        listOptions.Add(vm2);
        string edited = this.Localize("Library.Option.Edited");
        var vm3 = new SelectorButtonViewModel(edited, OptionButtonWidth, this.OnSelectOption, "Edited");
        listOptions.Add(vm3);
        this.Options = listOptions;

        if (message.ImageCount == 0)
        {
            // TODO 
            // Toast: Your library is empty 
            return;
        }

        var folderTree = this.libraryMgr.CapturedFolderTree;
        if (folderTree is null)
        {
            return;
        }

        this.BuildCalendarButtons(folderTree);

        // Need to schedule so that the view is bound 
        Schedule.OnUiThread(
            120,
            () =>
            {
                // Select the 'Captured' option
                this.Options[0].Select();
            },
            DispatcherPriority.Background);
    }

    private void BuildCalendarButtons(FolderTree folderTree)
    {
        List<SelectorButtonViewModel> listYears = [];
        foreach (var year in folderTree.YearFolders)
        {
            var vm = new SelectorButtonViewModel(year.Year.ToString(), YearButtonWidth, this.OnSelectYear, year);
            listYears.Add(vm);
        }

        this.Years = listYears;
        this.Months = [];
        this.Days = [];
    }

    private void OnSelectOption(object? tag)
    {
        if (tag is not string optionKey || string.IsNullOrEmpty(optionKey))
        {
            return;
        }

        FolderTree? folderTree;
        if (optionKey == "Captured")
        {
            this.selectedViewing = Viewing.Captured;
            folderTree = this.libraryMgr.CapturedFolderTree;
            this.IsAddedSelected = false;
        }
        else if (optionKey == "Added")
        {
            this.selectedViewing = Viewing.Added;
            folderTree = this.libraryMgr.AddedFolderTree;
            this.IsAddedSelected = true;
        }
        else if (optionKey == "Edited")
        {
            this.selectedViewing = Viewing.Edited;
            folderTree = this.libraryMgr.EditedFolderTree;
            this.IsAddedSelected = false;
        }
        else
        {
            throw new InvalidOperationException($"Unknown option key: {optionKey}");
        }

        if (folderTree is null)
        {
            // No folder tree should be null 
            Debugger.Break();
            return;
        }

        this.BuildCalendarButtons(folderTree);

        if (this.Years.Count > 0)
        {
            // Need to schedule so that the view is bound 
            Schedule.OnUiThread(
                120,
                () =>
                {
                    // Select the first year
                    // TODO 
                    // Check if we can use : this.selectedYear and select it ;
                    this.Years[^1].Select();
                },
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

        if (this.selectedViewing == Viewing.Captured)
        {
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
        else
        {
            bool forAdded = this.selectedViewing == Viewing.Added;
            var files = this.FindFilesAddedOrEdited(this.selectedDay, this.selectedMonth, this.selectedYear, forAdded);
            AddFiles(files);
        }
    }

    private List<string> FindFilesAddedOrEdited(
        DayFolder? selectedDay, MonthFolder? selectedMonth, YearFolder selectedYear, bool forAdded)
    {
        var thumbnails = this.model.LibraryManager.LoadedThumbnails;
        List<string> list = new(thumbnails.Count);

        bool checkDay = selectedDay is not null;
        // ! Checked by check day 
        int sDay = checkDay ? selectedDay!.Day : -1;

        bool checkMonth = selectedMonth is not null;
        // ! Checked by check month
        int sMonth = checkMonth ? selectedMonth!.Month : -1;
        int sYear = selectedYear.Year;

        foreach (var thumbnail in thumbnails)
        {
            Metadata metadata = thumbnail.Value.Metadata;
            DateTime date =
                forAdded ?
                    metadata.AddedToLibraryUTC.ToLocalTime().Date :
                    metadata.LastEditedUTC.ToLocalTime().Date;
            if (date.Year != sYear)
            {
                continue;
            }

            if (checkMonth && date.Month != sMonth)
            {
                continue;
            }

            if (checkDay && date.Day != sDay)
            {
                continue;
            }

            list.Add(thumbnail.Key);
        }

        return list;
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
            this.model.ProcessImageFromMetadata(metadata, isNew, this.model.FileUidString, parameters);
            Dispatch.OnUiThread(
                () =>
                {
                    this.libraryMgr.UpdateEditedFile(metadata);
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
        if (isValid && obj is ConfirmRemoveDialogModel)
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

    [RelayCommand]
    public void OnRateAndCull()
    {
        if (this.LibraryThumbnailsPanelViewModel.Thumbnails.Count == 0)
        {
            // no images to process
            return;
        }

        // Launch the Rate and Cull view
        var shell = App.GetRequiredService<ShellViewModel>();
        shell.EnableAndSelect(ActivatedView.Culling);
    }
}
