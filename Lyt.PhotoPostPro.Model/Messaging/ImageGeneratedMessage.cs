namespace Lyt.PhotoPostPro.Model.Messaging;

public sealed record class SourceImageGeneratedMessage(Frame Frame);

public sealed record class ResultImageGeneratedMessage(Frame Frame);
