namespace Lyt.PhotoPostPro.Model.Messaging;

public enum FolderTreeKind
{
    Captured, 
    Unrated, 
    Edited, 
}

public sealed record class FolderTreeUpdatedMessage(FolderTreeKind FolderTreeKind, DayFolder? DayFolder = null);
