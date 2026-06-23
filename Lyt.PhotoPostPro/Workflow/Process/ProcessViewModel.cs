namespace Lyt.PhotoPostPro.Workflow.Process;

public sealed partial class ProcessViewModel :
    ViewModel<ProcessView>,
    IRecipient<WorkflowUpdateMessage>
{
    private readonly PhotoPostProModel model;
    private ViewSelector<ActivatedView>? viewSelector;
    private bool isFirstActivation;

    [ObservableProperty]
    public partial HistogramViewModel HistogramViewModel {  get ; set; }

    [ObservableProperty]
    public partial ToolboxHostViewModel ToolboxHostViewModel { get; set; }

    public ProcessViewModel(PhotoPostProModel photoPostProModel)
    {
        this.model = photoPostProModel;
        this.isFirstActivation = true;
        this.HistogramViewModel = new();
        this.ToolboxHostViewModel = new();
        this.Subscribe<WorkflowUpdateMessage>();
    }

    public override void Activate(object? activationParameters)
    {
        base.Activate(activationParameters);
        if (this.isFirstActivation)
        {
            this.isFirstActivation = false;
            this.SetupWorkflow();
        }

        this.InitializeWorkflow();
        var postProcess = this.model.CurrentPostProcess;
        if (postProcess is not null)
        {
            this.model.BeginPostProcess(); 
        }
    }

    public override void Deactivate()
    {
        base.Deactivate();

        // TODO: Close and save all project data 
    }

    public void Receive(WorkflowUpdateMessage message)
    {
        if (this.viewSelector is null)
        {
            return;
        }

        Dispatch.OnUiThread(() => 
        {
            var workflow = this.model.Workflow;
            string stepName = workflow.CurrentStep.Name;
            ActivatedView view = FromWorkflowstepName(stepName);
            this.viewSelector.SelectView(view);
        }, DispatcherPriority.Normal);

    }

    public void InitializeWorkflow ()
    {
        foreach (var selectableView in this.viewSelector!.SelectableViews)
        {
            if (selectableView.PrimaryViewModel is ViewModel stepViewModel)
            {
                stepViewModel.Initialize();
            }

            // We dont have any toolbars for now, but maybe later we'll do 
            if (selectableView.SecondaryViewModel is ViewModel toolbarViewModel)
            {
                toolbarViewModel.Initialize();
            }

            if (selectableView.TernaryViewModel is ViewModel toolboxViewModel)
            {
                toolboxViewModel.Initialize();
            }
        }
    }

    private void SetupWorkflow()
    {
        var selectableViews = new List<SelectableView<ActivatedView>>();

        void Setup<TViewModel, TControl, TToolboxViewModel, TPostProcessStep, TToolboxControl>(
                ActivatedView activatedView, Control? control = null)
            where TViewModel : ViewModel<TControl>
            where TControl : Control, IView, new()
            where TToolboxViewModel : ToolboxViewModel<TToolboxControl, TPostProcessStep>
            where TPostProcessStep : PostProcessStep
            where TToolboxControl : View, new()
        {
            var vm = App.GetRequiredService<TViewModel>();
            vm.CreateViewAndBind();
            var vmToolbox = App.GetRequiredService<TToolboxViewModel>();
            vmToolbox.CreateViewAndBind();
            vmToolbox.ToolboxHostViewModel = this.ToolboxHostViewModel; 
            var selectable = new SelectableView<ActivatedView>(activatedView, vm, control, null, vmToolbox);
            selectableViews.Add(selectable);
        }

        // No buttons or toolbars for all process views: 
        Setup<OrientViewModel, OrientView, OrientToolboxViewModel, OrientationStep, OrientToolboxView>(ActivatedView.Orient);
        Setup<StraightenViewModel, StraightenView, StraightenToolboxViewModel, StraightenStep, StraightenToolboxView>(ActivatedView.Straighten);
        Setup<ComposeViewModel, ComposeView, ComposeToolboxViewModel, CompositionStep,  ComposeToolboxView>(ActivatedView.Compose);
        Setup<ExposureViewModel, ExposureView, ExposureToolboxViewModel, ExposureStep, ExposureToolboxView>(ActivatedView.Exposure);
        Setup<RecoveryViewModel, RecoveryView, RecoveryToolboxViewModel, RecoveryStep,  RecoveryToolboxView>(ActivatedView.Recovery);
        Setup<WhiteBalanceViewModel, WhiteBalanceView, WhiteBalanceToolboxViewModel, WhiteBalanceStep, WhiteBalanceToolboxView>(ActivatedView.WhiteBalance);
        Setup<ContrastViewModel, ContrastView, ContrastToolboxViewModel, ContrastStep, ContrastToolboxView>(ActivatedView.Contrast);

        // Avalonia has a ColorView, so we need to specify part of the namespace here to avoid ambiguity.
        Setup<ColorViewModel, Color.ColorView, ColorToolboxViewModel, ColorStep, ColorToolboxView>(ActivatedView.Color);
        
        Setup<ExportViewModel, ExportView, ExportToolboxViewModel, ExportStep, ExportToolboxView>(ActivatedView.Export);

        //Setup<SharpenViewModel, SharpenView, SharpenToolboxViewModel, SharpenToolboxView>(ActivatedView.Sharpen);
        //Setup<TouchUpViewModel, TouchUpView, TouchUpToolboxViewModel, TouchUpToolboxView>(ActivatedView.TouchUp);
        //Setup<DenoiseViewModel, DenoiseView, DenoiseToolboxViewModel, DenoiseToolboxView>(ActivatedView.Denoise);
        //Setup<CleanupViewModel, CleanupView, CleanupToolboxViewModel, CleanupToolboxView>(ActivatedView.Cleanup);

        // Needs to be kept alive as a class member, or else callbacks will die (and wont work) 
        this.viewSelector =
            new ViewSelector<ActivatedView>(
                this.View.ProcessViewContent,
                null, // no toolbars 
                null, // no buttons 
                selectableViews,
                this.OnViewSelected,
                this.View.ToolboxHostView.ContentGrid);
    }

    private void OnViewSelected(ActivatedView activatedView)
    {
        Debug.WriteLine($"Activated view: {activatedView}");

        if (this.viewSelector is null)
        {
            throw new Exception("No view selector");
        }

        var currentTernaryViewModel = this.viewSelector.CurrentTernaryViewModel;
        if (currentTernaryViewModel is IToolboxViewModel toolboxViewModel)
        {
            this.ToolboxHostViewModel.ActiveToolboxViewModel = toolboxViewModel;
        } 
    }

    private static ActivatedView FromWorkflowstepName(string workflowStepName)
        => workflowStepName switch
        {
            PostProcessStep.OrientationStepName => ActivatedView.Orient,
            PostProcessStep.StraightenStepName => ActivatedView.Straighten,
            PostProcessStep.CompositionStepName => ActivatedView.Compose,
            PostProcessStep.ExposureStepName => ActivatedView.Exposure,
            PostProcessStep.RecoveryStepName => ActivatedView.Recovery,
            PostProcessStep.WhiteBalanceStepName => ActivatedView.WhiteBalance,
            PostProcessStep.ContrastStepName => ActivatedView.Contrast,
            PostProcessStep.ColorStepName => ActivatedView.Color,

            // TODO: Add the rest 
            PostProcessStep.ExportStepName => ActivatedView.Export,

            _ => throw new NotImplementedException("Missing step name."),
        };
}
