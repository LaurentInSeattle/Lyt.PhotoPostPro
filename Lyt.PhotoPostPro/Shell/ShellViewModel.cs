namespace Lyt.PhotoPostPro.Shell;

using static Messaging.ApplicationMessagingExtensions;

public sealed partial class ShellViewModel
    : ViewModel<ShellView>,
    IRecipient<ToolbarCommandMessage>,
    IRecipient<LanguageChangedMessage>
{
    private readonly PhotoPostProModel photoPostProModel;
    private readonly Fullscreen fullscreen;
    private readonly IToaster toaster;

    [ObservableProperty]
    public partial bool MainToolbarIsVisible { get; set; }

    private ViewSelector<ActivatedView>? viewSelector;
    public bool isFirstActivation;

    public ShellViewModel(PhotoPostProModel photoPostProModel, IToaster toaster)
    {
        this.photoPostProModel = photoPostProModel;
        this.toaster = toaster;
        this.fullscreen = new Fullscreen(App.MainWindow);

        this.Subscribe<ToolbarCommandMessage>();
        this.Subscribe<LanguageChangedMessage>();
    }

    public void Receive(LanguageChangedMessage _)
    {
    }

    public void Receive(ToolbarCommandMessage message)
    {
        if (message.Command == ToolbarCommandMessage.ToolbarCommand.GoFullscreen)
        {
            var vm = App.GetRequiredService<ProcessViewModel>();
            this.fullscreen.GoFullscreen(this.View.ShellViewContent, vm.View);
        }
        else if (message.Command == ToolbarCommandMessage.ToolbarCommand.BackToWindowed)
        {
            this.fullscreen.ReturnToWindowed();
        }
    }

    public override void OnViewLoaded()
    {
        this.Logger.Debug("OnViewLoaded begins");

        base.OnViewLoaded();
        if (this.View is null)
        {
            throw new Exception("Failed to startup...");
        }

        // Select default language 
        string preferredLanguage = this.photoPostProModel.Language;
        this.Logger.Debug("Language: " + preferredLanguage);
        this.Localizer.SelectLanguage(preferredLanguage);
        Thread.CurrentThread.CurrentCulture = new CultureInfo(preferredLanguage);
        Thread.CurrentThread.CurrentUICulture = new CultureInfo(preferredLanguage);

        this.Logger.Debug("OnViewLoaded language loaded");

        // Create all statics views and bind them 
        this.SetupWorkflow();
        this.Logger.Debug("OnViewLoaded SetupWorkflow complete");

        // Ready 
        this.toaster.Host = this.View.ToasterHost;
        this.toaster.Show(
            this.Localize("Shell.Ready"), this.Localize("Shell.Greetings"),
            5_000, InformationLevel.Info);

        this.isFirstActivation = true;

        // ! viewSelector is Always there
        this.viewSelector!.SelectView(this.photoPostProModel.IsFirstRun ? ActivatedView.Language : ActivatedView.Library);
        HotKeys.Instance.Set(this.View); 

        this.Logger.Debug("OnViewLoaded complete");
    }

    private void SetupWorkflow()
    {
        if (this.View is not ShellView view)
        {
            throw new Exception("No view: Failed to startup...");
        }

        var selectableViews = new List<SelectableView<ActivatedView>>();

        void Setup<TViewModel, TControl, TToolbarViewModel, TToolbarControl>(
                ActivatedView activatedView, Control? control)
            where TViewModel : ViewModel<TControl>
            where TControl : Control, IView, new()
            where TToolbarViewModel : ViewModel<TToolbarControl>
            where TToolbarControl : Control, IView, new()
        {
            var vm = App.GetRequiredService<TViewModel>();
            vm.CreateViewAndBind();
            var vmToolbar = App.GetRequiredService<TToolbarViewModel>();
            vmToolbar.CreateViewAndBind();
            selectableViews.Add(
                new SelectableView<ActivatedView>(activatedView, vm, control, vmToolbar));
        }

        void SetupNoToolbar<TViewModel, TControl>(
                ActivatedView activatedView, Control control)
            where TViewModel : ViewModel<TControl>
            where TControl : Control, IView, new()
        {
            var vm = App.GetRequiredService<TViewModel>();
            vm.CreateViewAndBind();
            selectableViews.Add(new SelectableView<ActivatedView>(activatedView, vm, control));
        }

        void SetupToolboxNoToolbar<TViewModel, TControl, TToolboxViewModel, TToolboxControl>(
                ActivatedView activatedView, Control? control)
            where TViewModel : ViewModel<TControl>
            where TControl : Control, IView, new()
            where TToolboxViewModel : ViewModel<TToolboxControl>
            where TToolboxControl : Control, IView, new()
        {
            var vm = App.GetRequiredService<TViewModel>();
            vm.CreateViewAndBind();
            var vmToolbox = App.GetRequiredService<TToolboxViewModel>();
            vmToolbox.CreateViewAndBind();
            var v = vmToolbox.View; 
            var selectable = new SelectableView<ActivatedView>(activatedView, vm, control, null, vmToolbox);
            selectableViews.Add(selectable);
        }

        SetupNoToolbar<CameraViewModel, CameraView>(ActivatedView.Camera, view.CameraButton);

        SetupToolboxNoToolbar<SingleViewModel, SingleView, SingleToolboxViewModel, SingleToolboxView>(
            ActivatedView.Single, view.SingleButton);

        SetupNoToolbar<LibraryViewModel, LibraryView>(ActivatedView.Library, view.LibraryButton);

        Setup<LanguageViewModel, LanguageView, LanguageToolbarViewModel, LanguageToolbarView>(
            ActivatedView.Language, view.FlagButton);

        // No button for process view, as it is only used as a secondary view (from single or folder)
        Setup<ProcessViewModel, ProcessView, ProcessToolbarViewModel, ProcessToolbarView>(
            ActivatedView.Process, null);

        // Needs to be kept alive as a class member, or else callbacks will die (and wont work) 
        this.viewSelector =
            new ViewSelector<ActivatedView>(
                this.View.ShellViewContent,
                this.View.ShellViewToolbar,
                this.View.SelectionGroup,
                selectableViews,
                this.OnViewSelected,
                this.View.ShellViewToolbox);

        // Process view is only activated from single or folder view, so disable it by default
        ViewSelector<ActivatedView>.Disable(ActivatedView.Process);
    }

    private void OnViewSelected(ActivatedView activatedView)
    {
        if (this.viewSelector is null)
        {
            throw new Exception("No view selector");
        }

        var newViewModel = this.viewSelector.CurrentPrimaryViewModel;
        if (newViewModel is not null)
        {
            bool mainToolbarIsHidden = false;
            this.MainToolbarIsVisible = !mainToolbarIsHidden;

            // SLOW ! 
            // Figure out why this line below takes about 4 seconds !
            //
            //this.Profiler.MemorySnapshot(
            //    newViewModel.ViewBase!.GetType().Name + ":  Activated", withGCCollect: false);
        }

        this.isFirstActivation = false;
    }

    [RelayCommand]
    public void OnCamera() => this.viewSelector?.SelectView(ActivatedView.Camera);

    [RelayCommand]
    public void OnSingle() =>this.viewSelector?.SelectView(ActivatedView.Single);

    [RelayCommand]
    public void OnFolder() => this.viewSelector?.SelectView(ActivatedView.Library);

    [RelayCommand]
    public void OnLanguage() => this.viewSelector?.SelectView(ActivatedView.Language);

#pragma warning disable IDE0079 
#pragma warning disable CA1822 // Mark members as static

    [RelayCommand]
    public void OnClose() => OnExit();

    private static async void OnExit()
    {
        var application = App.GetRequiredService<IApplicationBase>();
        await application.Shutdown();
    }

#pragma warning restore CA1822
#pragma warning restore IDE0079

    internal void EnableAndSelect(ActivatedView view)
    {
        // ! viewSelector is Always there
        this.viewSelector!.EnableView(view);
        // ! viewSelector is Always there
        this.viewSelector!.SelectView(view); 
    }
}