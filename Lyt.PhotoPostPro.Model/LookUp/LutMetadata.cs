namespace Lyt.PhotoPostPro.Model.LookUp;

public enum LutFormat
{
    Cube, 
    ThreeDL, 
}

internal sealed record class LutMetadata(string FriendlyName, string Path, LutFormat LutFormat, bool IsEmbedded); 
