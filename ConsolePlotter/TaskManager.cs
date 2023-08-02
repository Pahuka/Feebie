namespace ConsolePlotter;

public static class TaskManager
{
	public static bool IsNotStopped { get; set; } = true;
	public static List<Task> Tasks { get; set; } = new List<Task>();
}