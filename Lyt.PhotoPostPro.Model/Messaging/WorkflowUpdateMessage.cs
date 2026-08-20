namespace Lyt.PhotoPostPro.Model.Messaging;

public sealed record class WorkflowUpdateMessage(
    PostProcessStep? PreviousStep, PostProcessStep CurrentStep, WorkflowUpdateKind Kind);

public sealed record class WorkflowAbortMessage();

