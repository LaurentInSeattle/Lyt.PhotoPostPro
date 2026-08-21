namespace Lyt.PhotoPostPro.Model.Messaging;

public sealed record class WorkflowUpdateMessage(
    ProcessStep? PreviousStep, ProcessStep CurrentStep, WorkflowUpdateKind Kind);

public sealed record class WorkflowAbortMessage();

public sealed record class WorkflowProgressMessage(string StepLocalizationName);
