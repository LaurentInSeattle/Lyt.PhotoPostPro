namespace Lyt.PhotoPostPro.Model;

public sealed partial class PhotoPostProModel : ModelBase
{
    private const uint ColorBlack = 0xFF_000000;
    private const uint ColorWhite = 0xFF_FFFFFF;

    #region Serialized -  No model changed event

    [JsonRequired]
    public string Language { get => this.Get<string>()!; set => this.Set(value); }

    /// <summary> This should stay true, ==> But... Just FOR NOW !  </summary>
    [JsonRequired]
    public bool IsFirstRun { get; set; } = false;

    [JsonRequired]
    public Dictionary<string, ProjectMetadata> Projects { get; set; } = new(8);

    #endregion Serialized -  No model changed event

    #region Not serialized - No model changed event

    [JsonIgnore]
    public bool IsUpdatePending { get; set; } = false;

    [JsonIgnore]
    public ProjectMetadata? CurrentProjectMetadata { get; set; } = null;

    [JsonIgnore]
    public Project? CurrentProject { get; set; } = null;

    [JsonIgnore]
    public PostProcess? CurrentPostProcess { get; set; } = null;

    [JsonIgnore]
    public PostProcessWorkflow? CurrentWorkflow  => this.CurrentPostProcess?.Workflow;

    [JsonIgnore]
    public Frame? LastFrame { get; set; } = null;

    [JsonIgnore]
    public PostProcessWorkflow Workflow
    {
        // For when we are 100 % sure that nothing should be null
        // Danger Zone... 
        get             
        {
            if (this.CurrentPostProcess is null)
            {
                if ( Debugger.IsAttached ) {  Debugger.Break(); }
                throw new InvalidOperationException(); 
            }

            if (this.CurrentPostProcess.Workflow is null)
            {
                if (Debugger.IsAttached) { Debugger.Break(); }
                throw new InvalidOperationException();
            }

            return this.CurrentPostProcess.Workflow;
        }
    }

    #endregion Not serialized - No model changed event

    #region NOT serialized - WITH model changed event

    // None for now 

    #endregion NOT serialized - WITH model changed event    
}
