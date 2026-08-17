namespace Lyt.PhotoPostPro.Workflow.Process.Lut;

using System.IO;

public sealed partial class LutToolboxViewModel :
    ToolboxViewModel<LutToolboxView, LutStep>, IDropPathHandler
{
    private bool doNotUpdateModel;
    private LutMetadata lutMetadata = LutMetadata.Empty;

    public LutToolboxViewModel()
    {
        this.DropViewModel = new DropViewModel(this, "Workflow.Lut.DragDropHelp")
        {
            IsVisible = true
        };
    }

    protected override string Title => this.Localize("Workflow.Lut.Title");

    [ObservableProperty]
    public partial bool IsExplorerMode { get; set; }

    [ObservableProperty]
    public partial DropViewModel DropViewModel { get; set; }

    [ObservableProperty]
    public partial List<string> AvailableLutNames { get; set; } = [];

    [ObservableProperty]
    public partial int SelectedIndex { get; set; }

    public List<LutMetadata> AvailableLuts { get; set; } = [];

    public void OnDropPath(string path, bool isDirectory)
    {
        if (isDirectory)
        {
            return;
        }

        bool isValid = LutsManager.Validate(path, out string message);
        if (!isValid)
        {
            // TODO
            // Message user and abort 
            Debug.WriteLine(" LUT OnDropPath Failed: " + message); 
            return;
        }

        bool loaded = this.model.LutsManager.AddLut(path, out message, out LutMetadata? lutMetadata);
        if (!loaded || lutMetadata is null)
        {
            // TODO
            // Message user and abort 
            Debug.WriteLine(" LUT OnDropPath Failed: " + message);
            return;
        }

        // Lut data has been cached and will be ready 
        this.AvailableLutNames.Add(lutMetadata.FriendlyName);
        this.AvailableLuts.Add(lutMetadata);

        // Will trigger a SelectedIndexChanged event and simulate choice from the combo box 
        this.SelectedIndex = this.AvailableLuts.Count - 1;
    }

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();

        if (!this.isFirstLoad)
        {
            return;
        }

        With.Flag(ref this.doNotUpdateModel, () =>
        {
            // Sliders initial positions and string values
            var metaLuts = this.model.LutsManager.EnumerateLuts();
            List<string> list = new(1 + metaLuts.Count)
            {
               this.Localize("Workflow.Lut.None"),
            };

            this.AvailableLuts.Add(LutMetadata.Empty);

            foreach (var metaLut in metaLuts)
            {
                this.AvailableLuts.Add(metaLut);
                list.Add(metaLut.FriendlyName);
            }

            this.AvailableLutNames = list;

            // Enforce property changed 
            this.SelectedIndex = 1;
            this.SelectedIndex = 0;
        });

        this.isFirstLoad = false;
    }

    [RelayCommand]
    public void OnLaunchExplorer()
    {
        var vm = App.GetRequiredService<LutViewModel>();
        vm.LaunchExplorer();
        this.IsExplorerMode = true;
    }

    public override void OnBeforeBack()
    {
        this.HideExplorer();
        base.OnBeforeBack();
    }

    public override void OnBeforeReset()
    {
        this.HideExplorer();
        base.OnBeforeReset();
    }

    public override void OnBeforeNext()
    {
        this.HideExplorer();
        base.OnBeforeNext();
    }

    private void HideExplorer()
    {
        this.IsExplorerMode = false;
        var vm = App.GetRequiredService<LutViewModel>();
        vm.HideExplorer();
    }

    public override void OnModelStepUpdated(LutStep step) => this.UpdateUI(step);

    private void UpdateUI(LutStep step)
    {
        With.Flag(ref this.doNotUpdateModel, () =>
        {
            var lutMetadata = step.LutMetadata;
            if (!lutMetadata.IsEmpty)
            {
                if (lutMetadata.IsEmbedded)
                {
                    int index = 0;
                    string lutName = lutMetadata.FriendlyName;
                    foreach (string name in this.AvailableLutNames)
                    {
                        if (name.Equals(lutName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            this.SelectedIndex = index;
                            break;
                        }

                        ++index;
                    }
                }
            }
        });
    }

    partial void OnSelectedIndexChanged(int value)
    {
        if (value >= 0 && value < this.AvailableLutNames.Count)
        {
            // Wait one frame before launching the image color lookup process
            // If we dont, the app randomly freezes, with no exceptions thrown.
            // Possible Avalonia Bug ? Need to test with latest 12.0.5
            // Debug Output shows:
            // [Control] PlatformImpl is null, couldn't handle input. (PresentationSource #<some number>>)
            Schedule.OnUiThread(150, () =>
                {
                    this.lutMetadata = this.AvailableLuts[value];
                    this.UpdateModel();
                }, DispatcherPriority.Background);
        }
    }

    private void UpdateModel()
    {
        if (this.doNotUpdateModel)
        {
            return;
        }

        if (this.lutMetadata.LutFormat == LutFormat.None)
        {
            // Call on the model, and NOT the workflow 
            this.model.Reset();
        }
        else
        {
            this.model.Lut(this.lutMetadata);
        }
    }
}
