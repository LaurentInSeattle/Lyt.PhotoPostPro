namespace Lyt.PhotoPostPro.Model.PostProcessors;

using static System.Net.Mime.MediaTypeNames;

public class ExportStep(PostProcessWorkflow postProcessWorkflow) :
    PostProcessStep(postProcessWorkflow, PostProcessStep.ExportStepName)
{
    public override void Initialize(Image<Rgb48> originalImage) { }

    // DO NOT call the base class or else it will call Transform 
    public override void Activate(WorkflowUpdateKind workflowUpdateKind) => this.Reset();

    public override Frame? Transform(bool withFrame = true) => this.Reset();

    private void Clear()
    {
    }

    public override Frame? Reset()
    {
        this.Clear();

        if (this.SourceImage is null)
        {
            return null;
        }

        // Pick the possibly rotated or mirrored from the first step as 
        // the source image so that comparisons can be made 
        // The result image from the previous step is our current source
        // so we clone it and that becomes our result image... 
        var clone = this.SourceImage.Clone();
        this.SourceImage = this.PostProcessWorkflow.Steps[0].ResultImage;
        this.ResultImage = clone;
        return this.ResultImage.ToFrame();
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

        // Create subdirectory for exported images
        string sourcePath;
        string fileName;
        string subDirectory;
        try
        {
            sourcePath = this.PostProcessWorkflow.PostProcess.SourceFilePath;
            FileInfo fi = new(sourcePath);
            string sourceDirectory = fi.DirectoryName!;
            fileName = Path.GetFileNameWithoutExtension(fi.Name);
            string ts = FileManagerModel.BriefTimestampString();
            string subDirName = fileName + "_EXP_" + ts;
            subDirectory = Path.Combine(sourceDirectory, subDirName);
            if (!Directory.Exists(subDirectory))
            {
                Directory.CreateDirectory(subDirectory);
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            if (Debugger.IsAttached) Debugger.Break();
            return null;
        }

        foreach (var imageParameters in exportParameters.Images)
        {
            try
            {
                // Resize and rescale 
                Image<Rgb48> imageToResize = this.ResultImage.Clone();
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

                // TODO: Add watermark, signature, if specified
                //

                // Add Borders, if specified
                Image<Rgb48> imageWithBorders = imageToResize;
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

                Image<Rgb48> finalImage = imageWithBorders;
                var encoder = imageParameters.ImageEncoder;
                string extension = imageParameters.FileExtension;
                string exportPath =
                    Path.Combine(subDirectory, fileName + imageParameters.PostFix + extension);
                finalImage.Save(exportPath, encoder);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                if (Debugger.IsAttached) Debugger.Break();
                return null;
            }

        }

        return null;
    }
}
