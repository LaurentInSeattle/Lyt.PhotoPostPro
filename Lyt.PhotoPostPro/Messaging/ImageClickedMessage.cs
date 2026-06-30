namespace Lyt.PhotoPostPro.Messaging;

public sealed record class ImageClickedMessage(bool IsSource, int PixelX, int PixelY, WriteableBitmap WriteableBitmap);
