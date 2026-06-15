namespace Lyt.PhotoPostPro.Workflow.Process;

public partial class ToolboxViewModel<T> : ViewModel<T> where T : View, new()
{
    protected readonly PhotoPostProModel model; 

    public ToolboxViewModel() => this.model = App.GetRequiredService<PhotoPostProModel>();
    
    // ==> Important! 
    // Commands in aXAML MUST have ReflectionBinding - and not just Binding
    // Or else : Invalid cast exception will be thrown   :( 

    [RelayCommand]
    public void OnBack() => this.model.Workflow.GoBack();

    [RelayCommand]
    public void OnNext()
    {
        this.OnBeforeNext(); 
        this.model.Workflow.SaveAndNext();
    } 

    protected virtual void OnBeforeNext () {  } 
}
