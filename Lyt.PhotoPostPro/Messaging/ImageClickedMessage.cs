namespace Lyt.PhotoPostPro.Messaging;

public sealed record class ImageClickedMessage(
    bool IsSource, bool IsShifted, int PixelX, int PixelY, WriteableBitmap WriteableBitmap);
