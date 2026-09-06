namespace Lyt.PhotoPostPro.Model;

public sealed partial class PhotoPostProModel : ModelBase
{
    public bool EditSignature(Signature newSignature, out string message)
    {
        if (this.DeleteSignature(newSignature.FriendlyName, out message))
        {
            return this.AddSignature(newSignature, out message);

        }

        return false;
    }

    public bool AddSignature(Signature newSignature, out string message)
    {
        message = string.Empty;
        Signature? existing = this.Signatures.FromFriendlyName(newSignature.FriendlyName.Trim());
        if (existing is not null)
        {
            message = "Tools.Editor.Validation.FriendlyNameAlreadyExists";
            return false;
        }

        try
        {
            this.Signatures.AvailableSignatures.Add(newSignature);
            this.Save();
            return true;
        }
        catch (Exception ex)
        {
            this.Logger.Error(" Failed to add new signature: " + newSignature.FriendlyName + "  " + ex);
            message = "Tools.Editor.Validation.AddSignatureFailed";
            return false;
        }
    }

    public bool DeleteSignature(string friendlyName, out string message)
    {
        message = string.Empty;
        Signature? existing = this.Signatures.FromFriendlyName(friendlyName.Trim());
        if (existing is null)
        {
            message = "Tools.Editor.Validation.FriendlyNameNotFound";
            return false;
        }

        try
        {
            this.Signatures.AvailableSignatures.Remove(existing);
            this.Save();
            return true;
        }
        catch (Exception ex)
        {
            this.Logger.Error(" Failed to delete signature: " + friendlyName + "  " + ex);
            message = "Tools.Editor.Validation.DeleteSignatureFailed";
            return false;
        }
    }

    public bool EditWatermark(Watermark newWatermark, out string message)
    {
        if (this.DeleteWatermark(newWatermark.FriendlyName, out message))
        {
            return this.AddWatermark(newWatermark, out message);

        }

        return false;
    }

    public bool AddWatermark(Watermark newWatermark, out string message)
    {
        message = string.Empty;
        Watermark? existing = this.Watermarks.FromFriendlyName(newWatermark.FriendlyName.Trim());
        if (existing is not null)
        {
            message = "Tools.Editor.Validation.FriendlyNameAlreadyExists";
            return false;
        }

        try
        {
            this.Watermarks.AvailableWatermarks.Add(newWatermark);
            this.Save();
            return true;
        }
        catch (Exception ex)
        {
            this.Logger.Error(" Failed to add new watermark: " + newWatermark.FriendlyName + "  " + ex);
            message = "Tools.Editor.Validation.AddWatermarkFailed";
            return false;
        }
    }

    public bool DeleteWatermark(string friendlyName, out string message)
    {
        message = string.Empty;
        Watermark? existing = this.Watermarks.FromFriendlyName(friendlyName.Trim());
        if (existing is null)
        {
            message = "Tools.Editor.Validation.FriendlyNameNotFound";
            return false;
        }

        try
        {
            this.Watermarks.AvailableWatermarks.Remove(existing);
            this.Save();
            return true;
        }
        catch (Exception ex)
        {
            this.Logger.Error(" Failed to delete watermark: " + friendlyName + "  " + ex);
            message = "Tools.Editor.Validation.DeleteWatermarkFailed";
            return false;
        }
    }
}
