namespace Lyt.PhotoPostPro.Workflow.Camera;

// using static Lyt.Persistence.FileManagerModel;

public sealed partial class OtherFilesPanelViewModel :
    ViewModel<OtherFilesPanelView>,
    IRecipient<LanguageChangedMessage>
{
    private readonly PhotoPostProModel photoPostProModel;
    private readonly CameraViewModel cameraViewModel;

    //[ObservableProperty]
    //public partial ObservableCollection<CameraThumbnailViewModel> Thumbnails { get; set; }

    public OtherFilesPanelViewModel(PhotoPostProModel photoPostProModel, CameraViewModel collectionViewModel)
    {
        this.photoPostProModel = photoPostProModel;
        this.cameraViewModel = collectionViewModel;
        this.Subscribe<LanguageChangedMessage>();
    }

    public void Receive(LanguageChangedMessage _) { } 
}
