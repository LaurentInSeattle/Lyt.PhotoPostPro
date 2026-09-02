namespace Lyt.PhotoPostPro.Model.Library;

// To avoid namespace conflicts for 'Path' 
using System.IO;

public sealed partial class LibraryManager
{
    private bool cancelImport;

    public void BeginImport(List<string> pathList)
    {
        this.cancelImport = false;

        // Launch the Import thread 
        _ = Task.Run(async () =>
        {
            this.Import(pathList);
        });
    }

    public void CancelImport()
    {
        this.cancelImport = true;
        this.logger.Info("Import cancelled");
    }

    private async void Import(List<string> pathList)
    {
        bool completed = false;
        int errors = 0;
        int imports = 0;
        try
        {
            // Speed up this loop 
            var options = new ParallelOptions()
            {
                // Limit to 4 concurrent threads
                MaxDegreeOfParallelism = 4
            };

            Parallel.For(0, pathList.Count, options, async (index) =>
            {
                if (this.cancelImport)
                {
                    return;
                }

                string file = pathList[index];
                bool success = this.ImportFile(file);
                if (success)
                {
                    Interlocked.Increment(ref imports);
                }
                else
                {
                    Interlocked.Increment(ref errors);
                    this.logger.Warning("Download error" + file);
                }

                // Throttle so that the UI has enough time to show the thumbanil 
                await Task.Delay(40);
            });

            completed = true;
        }
        catch (Exception ex)
        {
            this.logger.Warning($" Error while importing files: {ex.Message}");
        }
        finally
        {
            new ImportCompleteMessage(completed, pathList.Count, imports, errors).Publish();
        }
    }

    private bool ImportFile(string file)
    {
        try
        {
            if (!File.Exists(file))
            {
                new ImportFileMessage(IsSuccess: false, Path: file, Message: "No Such File.").Publish();
                return false;
            }

            LoadedImage loadedImage = ImageLoader.PreLoadImage(file);
            if (loadedImage.IsSuccess && loadedImage.IsPreLoaded)
            {
                // ! Verified by loadedImage.IsPreLoaded
                new ImportFileMessage(
                    IsSuccess: true, Path: file, Message: "Success", loadedImage).Publish();
                return true;
            }

            new ImportFileMessage(IsSuccess: false, Path: file, Message: "Unknown Error").Publish();
            return false;
        }
        catch (Exception ex)
        {
            this.logger.Warning(" Import File: Exception thrown: " + ex);
            new ImportFileMessage(IsSuccess: false, Path: file, Message: "Exception thrown: " + ex).Publish();
            return false;
        }
    }
}
