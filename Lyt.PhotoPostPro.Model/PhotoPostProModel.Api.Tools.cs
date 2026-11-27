namespace Lyt.PhotoPostPro.Model;

public sealed partial class PhotoPostProModel : ModelBase
{
    public bool AddLibraryFolder(LibraryFolder newLibraryFolder, out string message)
    {
        message = string.Empty;
        LibraryFolder? existing = this.LibraryFolders.FromFriendlyName(newLibraryFolder.FriendlyName.Trim());
        if (existing is not null)
        {
            message = "Tools.Editor.Validation.FriendlyNameAlreadyExists";
            return false;
        }

        try
        {
            this.LibraryFolders.AddLibraryFolder(newLibraryFolder);
            this.Save();
            return true;
        }
        catch (Exception ex)
        {
            this.Logger.Error(" Failed to add new library folder: " + newLibraryFolder.FriendlyName + "  " + ex);
            message = "Tools.Editor.Validation.AddLibraryFolderFailed";
            return false;
        }
    }

    public bool DeleteLibraryFolder(string friendlyName, out string message)
    {
        message = string.Empty;
        LibraryFolder? existing = this.LibraryFolders.FromFriendlyName(friendlyName.Trim());
        if (existing is null)
        {
            message = "Tools.Editor.Validation.FriendlyNameNotFound";
            return false;
        }

        try
        {
            this.LibraryFolders.AvailableLibraryFolders.Remove(existing);
            this.Save();
            return true;
        }
        catch (Exception ex)
        {
            this.Logger.Error(" Failed to delete library folder: " + friendlyName + "  " + ex);
            message = "Tools.Editor.Validation.DeleteLibraryFolderFailed";
            return false;
        }
    }


    public bool EditSignature(Signature newSignature, out string message)
    {
        if (this.DeleteSignature(newSignature.FriendlyName, out message))
        {
            return this.AddSignature(newSignature, withSave: true, out message);
        }

        return false;
    }

    public bool AddSignature(Signature newSignature, bool withSave, out string message)
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
            if ( withSave)
            {
                this.Save();
            }

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
            return this.AddWatermark(newWatermark, withSave: true, out message);
        }

        return false;
    }

    public bool AddWatermark(Watermark newWatermark, bool withSave, out string message)
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
            if (withSave)
            {
                this.Save();
            }

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

    public bool EditImageExport(ImageExport newImageExport, out string message)
    {
        if (this.DeleteImageExport(newImageExport.FriendlyName, out message))
        {
            return this.AddImageExport(newImageExport, withSave: true, out message);

        }

        return false;
    }

    public bool AddImageExport(ImageExport newImageExport, bool withSave, out string message)
    {
        message = string.Empty;
        ImageExport? existing = this.ImageExports.FromFriendlyName(newImageExport.FriendlyName.Trim());
        if (existing is not null)
        {
            message = "Tools.Editor.Validation.FriendlyNameAlreadyExists";
            return false;
        }

        try
        {
            this.ImageExports.AvailableImageExports.Add(newImageExport);
            if (withSave)
            {
                this.Save();
            }

            return true;
        }
        catch (Exception ex)
        {
            this.Logger.Error(" Failed to add new image export: " + newImageExport.FriendlyName + "  " + ex);
            message = "Tools.Editor.Validation.AddImageExportFailed";
            return false;
        }
    }

    public bool DeleteImageExport(string friendlyName, out string message)
    {
        message = string.Empty;
        ImageExport? existing = this.ImageExports.FromFriendlyName(friendlyName.Trim());
        if (existing is null)
        {
            message = "Tools.Editor.Validation.FriendlyNameNotFound";
            return false;
        }

        try
        {
            this.ImageExports.AvailableImageExports.Remove(existing);
            this.Save();
            return true;
        }
        catch (Exception ex)
        {
            this.Logger.Error(" Failed to delete image export: " + friendlyName + "  " + ex);
            message = "Tools.Editor.Validation.DeleteImageExportFailed";
            return false;
        }
    }

    internal bool CreateDefaultLibraryFolder()
    {
        bool needModelSave = false;
        if (this.LibraryFolders.AvailableLibraryFolders.Count == 0)
        {
            this.LibraryFolders.SelectedLibraryFolderName = LibraryFolder.DefaultLibraryFolderName;
            this.LibraryFolders.AvailableLibraryFolders.Add(LibraryFolder.Default);
            needModelSave = true;
        }

        return needModelSave;
    }

    internal bool CreateDefaultExports()
    {
        bool needModelSave = false;
        if (this.Signatures.AvailableSignatures.Count == 0)
        {
            this.AddSignature(Signature.Default, withSave: false, out string _);
            needModelSave = true;
        }

        if (this.Watermarks.AvailableWatermarks.Count == 0)
        {
            this.AddWatermark(Watermark.Default, withSave: false, out string _);
            needModelSave = true;
        }

        if (this.ImageExports.AvailableImageExports.Count == 0)
        {
            this.AddImageExport(ImageExport.Default, withSave: false, out string _);
            this.AddImageExport(ImageExport.HiDef, withSave: false, out string _);
            this.AddImageExport(ImageExport.Web, withSave: false, out string _);
            needModelSave = true;
        }

        return needModelSave;
    }

    internal bool CreateDefaultTypography()
    {
        bool needModelSave = false;
        if (this.TextStyles.AvailableSlotTextStyles.Count == 0)
        {
            this.AddSlotTextStyle(SlotTextStyle.Title, withSave: false, out string _);
            this.AddSlotTextStyle(SlotTextStyle.Header_1, withSave: false, out string _);
            this.AddSlotTextStyle(SlotTextStyle.Header_2, withSave: false, out string _);
            this.AddSlotTextStyle(SlotTextStyle.LargeBodyText, withSave: false, out string _);
            this.AddSlotTextStyle(SlotTextStyle.SmallBodyText, withSave: false, out string _);
            needModelSave = true;
        }

        return needModelSave;
    }

    public bool AddSlotTextStyle(SlotTextStyle newSlotTextStyle, bool withSave, out string message)
    {
        message = string.Empty;
        SlotTextStyle? existing = this.TextStyles.FromFriendlyName(newSlotTextStyle.FriendlyName.Trim());
        if (existing is not null)
        {
            message = "Tools.Editor.Validation.FriendlyNameAlreadyExists";
            return false;
        }

        try
        {
            this.TextStyles.AvailableSlotTextStyles.Add(newSlotTextStyle);
            if (withSave)
            {
                this.Save();
            } 

            return true;
        }
        catch (Exception ex)
        {
            this.Logger.Error(" Failed to add new image export: " + newSlotTextStyle.FriendlyName + "  " + ex);
            message = "Tools.Editor.Validation.AddImageExportFailed";
            return false;
        }
    }

}
