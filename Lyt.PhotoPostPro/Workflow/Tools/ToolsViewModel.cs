namespace Lyt.PhotoPostPro.Workflow.Tools;

public sealed partial class ToolsViewModel : ViewModel<ToolsView>
{
    private static readonly Dictionary<string, ActivatedView> ToolsString = new()
    {
        { "Tools.Select.Signatures", ActivatedView.Signatures },
        { "Tools.Select.Watermarks", ActivatedView.Watermarks },
        { "Tools.Select.Exports", ActivatedView.Exports },
    };

    private readonly PhotoPostProModel model;
    private readonly IToaster toaster;

    private ViewSelector<ActivatedView>? viewSelector;
    private bool isFirstActivation;

    public ToolsViewModel(PhotoPostProModel model, IToaster toaster)
    {
        this.model = model;
        this.toaster = toaster;
        this.isFirstActivation = true;
    }

    [ObservableProperty]
    public partial List<SelectorButtonViewModel> ToolsButtons { get; set; } = [];

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);

        if (this.isFirstActivation)
        {
            // This cannot be done in the constructor
            this.SetupWorkflow();
            this.BuildToolsButtons(); 
            this.isFirstActivation = false;

            // Need to schedule so that the newly created control is bound to its view model 
            Schedule.OnUiThread(90, () =>
            {
                var button = this.ToolsButtons[0]; 
                if (button.IsBound)
                {
                    button.Select();
                }
            }, DispatcherPriority.Background);
        }
    }

    private void BuildToolsButtons()
    {
        List<SelectorButtonViewModel> toolList = [];
        foreach (string toolName in ToolsString.Keys)
        {
            string label = this.Localize(toolName);
            ActivatedView activatedView = ToolsString[toolName];
            var vm = new SelectorButtonViewModel(label, 220, 54, this.OnSelectTool, activatedView);
            toolList.Add(vm);
        }

        this.ToolsButtons = toolList;
    }

    private void OnSelectTool(object? tag)
    {
        if (tag is not ActivatedView activatedView)
        {
            return;
        }

        if (this.viewSelector is null)
        {
            throw new Exception("No view selector");
        }

        this.viewSelector.SelectView(activatedView);
    }

    private void SetupWorkflow()
    {
        var selectableViews = new List<SelectableView<ActivatedView>>();

        void Setup<TViewModel, TControl>(ActivatedView activatedView)
            where TViewModel : ViewModel<TControl>
            where TControl : UserControl, IView, new()
        {
            var vm = App.GetRequiredService<TViewModel>();
            vm.CreateViewAndBind();
            selectableViews.Add(new SelectableView<ActivatedView>(activatedView, vm));
        }

        // No buttons, toolbox or toolbars for all tool views: 
        Setup<ExportsViewModel, ExportsView>(ActivatedView.Exports);
        Setup<SignaturesViewModel, SignaturesView>(ActivatedView.Signatures);
        Setup<WatermarksViewModel, WatermarksView>(ActivatedView.Watermarks);

        var animationService = App.GetRequiredService<IAnimationService>();
        this.viewSelector =
            new ViewSelector<ActivatedView>(
                this.View.ViewContent,
                null, // no toolbars 
                null, // no buttons 
                selectableViews,
                this.OnViewSelected,
                null, // no toolbox host view
                animationService,
                animationDuration: 0.25);
    }

    private void OnViewSelected(ActivatedView activatedView)
    {
        Debug.WriteLine($"Activated view: {activatedView}");

        if (this.viewSelector is null)
        {
            throw new Exception("No view selector");
        }
    }
}
