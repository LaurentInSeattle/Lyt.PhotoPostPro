namespace Lyt.PhotoPostPro.Model.Messaging;

public sealed record class LibraryLoadedMessage(int ImageCount, int ErrorCount);

public sealed record class LibraryRemovedMessage(Metadata Metadata);

public sealed record class LibraryMetadataUpdateMessage(Metadata Metadata);
