namespace Lyt.PhotoPostPro;

public partial class App : ApplicationBase
{
    public const string Organization = "Lyt";
    public const string Application = "PhotoPostPro";
    public const string RootNamespace = "Lyt.PhotoPostPro";
    public const string AssemblyName = "Lyt.PhotoPostPro";
    public const string AssetsFolder = "Assets";

    public App(
        IMtpService mtpService,
        IWallpaperService wallpaperService) :
        base(
            App.Organization,
            App.Application,
            App.RootNamespace,
            InitializeHosting,
            GetModelTypes,
            singleInstanceRequested: false,
            splashImageUri: null,
            appSplashWindow: new SplashWindow()
            )
    {
        // This should be mostly empty, use the OnStartup override
        Instance = this;

        // see below comment about service instances 
        App.MtpService = mtpService;
        App.WallpaperService = wallpaperService;

        Debug.WriteLine("App Instance created");
    }

#pragma warning disable CS8618 
    // Non-nullable field must contain a non-null value when exiting constructor. 
    public static App Instance { get; private set; }

    // For these services we dont know the type of the instance, therefore we cannot
    // register them easily, and we just keep them available as global statics 
    public static IMtpService MtpService { get; set; }
    public static IWallpaperService WallpaperService { get; set; }

#pragma warning restore CS8618 

    public bool RestartRequired { get; set; }

    public static List<Type> GetModelTypes () 
        => [typeof(FileManagerModel), typeof(PhotoPostProModel)];
    
    public static IHost InitializeHosting()
    {
        IServiceCollection? registeredServices = null;  
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((_0, services) =>
            {
                // Register the app
                _ = services.AddSingleton<IApplicationBase>(App.Instance);

                // Always Main Window 
                _ = services.AddSingleton<Window, MainWindow>();

                // The Application Model, also  a singleton, no need here to also add it without the inferface  
                _ = services.AddSingleton<IApplicationModel, ApplicationModelBase>(); // Top level model

                // Models 
                _ = services.AddSingleton<FileManagerModel>();
                _ = services.AddSingleton<PhotoPostProModel>();

                // Singletons, they do not need an interface. 
                //
                // Shell 
                _ = services.AddSingleton<ShellViewModel>();

                // Views and ViewModels from the main view selector            
                _ = services.AddSingleton<ImportViewModel>();
                _ = services.AddSingleton<CameraViewModel>();
                _ = services.AddSingleton<LibraryViewModel>();
                _ = services.AddSingleton<GalleryViewModel>();
                _ = services.AddSingleton<GalleryToolbarViewModel>();
                _ = services.AddSingleton<SettingsViewModel>();
                _ = services.AddSingleton<ToolsViewModel>();
                _ = services.AddSingleton<LanguageViewModel>();
                _ = services.AddSingleton<LanguageToolbarViewModel>();


                // Culling ViewModel and its Toolbar ViewModel
                // 
                _ = services.AddSingleton<CullingViewModel>();
                _ = services.AddSingleton<CullingToolbarViewModel>();

                // Process ViewModels and Toolbox ViewModels, in Workflow order for convenience.
                // 
                _ = services.AddSingleton<ProcessViewModel>();
                _ = services.AddSingleton<ProcessToolbarViewModel>();
                _ = services.AddSingleton<ToolboxHostViewModel>();

                _ = services.AddSingleton<OrientViewModel>();
                _ = services.AddSingleton<OrientToolboxViewModel>();

                _ = services.AddSingleton<StraightenViewModel>();
                _ = services.AddSingleton<StraightenToolboxViewModel>();

                _ = services.AddSingleton<ComposeViewModel>();
                _ = services.AddSingleton<ComposeToolboxViewModel>();

                _ = services.AddSingleton<ExposureViewModel>();
                _ = services.AddSingleton<ExposureToolboxViewModel>();

                _ = services.AddSingleton<RecoveryViewModel>();
                _ = services.AddSingleton<RecoveryToolboxViewModel>();

                _ = services.AddSingleton<VignetteViewModel>();
                _ = services.AddSingleton<VignetteToolboxViewModel>();

                _ = services.AddSingleton<WhiteBalanceViewModel>();
                _ = services.AddSingleton<WhiteBalanceToolboxViewModel>();

                _ = services.AddSingleton<ContrastViewModel>();
                _ = services.AddSingleton<ContrastToolboxViewModel>();

                _ = services.AddSingleton<LutViewModel>();
                _ = services.AddSingleton<LutToolboxViewModel>();
                _ = services.AddSingleton<LutExplorerViewModel>();

                _ = services.AddSingleton<ColorViewModel>();
                _ = services.AddSingleton<ColorToolboxViewModel>();

                _ = services.AddSingleton<SharpenViewModel>();
                _ = services.AddSingleton<SharpenToolboxViewModel>();

                _ = services.AddSingleton<FiltersViewModel>();
                _ = services.AddSingleton<FiltersToolboxViewModel>();

                _ = services.AddSingleton<ExportViewModel>();
                _ = services.AddSingleton<ExportToolboxViewModel>();

                // Services, all must comply to a specific interface 
                // _ = services.AddSingleton<ILogger, LogViewerWindow>();
                _ = services.AddSingleton<ILogger, BasicLogger>();
                _ = services.AddSingleton<IFocuser, Focuser>();
                _ = services.AddSingleton<IAnimationService, AnimationService>();
                _ = services.AddSingleton<ILocalizer, LocalizerModel>();
                _ = services.AddSingleton<IDialogService, DialogService>();
                _ = services.AddSingleton<IDispatch, Dispatch>();
                _ = services.AddSingleton<IProfiler, Profiler>();
                _ = services.AddSingleton<IToaster, Toaster>();
                _ = services.AddSingleton<IRandomizer, Randomizer>();

                registeredServices = services; 
            }).Build();

        return host;
    }

