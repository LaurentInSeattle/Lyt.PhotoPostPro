namespace Lyt.PhotoPostPro.Model.Export;

/// <summary> Will load from disk ~ LATER </summary>
public sealed class SignaturesCollection
{
    public List<Signature> AvailableSignatures { get; set; } = [];

    public SignaturesCollection() => this.AvailableSignatures.Add(Signature.Default);

    public Signature? FromFriendlyName(string friendlyName)
        => this.AvailableSignatures.FirstOrDefault(s => s.FriendlyName == friendlyName);
}
