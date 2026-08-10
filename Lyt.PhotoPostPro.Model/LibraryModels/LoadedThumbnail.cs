namespace Lyt.PhotoPostPro.Model.LibraryModels;

public sealed class LoadedThumbnail(Metadata metadata, byte[] imageBytes)
{
    public Metadata Metadata { get; private set; } = metadata;

    public byte[] ImageBytes { get; private set; } = imageBytes; 

    public void Update(Metadata metadata) => this.Metadata = metadata; 
}
