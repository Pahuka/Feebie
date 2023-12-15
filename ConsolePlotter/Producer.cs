using System.Collections.Concurrent;

namespace ConsolePlotter;

public class Producer
{
	public Producer(int taskCapacity)
	{
		Tasks = new BlockingCollection<Task>(taskCapacity);
	}

	public BlockingCollection<Task> Tasks { get; }

	public void RemoveTask()
	{
		try
		{
			var task = Tasks.Take();
			Logger.WriteLog($"Удаляем завершенную задачу {task.Id}. Осталось {Tasks.Count} задач.");
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
	}

	public void AddTask(Task task, string dir)
	{
		if(!Tasks.IsAddingCompleted)
		{
			Tasks.Add(task);
			Logger.WriteLog($"Добавляем новую задачу {task.Id} для диска {dir}", ConsoleColor.DarkMagenta);
		}
	}
}