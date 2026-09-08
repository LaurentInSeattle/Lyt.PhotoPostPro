namespace Lyt.PhotoPostPro.Model.Export;

public sealed class SignaturesCollection
{
    public List<Signature> AvailableSignatures { get; set; } = [];

    public SignaturesCollection() { /* Required for JSON serialization */ }

    public Signature? FromFriendlyName(string friendlyName)
        => this.AvailableSignatures.FirstOrDefault(
            s => s.FriendlyName.Equals( friendlyName, StringComparison.InvariantCultureIgnoreCase));

    public bool IsAvailable(string friendlyName)
        => this.FromFriendlyName(friendlyName) is not null;
}
