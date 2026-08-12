namespace Lyt.PhotoPostPro.Workflow.Process.Lut;

public sealed partial class LutExplorerViewModel : 
    ViewModel<LutExplorerView> , 
    IRecipient<ExploreLutImageGeneratedMessage>
{
    public LutExplorerViewModel()
    {
        this.Subscribe<ExploreLutImageGeneratedMessage>();        
    }


    internal void Hide() {  }

    internal void Launch(LutViewModel lutViewModel, LutStep lutStep)
    {
        Debug.WriteLine(" Launching Explore Luts ");
        lutStep.LaunchExploreLuts(); 
    }

    public void Receive(ExploreLutImageGeneratedMessage message)
        => Dispatch.OnUiThread(
            () => { this.ReceiveOnUiThread(message); }, 
            DispatcherPriority.Background); 

    private void ReceiveOnUiThread(ExploreLutImageGeneratedMessage message)
    {
        var bitmap = message.Frame.ToWriteableBitmap();
        var metadata = message.LutMetadata; 
        Debug.WriteLine(" Image received "); 
        if (metadata.IsEmpty )
        {
            Debug.WriteLine(" Original ");
        }
        else
        {
            Debug.WriteLine( " " + metadata.FriendlyName);
        }
    }
}
