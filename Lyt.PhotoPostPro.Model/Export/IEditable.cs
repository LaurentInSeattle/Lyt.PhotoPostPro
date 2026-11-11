namespace Lyt.PhotoPostPro.Model.Export;

public interface IEditable
{
    string FriendlyName { get; set; }

    bool CanDelete { get; }
}

