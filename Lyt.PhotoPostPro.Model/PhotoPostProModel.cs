namespace Lyt.PhotoPostPro.Model;

using Lyt.Framework.Interfaces.Dispatching;

using System.Globalization;

using static Lyt.Persistence.FileManagerModel;

public sealed partial class PhotoPostProModel : ModelBase
{
    public const string DefaultLanguage = "en-US";
    public const string PhotoPostProAppName = "PhotoPostPro";
    public const string PhotoPostProFilename = "PhotoPostProData";

    private static readonly PhotoPostProModel DefaultData =
        new()
        {
            Language = DefaultLanguage,
            FileUid = 0, 
            IsFirstRun = true,
            Signatures = new Signatures(),
            Watermarks = new Watermarks(),
        };

    private readonly Lock lockObject = new();

    private readonly FileManagerModel fileManager;
    private readonly ILocalizer localizer;
    private readonly IProfiler profiler;
    private readonly IDispatch dispatcher; 
    private readonly FileId modelFileId;
    private readonly TimeoutTimer timeoutTimer;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
    public PhotoPostProModel() : base(null)
    {
        this.modelFileId = new FileId(Area.User, Kind.Json, PhotoPostProModel.PhotoPostProFilename);
        // Do not inject the FileManagerModel instance: a parameter-less ctor is required for Deserialization 
        // Empty CTOR required for deserialization 
        this.ShouldAutoSave = false;
    }
#pragma warning restore CS8625 
#pragma warning restore CS8618

    public PhotoPostProModel(
        FileManagerModel fileManager,
        ILocalizer localizer,
        IProfiler profiler,
        IDispatch dispatcher,
        ILogger logger) : base(logger)
    {
        this.fileManager = fileManager;
        this.localizer = localizer;
        this.profiler = profiler;
        this.dispatcher = dispatcher;

        this.CameraManager = new CameraManager(logger);
        this.LibraryManager.Initialize(this, fileManager, dispatcher); 
        this.modelFileId = new FileId(Area.User, Kind.Json, PhotoPostProModel.PhotoPostProFilename);
        this.timeoutTimer = new TimeoutTimer(this.OnModelUpdate, timeoutMilliseconds: 250);
        this.ShouldAutoSave = true;
    }

    public IProfiler Profiler => this.profiler;

    public override async Task Initialize()
    {
        this.IsInitializing = true;
        await this.Load();
        this.IsInitializing = false;
        this.IsDirty = false;

        // Get the version of LibRaw
        string libRawVersion = string.Concat(" LibRaw Version: ", ImageLoader.LibRawVersion);
        Debug.WriteLine(libRawVersion);
        this.Logger.Info(libRawVersion);

        // Get the assembly version of ImageSharp
        // ! Has a version
        string imageSharpVersion =
            string.Concat(
                " ImageSharp Version: ",
                typeof(Image).GetTypeInfo().Assembly.GetName().Version!.ToString());
        Debug.WriteLine(imageSharpVersion);
        this.Logger.Info(imageSharpVersion);
    } 

    // Force a save on shutdown 
    public override async Task Shutdown() => await this.Save();

    public Task Load()
    {
        try
        {
            var jsonTypeInfo = AppJsonContext.Default.PhotoPostProModel; 
            if (!this.fileManager.Exists(this.modelFileId))
            {
                this.fileManager.Save(this.modelFileId, PhotoPostProModel.DefaultData, jsonTypeInfo);
            }

            PhotoPostProModel model = this.fileManager.Load<PhotoPostProModel>(this.modelFileId, jsonTypeInfo);

            // Copy all properties with attribute [JsonRequired]
            base.CopyJSonRequiredProperties<PhotoPostProModel>(model);
            this.SelectLanguage(this.Language);
            new ModelLoadedMessage().Publish();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            string msg = "Failed to load Model from " + this.modelFileId.Filename;
            this.Logger.Fatal(msg);
            throw new Exception("", ex);
        }
    }

    public override Task Save()
    {
        // Null check is needed !
        // If the File Manager is null we are currently loading the model and activating properties on a second instance 
        // causing dirtyness, and in such case we must avoid the null crash and anyway there is no need to save anything.
        if (this.fileManager is not null)
        {
#if DEBUG 
            //if (this.fileManager.Exists(this.modelFileId))
            //{
            //    this.fileManager.Duplicate(this.modelFileId);
            //}
#endif // DEBUG 

            var jsonTypeInfo = AppJsonContext.Default.PhotoPostProModel;
            this.fileManager.Save(this.modelFileId, this, jsonTypeInfo);

#if DEBUG 
            //try
            //{
            //    string path = this.fileManager.MakePath(this.modelFileId);
            //    var fileInfo = new FileInfo(path);
            //    if (fileInfo.Length < 1024)
            //    {
            //        // if (Debugger.IsAttached) { Debugger.Break(); }
            //        this.Logger.Warning("Model file is too small!");
            //    }
            //}
            //catch (Exception ex)
            //{
            //    if (Debugger.IsAttached) { Debugger.Break(); }
            //    Debug.WriteLine(ex);
            //}
#endif // DEBUG 

            base.Save();
        }

        return Task.CompletedTask;
    }

    public void SetupLanguage()
    {
        // Select default language 
        string preferredLanguage = this.Language;
        this.Logger.Debug("Language: " + preferredLanguage);
        this.Language = preferredLanguage;
        Thread.CurrentThread.CurrentCulture = new CultureInfo(preferredLanguage);
        Thread.CurrentThread.CurrentUICulture = new CultureInfo(preferredLanguage);

        this.Logger.Debug("OnViewLoaded language loaded");
    }

    public void SelectLanguage(string languageKey)
    {
        this.Language = languageKey;
        this.localizer.SelectLanguage(languageKey);
    }

    public void ClearFirstRun()
    {
        this.IsFirstRun = false;
        this.Save();
    }

    private void OnModelUpdate()
    {
        if (!this.IsUpdatePending)
        {
            return;
        }

        if ( this.IsSourceImageUpdatePending && this.LastSourceFrame is not null)
        {
            this.IsSourceImageUpdatePending = false; 
            new SourceImageGeneratedMessage(this.LastSourceFrame).Publish();
        }

        if (this.IsResultImageUpdatePending && this.LastResultFrame is not null)
        {
            this.IsResultImageUpdatePending = false;
            new ResultImageGeneratedMessage(this.LastResultFrame).Publish();
        }
    }
}
