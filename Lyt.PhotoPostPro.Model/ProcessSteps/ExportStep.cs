namespace Lyt.PhotoPostPro.Model.ProcessSteps;

using System.IO;

public class ExportStep(ProcessWorkflow processWorkflow) :
    ProcessStep(processWorkflow, ProcessStep.ExportStepName)
{
    public const string ExportTag = "EXP_";

    private string currentDirectoryExport = string.Empty;

    public override void Initialize(Image<RgbaHalf> originalImage) { }

    // DO NOT call the base class or else it will call Transform 
    public override void Activate(WorkflowUpdateKind workflowUpdateKind) => this.Reset();

    // For abstract compliance only 
    internal override Frame? Transform(bool withFrame = true) { return null;  }

    // For abstract compliance only 
    protected override void SetIdentity() => base.IsIdentity = false;

    // For abstract compliance only 
    public override void PerformStep(ProcessParameters postProcessParameters) { }

    public override Frame? Reset()
    {
        if (this.SourceImage is null)
        {
            return null;
        }

        // Pick the possibly rotated or mirrored from the very first step as 
        // the source image so that comparisons can be made 
        // The result image from the previous step is our current source
        // so we re-use it and that becomes our result image... 
        int currentStepIndex = this.ProcessWorkflow.CurrentStepIndex;
        var previousResult = this.ProcessWorkflow.Steps[currentStepIndex - 1].ResultImage;
        if (previousResult is not null)
        {
            this.ResultImage = previousResult;
            this.SourceImage = this.ProcessWorkflow.Steps[0].ResultImage;
            return this.ResultImage.ToFrame();
        }
        else
        {
            // Should never happen
            if (Debugger.IsAttached) { Debugger.Break(); }
            return null;
        }
    }

    internal Frame? Export(ExportParameters exportParameters)
    {
        if (this.ResultImage is null)
        {
            return null;
        }

        var images = exportParameters.Images;
        if (images.Count == 0)
        {
            exportParameters.Images.Add(ImageParameters.Default);
        }

        PhotoPostProModel model = this.ProcessWorkflow.Model;

        // Create subdirectory for exported images
        string sourceImagePath;
        string fileName;
        string subDirectoryExport;
        try
        {
            string exportFolderPath = model.LibraryManager.ExportsFolderPath;
            sourceImagePath = this.ProcessWorkflow.SourceFilePath;
            FileInfo fi = new(sourceImagePath);
            string? sourceDirectory = fi.DirectoryName;
            if (string.IsNullOrWhiteSpace(sourceDirectory))
            {
                throw new Exception("No directory");
            }

            fileName = System.IO.Path.GetFileNameWithoutExtension(fi.Name);
            string timestamp = FileManagerModel.BriefTimestampString();
            string subDirName = ExportTag + fileName + "_" + timestamp;
            subDirectoryExport = System.IO.Path.Combine(exportFolderPath, subDirName);
            if (!Directory.Exists(subDirectoryExport))
            {
                Directory.CreateDirectory(subDirectoryExport);
            }

            this.currentDirectoryExport = subDirectoryExport;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            return null;
        }

        string ExportImage(ImageParameters imageParameters, string folderPath)
        {
            try
            {
                // Resize and rescale 
                // ! Compiler fail ? Verified at begin of method 
                Image<RgbaHalf> imageToResize = this.ResultImage!.Clone();
                switch (imageParameters.Action)
                {
                    default:
                    case ExportAction.None:
                        break;

                    case ExportAction.ToFileSize:
                        // TODO: Implement file size export logic here
                        throw new NotImplementedException("ExportAction.ToFileSize is not implemented yet.");

                    case ExportAction.ToScale:
                        // TODO: Implement to scale export logic here
                        throw new NotImplementedException("ExportAction.ToScale is not implemented yet.");

                    case ExportAction.ToDimensions:
                        int dimension = imageParameters.Dimension;
                        int width = imageToResize.Width;
                        int height = imageToResize.Height;
                        if (width > height)
                        {
                            float scale = width / (float)dimension;
                            int newHeight = (int)(height / scale);
                            imageToResize.Mutate(x => x.Resize(dimension, newHeight));
                        }
                        else
                        {
                            float scale = height / (float)dimension;
                            int newWidth = (int)(width / scale);
                            imageToResize.Mutate(x => x.Resize(newWidth, dimension));
                        }

                        break;
                }

                // Add watermark, if specified
                Image<RgbaHalf> imageWithWatermark = imageToResize;
                if (imageParameters.WithWatermark)
                {
                    Watermark? watermark = model.Watermarks.FromKey(imageParameters.WatermarkKey);
                    if (watermark is not null)
                    {
                        // Adding watermark : Placement 
                        int fontSpace = (int)(watermark.FontSize * 0.7);
                        PointF origin = new(imageWithWatermark.Width / 2, 0.8f * imageWithWatermark.Height / 2);

                        // Adding watermark : Drawing
                        Font font = SystemFonts.CreateFont(watermark.FontFamily, watermark.FontSize, watermark.FontStyle);
                        var textOptions = new RichTextOptions(font)
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                            TextAlignment = TextAlignment.Center,
                            Origin = origin,
                            LayoutMode = LayoutMode.HorizontalTopBottom,
                            MaxLines = 20,
                            WordBreaking = WordBreaking.Standard,
                            WrappingLength = imageWithWatermark.Width * 0.9f, // Wrap text to 90% of image width
                        };

                        imageWithWatermark.Mutate(x => x.Paint(canvas =>
                        {
                            canvas.DrawText(textOptions, watermark.Text, Brushes.Solid(watermark.Color), pen: null);
                        }));
                    }
                }

                // Add Borders, if specified
                Image<RgbaHalf> imageWithBorders = imageWithWatermark;
                if (imageParameters.WithBorders)
                {
                    Color borderColor =
                        imageParameters.BorderStyle == ImageBorderStyle.BlackBorder ? Color.Black : Color.White;
                    double thicknessFactor =
                        imageParameters.BorderThickness == ImageBorderThickness.Thick ? 3.5 : 2.5;
                    int borderWidth = (int)(thicknessFactor * imageWithBorders.Width / 100.0);
                    int borderHeight = (int)(thicknessFactor * imageWithBorders.Height / 100.0);
                    int border = Math.Max(borderWidth, borderHeight);
                    // Weigthed padding would need a second pass 
                    imageWithBorders.Mutate(x => x.Resize(
                        new ResizeOptions
                        {
                            // Add twice the border width to both dimensions for the final canvas size
                            Size = new Size(imageWithBorders.Width + border * 2, imageWithBorders.Height + border * 2),
                            Mode = ResizeMode.BoxPad,
                            PadColor = borderColor,
                        }));
                }

                // Add signature, if specified
                Image<RgbaHalf> imageWithSignature = imageWithBorders;
                if (imageParameters.WithSignature)
                {
                    Signature? signature = model.Signatures.FromFriendlyName(imageParameters.SignatureKey);
                    if (signature is not null)
                    {
                        // Adding signature : Placement 
                        int fontSpace = (int)(signature.FontSize * 0.7);
                        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Right;
                        PointF origin = new();
                        switch (signature.Location)
                        {
                            default:
                            case SignatureLocation.BottomRight:
                                origin = new PointF(imageWithBorders.Width - fontSpace, imageWithBorders.Height - 2 * fontSpace);
                                horizontalAlignment = HorizontalAlignment.Right;
                                break;

                            case SignatureLocation.TopLeft:
                                horizontalAlignment = HorizontalAlignment.Left;
                                origin = new PointF(fontSpace, fontSpace);
                                break;

                            case SignatureLocation.TopRight:
                                horizontalAlignment = HorizontalAlignment.Right;
                                origin = new PointF(imageWithBorders.Width - fontSpace, fontSpace);
                                break;

                            case SignatureLocation.BottomLeft:
                                horizontalAlignment = HorizontalAlignment.Left;
                                origin = new PointF(fontSpace, imageWithBorders.Height - 2 * fontSpace);
                                break;
                        }

                        // Adding signature : Drawing
                        Font font = SystemFonts.CreateFont(signature.FontFamily, signature.FontSize, signature.FontStyle);
                        var textOptions = new RichTextOptions(font)
                        {
                            HorizontalAlignment = horizontalAlignment,
                            Origin = origin,
                        };

                        imageWithSignature.Mutate(x => x.Paint(canvas =>
                        {
                            canvas.DrawText(textOptions, signature.Text, Brushes.Solid(signature.Color), pen: null);
                        }));
                    }
                }

                // Saving export formatted image: pick encoder and file extension  
                Image<RgbaHalf> finalImage = imageWithSignature;
                var encoder = imageParameters.ImageEncoder;
                string extension = imageParameters.FileExtension;
                string exportPath =
                    System.IO.Path.Combine(folderPath, fileName + imageParameters.PostFix + extension);
                finalImage.Save(exportPath, encoder);
                return exportPath;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                return string.Empty;
            }
        }

        try
        {
            // Create target folder if needed 
            var postProcess = this.ProcessWorkflow;
            var libraryManager = postProcess.Model.LibraryManager;
            var metadata = postProcess.Metadata;
            MetadataFolders metadataFolders = new(metadata);
            string libraryFolderPath = libraryManager.LibraryFolderPath;
            string targetFolder = metadataFolders.CreateDirectoryPathIfNeeded(libraryFolderPath);
            string? imageLibraryFolderPath =
                System.IO.Path.GetDirectoryName(metadata.FullPath) ??
                throw new Exception("No source folder for: " + metadata.FullPath);
            string thumbnailPath = ExportImage(ImageParameters.Thumbnail, imageLibraryFolderPath);
            libraryManager.UpdateThumbnailCache(metadata, thumbnailPath);

            // Save Editing parameters and copy them to the current export folder
            // for convenience / inspection / debug / whatever
            string srcPath = libraryManager.SaveEditParameters(postProcess);
            string filename = Path.GetFileName(srcPath);
            string destPath = Path.Combine(subDirectoryExport, filename);
            File.Copy(srcPath, destPath, overwrite: true);

            bool hasBeenExportedToGallery = false;
            bool isFiveStars = metadata.Rating == 5;

            foreach (var imageParameters in exportParameters.Images)
            {
                string exportPath = ExportImage(imageParameters, subDirectoryExport);
                if (imageParameters.IsGalleryFormat && isFiveStars && !hasBeenExportedToGallery)
                {
                    // Dont care if fails (at least for now) 
                    _ = model.LibraryManager.AddToGallery(sourceFilePath: exportPath);
                    hasBeenExportedToGallery = true;
                }
            }

            // Must return Frame? for base class override compliance 
            return null;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            return null;
        }
    }

    internal bool NavigateToExport()
    {
        // Navigate to subdirectory for exported images
        if (string.IsNullOrWhiteSpace(this.currentDirectoryExport))
        {
            return false;
        }

        this.currentDirectoryExport.OpenInExplorer();
        return true;
    }
}
