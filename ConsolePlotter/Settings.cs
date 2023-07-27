namespace ConsolePlotter;

public class Settings
{
	public string SourceDrive { get; set; }
	public string SystemDrive { get; set; }
	public string SourceDirectory { get; set; }
	public string DestinationDirectory { get; set; }
	public int Delay { get; set; }
	public int MaxCopyThreads { get; set; }
	public long FreeSpaceSize { get; set; }
}