    protected override async Task OnStartupBegin()
    {
        ViewModel.TypeInitialize(ApplicationBase.AppHost);

        var logger = App.GetRequiredService<ILogger>();
        logger.Debug("OnStartupBegin begins");

        // This needs to complete before all models are initialized.
        var fileManager = App.GetRequiredService<FileManagerModel>();
        await fileManager.Configure(
            new FileManagerConfiguration(
                App.Organization, App.Application, App.RootNamespace, App.AssemblyName, App.AssetsFolder,
                userFolders: []));

        // The localizer needs the File Manager, do not change the order.
        var localizer = App.GetRequiredService<ILocalizer>();
        await localizer.Configure(
            new LocalizerConfiguration
            {
                Assembly = Assembly.GetExecutingAssembly(), 
                AssemblyName = App.AssemblyName,
                Languages =
                [
                    // Not supported by Libre: "hy-AM", Replaced by Vietnamese

                    // Master, See PppLanguages.json in Tools folder 
                    "en-US", 

                    // Human Translated, with some machine help
                    "fr-FR", "it-IT",

                    // Partially Human Translated, with a lot of machine help
                    "es-ES", 

                    // Fully Auto Translated
                    "uk-UA", "bg-BG", "el-GR", "de-DE",
                    "jp-JP", "ko-KO", "zh-CN", "zh-TW",
                    "hi-IN", "bn-BD", "hu-HU", "vi-VI",

                    // New 
                    "pt-PT", "th-TH"
                ],
                // Use default for all other config parameters of the Localizer 
            });

        logger.Debug("OnStartupBegin complete");
    }

    protected override Task OnShutdownComplete()
    {
        var logger = App.GetRequiredService<ILogger>();
        logger.Debug("On Shutdown Complete");

        if (this.RestartRequired)
        {
            logger.Debug("On Shutdown Complete: Restart Required");
            var process = Process.GetCurrentProcess();
            if ((process is not null) && (process.MainModule is not null))
            {
                Process.Start(process.MainModule.FileName);
            }
        }

        return Task.CompletedTask;
    }

    // Why does it need to be there ??? 
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
}
