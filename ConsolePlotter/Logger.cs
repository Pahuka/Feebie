namespace ConsolePlotter;

public static class Logger
{
	public static void WriteLog(string message, ConsoleColor color = ConsoleColor.DarkGray)
	{
		Console.ResetColor();
		Console.ForegroundColor = color;
		Console.WriteLine(message);
	}
}