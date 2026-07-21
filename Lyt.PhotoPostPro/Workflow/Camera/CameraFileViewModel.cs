namespace Lyt.PhotoPostPro.Workflow.Camera;

using System.IO; 

public sealed partial class CameraFileViewModel : ViewModel<CameraFileView> 
{
	private readonly CameraViewModel parent;
	private readonly string path;	// file on disk 
	private readonly string file;	// file on camera 

	[ObservableProperty]
	public partial string Title { get; set; }

	[ObservableProperty]
	public partial string Details { get; set; }

	[ObservableProperty]
	public partial string Glyph { get; set; }

	/// <summary>  Creates a thumbnail view model </summary>
	public CameraFileViewModel(CameraViewModel parent, DeviceFileDownloadedMessage message)
	{
		this.parent = parent;
		this.path = message.Path;
		this.file = message.File;
		this.Glyph = this.file.EndsWith(".MOV", StringComparison.InvariantCultureIgnoreCase) ? "movies_and_tv" : "document"; 
		FileInfo fi = new(this.path);
		const float megaBytes = 1024.0f * 1024.0f;
		float sizeMB = fi.Length / megaBytes;
		this.Details = string.Format("{0:F2} MB", sizeMB);
		this.Title = this.file;
	}

	[RelayCommand]
	public void OnSave()
	{
		try
		{
			// Move file at path to desktop 
			string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
			string targetFilename = Path.GetFileName(this.path);
			string targetPath = Path.Combine(desktopPath, targetFilename);
			File.Move(this.path, targetPath, overwrite: true);
		}
		catch (Exception ex)
		{ 
			Debug.WriteLine(ex);
			// TODO : Message user 
		}
	}

	[RelayCommand]
	public void OnDelete() =>
		// Invoke parent to delete file on camera and to have this VM removed from the list
		this.parent.RemoveFromCamera(this, this.file);
}
