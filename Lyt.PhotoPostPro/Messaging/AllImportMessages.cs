namespace Lyt.PhotoPostPro.Messaging;

public sealed record class ImportCompleteMessage(
    bool Completed,
    int Count,
    int Imports,
    int Errors,
    string Message = "");

public sealed record class ImportFileMessage(
    bool IsSuccess,
    string Path,
    string Message = "",
    LoadedImage? LoadedImage = null);
