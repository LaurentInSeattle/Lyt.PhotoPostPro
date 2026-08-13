namespace Lyt.PhotoPostPro.Model.LibraryModels;

using Lyt.Framework.Interfaces.Dispatching;

// To avoid namespace conflicts for 'Path' 
using System.IO;

public sealed partial class LibraryManager
{
    public const string LibraryFolderName = "Library";
    public const string GalleryFolderName = "Gallery";
    public const string ExportsFolderName = "Exports";

    public const int CachedHdImageCount = 120;
    public const int CachedGalleryImageCount = 8;
    
    private readonly Lock lockObject;

    private readonly string libraryFolderPath;
    private readonly string galleryFolderPath;
    private readonly string exportsFolderPath;

    private PhotoPostProModel? model;
    private FileManagerModel? fileManager;
    private IDispatch? dispatcher;

    private int imageLoadedCount = 0;
    private int errorLoadingCount = 0;

    public LibraryManager()
    {
        this.lockObject = new Lock();
        string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        this.libraryFolderPath = Path.Combine(pictures, PhotoPostProModel.PhotoPostProAppName, LibraryFolderName);
        if (!Directory.Exists(this.libraryFolderPath))
        {
            Directory.CreateDirectory(this.libraryFolderPath);
        }

        this.galleryFolderPath = Path.Combine(pictures, PhotoPostProModel.PhotoPostProAppName, GalleryFolderName);
        if (!Directory.Exists(this.galleryFolderPath))
        {
            Directory.CreateDirectory(this.galleryFolderPath);
        }

        this.exportsFolderPath = Path.Combine(pictures, PhotoPostProModel.PhotoPostProAppName, ExportsFolderName);
        if (!Directory.Exists(this.exportsFolderPath))
        {
            Directory.CreateDirectory(this.exportsFolderPath);
        }

        // Adjust capacity as needed
        this.GalleryImages = new(CachedGalleryImageCount);
        this.LoadedHdImages = new(CachedHdImageCount);
        this.LoadedThumbnails = [];
    }

    public string LibraryFolderPath => this.libraryFolderPath;

    public string ExportsFolderPath => this.exportsFolderPath;

    // This dictionary is indexed by the path of the source image 
    public LruDictionary<string, byte[]> GalleryImages { get; private set; }

    // This dictionary is indexed by the path of the source image 
    public LruDictionary<string, LoadedImage> LoadedHdImages { get; private set; }

    // This dictionary is indexed by the path of the metadata file for the image 
    public Dictionary<string, LoadedThumbnail> LoadedThumbnails { get; private set; }

    public FolderTree? CapturedFolderTree { get; private set; }

    public FolderTree? AddedFolderTree { get; private set; }

    public FolderTree? EditedFolderTree { get; private set; }

    public bool IsLoading { get; private set; }

    public void Initialize(PhotoPostProModel model, FileManagerModel fileManagerModel, IDispatch dispatcher)
    {
        this.model = model;
        this.fileManager = fileManagerModel;
        this.dispatcher = dispatcher;

        this.IsLoading = true;

        Task.Run(() =>
        {
            // wait a bit so that we dont delay the app starting up 
            Task.Delay(666).Wait();
            this.GenerateFolderTree();
            Task.Delay(200).Wait();
            this.GenerateThumbnailCache();
            Task.Delay(200).Wait();
            this.InitializeGallery(); 
        });
    }
}