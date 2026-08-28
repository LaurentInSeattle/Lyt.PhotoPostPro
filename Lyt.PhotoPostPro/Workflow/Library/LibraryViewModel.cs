namespace Lyt.PhotoPostPro.Workflow.Library;

public sealed partial class LibraryViewModel :
    ViewModel<LibraryView>,
    IRecipient<LanguageChangedMessage>,
    IRecipient<LibraryLoadedMessage>,
    IRecipient<LibraryRemovedMessage>,
    IRecipient<LibraryMetadataUpdateMessage>,
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

    // TODO LATER
    // Make this an application setting 
    private const int CullingBatchSize = 20; // 48;

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

    [ObservableProperty]
    public partial bool IsCullButtonVisible { get; set; }

    [ObservableProperty]
    public partial bool IsCullTextVisible { get; set; }

    [ObservableProperty]
    public partial int SelectionRating { get; set; }

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

        this.Subscribe<LanguageChangedMessage>();
        this.Subscribe<LibraryLoadedMessage>();
        this.Subscribe<ThumbnailUpdatedMessage>();
        this.Subscribe<FolderTreeUpdatedMessage>();
        this.Subscribe<LibraryRemovedMessage>();
        this.Subscribe<LibraryMetadataUpdateMessage>();
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);

        // If equal to 3 we may have had a language change, so we need to select it again 
        if (this.Options.Count == 3)
        {
            // Need to schedule so that the newly created control is bound to its view model 
            Schedule.OnUiThread(80, () =>
            {
                int index = (int)this.selectedViewing;
                if (this.Options[index].IsBound)
                {
                    this.Options[index].Select();
                }
            }, DispatcherPriority.Background);
        }
    }

    public void Receive(LanguageChangedMessage message)
    {
        this.BuildLibraryOptions();

        // The newly created control is not bound yet
        // Selecting it will be done on activation 

        FolderTree? folderTree;
        if (this.selectedViewing == Viewing.Added)
        {
            folderTree = this.libraryMgr.AddedFolderTree;
        }
        else if (this.selectedViewing == Viewing.Captured)
        {
            folderTree = this.libraryMgr.CapturedFolderTree;
        }
        else // if (this.selectedViewing == Viewing.Edited)
        {
            folderTree = this.libraryMgr.EditedFolderTree;
        }

        if (folderTree is null)
        {
            return;
        }

        this.BuildCalendarButtons(folderTree);
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
                this.Options[^0].Select();
                break;

            case FolderTreeKind.Added:
                this.Options[^1].Select();
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
        this.LibraryThumbnailsPanelViewModel.Update(message.Path);
        var first  = this.LibraryThumbnailsPanelViewModel.GetFirstDisplayed();  
        if ( first is not  null)
        {
            // make it the current selection 
            this.OnSelect(first);
        }
    }

    public void Receive(LibraryLoadedMessage message)
        => Dispatch.OnUiThread(() => { this.ReceiveOnUiThread(message); }, DispatcherPriority.Background);

    public void ReceiveOnUiThread(LibraryLoadedMessage message)
    {
        Debug.WriteLine(" Loaded: " + message.ImageCount + "  - Errors: " + message.ErrorCount);

        this.BuildLibraryOptions();

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

    private void BuildLibraryOptions()
    {
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
        if (optionKey == "Added")
        {
            this.selectedViewing = Viewing.Added;
            folderTree = this.libraryMgr.AddedFolderTree;
            this.IsAddedSelected = true;
            this.IsCullButtonVisible = false;
        }
        else
        {
            this.IsAddedSelected = false;
            this.IsCullButtonVisible = false;

            if (optionKey == "Captured")
            {
                this.selectedViewing = Viewing.Captured;
                folderTree = this.libraryMgr.CapturedFolderTree;
            }
            else if (optionKey == "Edited")
            {
                this.selectedViewing = Viewing.Edited;
                folderTree = this.libraryMgr.EditedFolderTree;
            }
            else
            {
                throw new InvalidOperationException($"Unknown option key: {optionKey}");
            }
        }

        if (folderTree is null)
        {
            // No folder tree should be null 
            Debugger.Break();
            return;
        }

        this.LibraryThumbnailsPanelViewModel.SetViewingMode(this.selectedViewing);
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
        else
        {
            // Nothing has ever been edited : Clear the panel and clear selection 
            this.LibraryThumbnailsPanelViewModel.Clear();
            this.ClearSelection();
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
                if (this.libraryMgr.LoadedThumbnails.TryGetValue(path, out var thumbnail))
                {
                    LibraryThumbnailViewModel libraryThumbnailViewModel =
                        new(this, path, thumbnail.Metadata, thumbnail.ImageBytes);
                    list.Add(libraryThumbnailViewModel);
                }
            }

            this.LibraryThumbnailsPanelViewModel.Populate(list);

            var first = this.LibraryThumbnailsPanelViewModel.GetFirstDisplayed();
            if (first is not null)
            {
                this.OnSelect(first);
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

            this.IsCullButtonVisible = false;
            this.IsCullTextVisible = false;
        }
        else
        {
            bool forAdded = this.selectedViewing == Viewing.Added;
            var files = this.libraryMgr.FindFilesAddedOrEdited(
                this.selectedDay, this.selectedMonth, this.selectedYear, forAdded, out int zeroStarCount);
            AddFiles(files);

            this.IsCullTextVisible = forAdded && (zeroStarCount == 0);
            this.IsCullButtonVisible = forAdded && (zeroStarCount > 0);
        }
    }

    public void OnSelect(object selectedObject)
    {
        if (selectedObject is LibraryThumbnailViewModel libraryThumbnailViewModel)
        {
            this.HasSelection = true;
            this.selectedLibraryThumbnailViewModel = libraryThumbnailViewModel;
            this.SelectedThumbnail = libraryThumbnailViewModel.Thumbnail;
            var metadata = libraryThumbnailViewModel.Metadata;
            this.SelectionRating = metadata.Rating;
            if (this.SelectedThumnailMetadataViewModel is null)
            {
                this.SelectedThumnailMetadataViewModel = new MetadataViewModel(metadata);
            }
            else
            {
                this.SelectedThumnailMetadataViewModel.Update(metadata);
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
        List<ExistingPostProcessParameters> parameters = this.model.LibraryManager.EnumerateExistingEditParameters(metadata);

        // Launch dialogs if needed 
        if (parameters.Count == 0)
        {
            // New processing : no dialog 
            this.LaunchSpinProcessing(isNew: true, metadata, new ProcessParameters(), isReplayMode: false);
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
            this.LaunchSpinProcessing(isNew: true, metadata, new ProcessParameters(), isReplayMode: false);
        }
        else
        {
            // Grab info from dialog
            ProcessParameters? parameters = selectEditDialogModel.PostProcessParameters;
            bool isReplayMode = selectEditDialogModel.IsReplayMode;
            string fileUid = this.model.FileUidString;
            if (parameters is null || string.IsNullOrWhiteSpace(fileUid))
            {
                return;
            }

            // Continued process: isNew is false, recycled parameters 
            this.LaunchSpinProcessing(isNew: false, metadata, parameters, isReplayMode);
        }
    }

    private void LaunchSpinProcessing(bool isNew, Metadata metadata, ProcessParameters parameters, bool isReplayMode)
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
                    this.LaunchProcessing(isReplayMode);
                    this.SpinWait(start: false);
                },
                DispatcherPriority.ApplicationIdle);
        });
    }

    private void LaunchProcessing(bool isReplayMode)
    {
        var workflow = this.model.CurrentWorkflow;
        if (workflow is null)
        {
            this.Logger.Warning("Failed to create post process from dropped file: ");
            // TODO : Show error message to user
            return;
        }

        if (isReplayMode)
        {
            // Launch Processing Dialog 
            // Launch dialog for new processing
            if (this.dialogService is DialogService modalService)
            {
                modalService.RunViewModelModal(
                    this.shellViewModel.ModalHost, new ProcessingDialogModel(workflow), this.OnProcessingComplete);
            }
        }
        else
        {
            ActivateProcessView();
        }
    }

    private void OnProcessingComplete(object arg, bool _)
        => Schedule.OnUiThread(120, () =>
        {
            ActivateProcessView(); 
        }, DispatcherPriority.ApplicationIdle);


    private static void ActivateProcessView()
    {
        var shell = App.GetRequiredService<ShellViewModel>();
        shell.EnableAndSelect(ActivatedView.Process);
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

            // We should receive a LibraryRemovedMessage and execute the code below
            // See:   Receive(LibraryRemovedMessage message)
        }
    }

    public void Receive(LibraryRemovedMessage message)
        => Dispatch.OnUiThread(() => { this.ReceiveOnUiThread(message); }, DispatcherPriority.Background);

    public void ReceiveOnUiThread(LibraryRemovedMessage message)
    {
        if (!this.LibraryThumbnailsPanelViewModel.TryFindThumbnail(
            message.Metadata, out LibraryThumbnailViewModel? thumbnail))
        {
            // Nothing to do 
            return;
        }

        // Remove it the list 
        this.LibraryThumbnailsPanelViewModel.Remove(thumbnail);

        if (this.selectedLibraryThumbnailViewModel == null)
        {
            // If it was not the selection, Nothing to do 
            return;
        }

        this.ClearSelection();
    } 

    private void ClearSelection ()
    { 
        // Clear this view 
        this.SelectedThumbnail = null;
        this.SelectedThumnailMetadataViewModel = null;

        // Clear selection 
        this.selectedLibraryThumbnailViewModel = null;
        this.HasSelection = false;
    }

    public void Receive(LibraryMetadataUpdateMessage message)
        => Dispatch.OnUiThread(() => { this.ReceiveOnUiThread(message); }, DispatcherPriority.Background); 

    private void ReceiveOnUiThread(LibraryMetadataUpdateMessage message)
    {
        var metadata = message.Metadata;
        if (!this.LibraryThumbnailsPanelViewModel.TryFindThumbnail(
                metadata, out LibraryThumbnailViewModel? thumbnail))
        {
            // Not found, Nothing to do 
            return;
        }

        thumbnail.Update(metadata);
    }

    [RelayCommand]
    public void OnRateAndCull()
    {
        if (this.LibraryThumbnailsPanelViewModel.IsEmpty)
        {
            // no images to process
            return;
        }

        // Enumerate thumbs with zero rating, returning their paths
        var filteredFiles = this.LibraryThumbnailsPanelViewModel.GetUnratedThumbnailsPaths();

        // Limit list to first in the allowed batch size
        var files = filteredFiles.Take(CullingBatchSize).ToList();
        if (files.Count == 0)
        {
            // no unrated images left to process
            return;
        }

        var rateAndCull = App.GetRequiredService<CullingViewModel>();
        rateAndCull.Initialize(files);

        // Launch the Rate and Cull view
        var shell = App.GetRequiredService<ShellViewModel>();
        shell.EnableAndSelect(ActivatedView.Culling);
    }
}
