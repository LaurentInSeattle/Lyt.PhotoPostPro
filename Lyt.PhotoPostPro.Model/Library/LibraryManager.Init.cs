namespace Lyt.PhotoPostPro.Model.Library;

using Lyt.Framework.Interfaces.Dispatching;

// To avoid namespace conflicts for 'Path' 
using System.IO;

public sealed partial class LibraryManager
{
    public const string LibraryFolderName = "Library";
    public const string GalleryFolderName = "Gallery";
    public const string ExportsFolderName = "Exports";
    public const string CameraDownloadsFolderName = "CameraDownloads";

    public const int CachedHdImageCount = 120;
    public const int CachedGalleryImageCount = 4;

    private readonly Lock lockObject;

    private readonly PhotoPostProModel model;
    private readonly FileManagerModel fileManager;
    private readonly IDispatch dispatcher;
    private readonly ILogger logger;

    private readonly string galleryFolderPath;
    private readonly Lock lockObjectFiles = new();

    private string libraryFolderPath = string.Empty;
    private string exportsFolderPath = string.Empty;

    private int imageLoadedCount;
    private int errorLoadingCount;

    public LibraryManager(
        PhotoPostProModel model, FileManagerModel fileManager, IDispatch dispatcher, ILogger logger)
    {
        this.model = model;
        this.fileManager = fileManager;
        this.dispatcher = dispatcher;
        this.logger = logger;

        this.lockObject = new Lock();

        // Setup the library folder path based on the selection in the model's LibraryFolders collection.
        this.SetupLibraryFolderPath();

        // Setup the gallery folder path based on the Pictures folder and the application name.
        string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        this.galleryFolderPath = Path.Combine(pictures, PhotoPostProModel.PhotoPostProAppName, GalleryFolderName);
        if (!Directory.Exists(this.galleryFolderPath))
        {
            Directory.CreateDirectory(this.galleryFolderPath);
        }

        // Setup the export folder path based on the model's ExportPath.
        // If it's set to default , use the default library path.
        // Otherwise, attempt to create the specified directory.
        void SetDefaultExportPath()
        {
            this.exportsFolderPath = Path.Combine(pictures, PhotoPostProModel.PhotoPostProAppName, ExportsFolderName);
            if (!Directory.Exists(this.exportsFolderPath))
            {
                Directory.CreateDirectory(this.exportsFolderPath);
            }
        }

        if (this.model.ExportPath.Equals(PhotoPostProModel.ExportPathDefault, StringComparison.InvariantCultureIgnoreCase))
        {
            SetDefaultExportPath();
        }
        else
        {
            try
            {
                this.exportsFolderPath = this.model.ExportPath;
                if (!Directory.Exists(this.exportsFolderPath))
                {
                    Directory.CreateDirectory(this.exportsFolderPath);
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(
                    string.Format("Error creating exports folder path: {0}\n {1}", this.model.ExportPath, ex.ToString()));
                SetDefaultExportPath();
            }
        }

        // Adjust capacity as needed
        this.GalleryImages = new(CachedGalleryImageCount);
        this.LoadedHdImages = new(CachedHdImageCount);
        this.LoadedThumbnails = [];
        this.KeywordsIndex = [with(StringComparer.OrdinalIgnoreCase)]; ;
    }

    public string LibraryFolderPath => this.libraryFolderPath;

    public string ExportsFolderPath => this.exportsFolderPath;

    public string GalleryFolderPath => this.galleryFolderPath;

    // This dictionary is indexed by the path of the source image 
    public LruDictionary<string, byte[]> GalleryImages { get; private set; }

    // This dictionary is indexed by the path of the source image 
    public LruDictionary<string, LoadedImage> LoadedHdImages { get; private set; }

    // This dictionary is indexed by the path of the metadata file for the image 
    public Dictionary<string, LoadedThumbnail> LoadedThumbnails { get; private set; }

    // This dictionary is indexed by a keyword and values are a hash set for the paths of the metadata
    // files for the images 
    public Dictionary<string, HashSet<string>> KeywordsIndex { get; private set; }

    public FolderTree? CapturedFolderTree { get; private set; }

    public FolderTree? UnratedFolderTree { get; private set; }

    public FolderTree? EditedFolderTree { get; private set; }

    public bool IsLoading { get; private set; }

    public void Initialize() => this.LoadLibrary(isInitialLoad: true);

    public void ClearLibrary()
    {
        // Clear all data structures and reset the state before loading the new library.
        this.LoadedThumbnails.Clear();
        this.KeywordsIndex.Clear();
        this.LoadedHdImages.Clear();
        this.CapturedFolderTree = null;
        this.UnratedFolderTree = null;
        this.EditedFolderTree = null;
        new LibraryClearedMessage().Publish();

        // May delay a bit to allow the UI to update and show loading status
        GC.Collect();
    }

    public void ReloadLibrary(string newLibraryFolderName)
    {
        if (string.IsNullOrWhiteSpace(newLibraryFolderName))
        {
            throw new ArgumentException("Library folder name cannot be null or whitespace.", nameof(newLibraryFolderName));
        }

        var folders = this.model.LibraryFolders;
        if (!folders.IsValid(newLibraryFolderName))
        {
            throw new ArgumentException($"Library folder name '{newLibraryFolderName}' is not valid.", nameof(newLibraryFolderName));
        }

        // Change library folder and saves for subsequent runs
        folders.SelectedLibraryFolderName = newLibraryFolderName;
        this.SetupLibraryFolderPath();
        this.model.Save(); 

        // And finally reload here 
        this.IsLoading = true;
        this.LoadLibrary(isInitialLoad: false);
    }

    private void SetupLibraryFolderPath()
    {
        // Setup the library folder path based on the selection in the model's LibraryFolders collection.
        var libraryFolders = this.model.LibraryFolders;
        if (!libraryFolders.IsValid(libraryFolders.SelectedLibraryFolderName))
        {
            // Invalid : fallback to default 
            libraryFolders.SelectedLibraryFolderName = LibraryFolder.DefaultLibraryFolderName;
        }

        this.libraryFolderPath = libraryFolders.SelectedLibraryFolderPath;
    }

    private void LoadLibrary(bool isInitialLoad = true)
    {
        this.IsLoading = true;

        Task.Run(() =>
        {
#if DEBUG 
            const int fileCountTrigger = 1000;
            const int initialDelay = 700;
#else
            const int fileCountTrigger = 1500; 
            const int initialDelay  = 500;
#endif
            if (isInitialLoad)
            {
                // wait a bit so that we dont delay the app starting up 
                Task.Delay(initialDelay).Wait();
            }

            this.GenerateFolderTree();
            if (this.CapturedFolderTree is not null)
            {
                int fileCount = this.CapturedFolderTree.FileCount();
                if (fileCount > fileCountTrigger)
                {
                    new LibraryLoadingMessage(fileCount).Publish();
                    Task.Delay(300).Wait();
                }
            }

#if DEBUG 
            // Simulate slow loading - if needed for debug ONLY 
            // Task.Delay(3000).Wait();
#endif

            this.GenerateThumbnailCache();

            if (isInitialLoad)
            {
                Task.Delay(200).Wait();
                this.InitializeGallery();
            }
        });
    }

}