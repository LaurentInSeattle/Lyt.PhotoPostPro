namespace Lyt.PhotoPostPro.Interfaces;

public interface IDropPathHandler
{
    void OnDropPath(string path, bool isDirectory);

    bool IsActivated { get; }
}
