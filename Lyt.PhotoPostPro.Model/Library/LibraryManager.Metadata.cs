namespace Lyt.PhotoPostPro.Model.Library;

using System.IO;

public sealed partial class LibraryManager
{
    public bool Remove(Metadata metadata)
    {
        if (this.fileManager is null)
        {
            throw new Exception("Library Manager is not initialized.");
        }

        try
        {
            string? sourceFolder =
                Path.GetDirectoryName(metadata.FullPath) ??
                throw new Exception("No source folder for: " + metadata.FullPath);

            // TODO : Remove ToList()
            string searchPattern = string.Concat(metadata.Filename, "*.*");
            var files = Directory.EnumerateFiles(
                sourceFolder,
                searchPattern,
                new EnumerationOptions()
                {
                    IgnoreInaccessible = true,
                    MatchCasing = MatchCasing.PlatformDefault,
                }).ToList();
            foreach (string file in files)
            {
                CrossPlatformRecycle.SendToRecycleBin(file);
            }

            // Remove thumbnail from cache 
            string filename = string.Concat(metadata.Filename, "_META.json");
            string thumbnailKey = Path.Combine(sourceFolder, filename);
            if (this.LoadedThumbnails.ContainsKey(thumbnailKey))
            {
                this.LoadedThumbnails.Remove(thumbnailKey);
            }
            else
            {
                // No thumbnail ? 
                if (Debugger.IsAttached) { Debugger.Break(); }
            }

            new LibraryRemovedMessage(metadata).Publish();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            if (Debugger.IsAttached) { Debugger.Break(); }
            return false;
        }
    }

    public string SaveEditParameters(Metadata metadata, PostProcessWorkflow workflow)
    {
        // As of today, we can handle only one edit 
        if (this.fileManager is null || this.model is null)
        {
            throw new Exception("Library Manager is not initialized.");
        }

        try
        {
            // Create target folder if needed 
            MetadataFolders metadataFolders = new(metadata);
            string targetFolder = metadataFolders.CreateDirectoryPathIfNeeded(this.libraryFolderPath);
            string? sourceFolder =
                Path.GetDirectoryName(metadata.FullPath) ??
                throw new Exception("No source folder for: " + metadata.FullPath);
            string fileId = workflow.PostProcess.FileUidString;
            string filenameEdit = string.Concat(metadata.Filename, "_EDIT", fileId, ".json");
            string targetPathEdit = Path.Combine(targetFolder, filenameEdit);
            PostProcessParameters postProcessParameters;
            if (File.Exists(targetPathEdit))
            {
                string read = File.ReadAllText(targetPathEdit);
                postProcessParameters = this.fileManager.Deserialize<PostProcessParameters>(read);
                postProcessParameters.Update(workflow);
            }
            else
            {
                postProcessParameters = new PostProcessParameters(workflow);
            }

            string serialized = this.fileManager.Serialize<PostProcessParameters>(postProcessParameters);
            File.WriteAllText(targetPathEdit, serialized);

            return targetPathEdit;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            if (Debugger.IsAttached) { Debugger.Break(); }
            return string.Empty;
        }
    }

    public List<ExistingPostProcessParameters> EnumerateExistingEditParameters(Metadata metadata)
    {
        if (this.fileManager is null || this.model is null)
        {
            throw new Exception("Library Manager is not initialized.");
        }

        List<ExistingPostProcessParameters> list = [];
        string? targetFolder = Path.GetDirectoryName(metadata.FullPath);
        if (targetFolder is null)
        {
            return list;
            // throw new Exception("No source folder for: " + metadata.FullPath);
        }

        string filenameEditPattern = string.Concat(metadata.Filename, "_EDIT", "*", ".json");
        var editFiles = Directory.EnumerateFiles(
            targetFolder,
            filenameEditPattern,
            new EnumerationOptions()
            {
                IgnoreInaccessible = true,
                MatchCasing = MatchCasing.PlatformDefault,
                RecurseSubdirectories = false
            });

        foreach (string editFile in editFiles)
        {
            try
            {
                string fileUid = editFile.Replace(metadata.Filename, string.Empty);
                fileUid = fileUid.Replace("_EDIT", string.Empty);
                fileUid = fileUid.Replace(".json", string.Empty);
                Debug.WriteLine(" " + editFile + " " + fileUid);
                string read = File.ReadAllText(editFile);
                var postProcessParameters = this.fileManager.Deserialize<PostProcessParameters>(read);
                ExistingPostProcessParameters existingPostProcessParameters = new(fileUid, postProcessParameters);
                list.Add(existingPostProcessParameters);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

                // Swallow : Do nothing 
            }
        }

        return list;
    }

    public void SaveMetadata(Metadata metadata)
    {
        if (this.fileManager is null || this.model is null)
        {
            throw new Exception("Library Manager is not initialized.");
        }

        try
        {
            // Update the cache 
            string key = metadata.MetadataFullPath();
            if (this.LoadedThumbnails.TryGetValue(key, out LoadedThumbnail? loadedThumbnail))
            {
                loadedThumbnail.Update(metadata);
            }

            // Save to disk 
            MetadataFolders metadataFolders = new(metadata);
            string targetFolder = metadataFolders.CreateDirectoryPathIfNeeded(this.libraryFolderPath);
            string filenameMetadata = metadata.Filename + "_META.json";
            string targetPathMetadata = Path.Combine(targetFolder, filenameMetadata);
            string serialized = this.fileManager.Serialize<Metadata>(metadata);
            File.WriteAllText(targetPathMetadata, serialized);

            // notify UI 
            new LibraryMetadataUpdateMessage(metadata).Publish();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            if (Debugger.IsAttached) { Debugger.Break(); }
        }
    }
}
