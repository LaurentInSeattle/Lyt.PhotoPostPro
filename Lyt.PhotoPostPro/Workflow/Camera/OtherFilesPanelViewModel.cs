namespace Lyt.PhotoPostPro.Workflow.Camera;

// using static Lyt.Persistence.FileManagerModel;

public sealed partial class OtherFilesPanelViewModel :
    ViewModel<OtherFilesPanelView>,
    IRecipient<LanguageChangedMessage>
{
    private readonly PhotoPostProModel photoPostProModel;
    private readonly CameraViewModel cameraViewModel;

    [ObservableProperty]
    public partial ObservableCollection<CameraFileViewModel> Files { get; set; }

    public OtherFilesPanelViewModel(PhotoPostProModel photoPostProModel, CameraViewModel cameraViewModel)
    {
        this.photoPostProModel = photoPostProModel;
        this.cameraViewModel = cameraViewModel;
        this.Files = [];
        this.Subscribe<LanguageChangedMessage>();
    }

    public void Receive(LanguageChangedMessage _) { } 
}
