namespace Lyt.PhotoPostPro.Model;

using static Lyt.Persistence.FileManagerModel;

public sealed partial class PhotoPostProModel : ModelBase
{
    public bool NewProject(
        string name, string folderPath, 
        bool isSingleImage, 
        Image<Rgb48>? image, // Valid only for single image 
        ProcessMetadata? processMetadata, // Valid only for single image 
        out string errorMessage)
    {
        errorMessage = string.Empty;
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                errorMessage = "Project name is required.";
                Debug.WriteLine(errorMessage);
                return false;
            }

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                errorMessage = "Project folder path is required.";
                Debug.WriteLine(errorMessage);
                return false;
            }

            int imageCount = 0; // Placeholder for actual image count logic
            string imagePath = string.Empty;
            if (isSingleImage)
            {
                imagePath = folderPath;
                if (!File.Exists(imagePath))
                {
                    errorMessage = "Specified image file does not exist.";
                    Debug.WriteLine(errorMessage);
                    return false;
                }

                folderPath = Path.GetDirectoryName(imagePath) ?? string.Empty;
            }
            else
            {
                if (!Directory.Exists(folderPath))
                {
                    errorMessage = "Specified image folder does not exist.";
                    Debug.WriteLine(errorMessage);
                    return false;
                }

                // TODO: Check if any images in the folder
                if (imageCount == 0)
                {
                    errorMessage = "No images found in the specified folder.";
                    Debug.WriteLine(errorMessage);
                    return false;
                }
            }

            // Create metadata and add to projects list
            var projectMetadata = new ProjectMetadata
            {
                Id = FilenamesMgr.NewShortId(),
                Name = name,
                SourceFolderPath = folderPath,
                Created = DateTime.Now,
                LastUpdated = DateTime.Now,
                ImageCount = imageCount,
                IsSingleImage = isSingleImage,
            };
            this.Projects.Add(projectMetadata.Id, projectMetadata);
            this.Save();

            // Create empty project file and save it, save it early because Open below 
            // will try to read it back 
            var project = new Project { Metadata = projectMetadata };
            var fileId = new FileId(Area.User, Kind.Json, projectMetadata.Id, FilenamesMgr.ProjectsFolder);
            this.fileManager.Save(fileId, project);

            if (isSingleImage)
            {
                bool isOpened = this.OpenProject(projectMetadata.Id, out errorMessage);
                if (!isOpened)
                {
                    errorMessage = "Failed to open the created project.";
                    Debug.WriteLine(errorMessage);
                    return false;
                }

                // If No image provided: will load it from path 
                bool isImageAdded = this.AddImageToProject(imagePath, image, processMetadata, out errorMessage);
                if (!isImageAdded)
                {
                    errorMessage = "Failed to add image to the project.";
                    Debug.WriteLine(errorMessage);
                    return false;
                }

                // For single image project, there should be only one post process
                if (this.CurrentProject is null)
                {
                    errorMessage = "Failed to open the project.";
                    Debug.WriteLine(errorMessage);
                    return false;
                }

                this.CurrentPostProcess = this.CurrentProject.PostProcesses[0];

                // Save the changes done in this 'single image' area 
                this.fileManager.Save(fileId, project);
            }

            return true;
        }
        catch (Exception ex)
        {
            // Handle exception
            errorMessage = "An error occurred while creating the project." + ex.Message;
            Debug.WriteLine(ex);
            return false;
        }
    }

    public bool OpenProject(string projectId, out string errorMessage)
    {
        errorMessage = string.Empty;
        try
        {
            if (!this.Projects.TryGetValue(projectId, out var projectMetadata))
            {
                errorMessage = "Project not found.";
                Debug.WriteLine(errorMessage);
                return false;
            }

            // Load project file
            var fileId = new FileId(Area.User, Kind.Json, projectMetadata.Id, FilenamesMgr.ProjectsFolder);
            var project = this.fileManager.Load<Project>(fileId);

            if ((project is not null) && project.Metadata is not null)
            {
                this.CurrentProject = project;
                this.CurrentProjectMetadata = projectMetadata;
                if (this.CurrentProjectMetadata.IsSingleImage)
                {
                    if (this.CurrentProject.PostProcesses.Count > 0)
                    {
                        this.CurrentPostProcess = this.CurrentProject.PostProcesses[0];
                    }
                    else
                    {
                        // Still not an error if we dont have any post process loaded
                        this.CurrentPostProcess = null;
                    }
                }
                else
                {
                    // For multi-image project, we don't open any post process by default
                    this.CurrentPostProcess = null;
                }

                return true;
            }
            else
            {
                errorMessage = "Failed to load project data.";
                Debug.WriteLine(errorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            // Handle exception
            errorMessage = "An error occurred while opening the project." + ex.Message;
            Debug.WriteLine(ex);
            return false;
        }
    }

    public bool CloseProject(out string errorMessage)
    {
        errorMessage = string.Empty;
        try
        {
            // Save current project if needed
            this.CurrentProjectMetadata = null;
            this.CurrentProject = null;
            this.CurrentPostProcess = null;

            return true;
        }
        catch (Exception ex)
        {
            // Handle exception
            errorMessage = "An error occurred while closing the project." + ex.Message;
            Debug.WriteLine(ex);
            return false;
        }
    }

    public bool DeleteProject(out string errorMessage)
    {
        errorMessage = string.Empty;
        try
        {
            return true;
        }
        catch (Exception ex)
        {
            // Handle exception
            Debug.WriteLine(ex);
            errorMessage = "An error occurred while deleting the project." + ex.Message;
            return false;
        }
    }

    public bool AddImageToProject(
        string imagePath, Image<Rgb48>? image, ProcessMetadata? processMetadata, out string errorMessage)
    {
        errorMessage = string.Empty;
        try
        {
            if (this.CurrentProject is null || this.CurrentProjectMetadata is null)
            {
                errorMessage = "No project is currently open.";
                Debug.WriteLine(errorMessage);
                return false;
            }

            if (!File.Exists(imagePath))
            {
                errorMessage = "Specified image file does not exist.";
                Debug.WriteLine(errorMessage);
                return false;
            }

            // Create a new PostProcess object and add it to the current project
            var postProcess = new PostProcess
            {
                ProjectId = this.CurrentProjectMetadata.Id,
                ProcessId = FilenamesMgr.NewShortId(),
                Name = Path.GetFileName(imagePath),
                SourceFilePath = imagePath,
                Created = DateTime.Now,
                LastUpdated = DateTime.Now,
            };

            postProcess.Initialize(); 
            postProcess.SetProject(this.CurrentProject);

            bool sourceImageLoaded = postProcess.LoadSourceImage(image, processMetadata, out errorMessage);
            if (!sourceImageLoaded)
            {
                errorMessage = "Failed to load source image.";
                Debug.WriteLine(errorMessage);
                return false;
            }


            this.CurrentProject.PostProcesses.Add(postProcess);
            return true;
        }
        catch (Exception ex)
        {
            // Handle exception
            errorMessage = "An error occurred while adding the image to the project." + ex.Message;
            Debug.WriteLine(ex);
            return false;
        }
    }
}
