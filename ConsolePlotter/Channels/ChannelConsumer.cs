using System.Collections.Concurrent;
using System.Threading.Channels;

namespace ConsolePlotter.Channels;

public class ChannelConsumer
{
	private readonly CancellationToken _cancellationToken;
	private readonly ChannelReader<Task> _reader;

	public ChannelConsumer(ChannelReader<Task> reader, CancellationToken cancellationToken,
		BlockingCollection<Task> collection)
	{
		_reader = reader;
		_cancellationToken = cancellationToken;
		Tasks = collection;
	}

	public BlockingCollection<Task> Tasks { get; set; }

	public async Task Run()
	{
		try
		{
			while (await _reader.WaitToReadAsync(_cancellationToken))
				if (_reader.TryRead(out var task))
				{
					Tasks.Add(task);
					await task;
					Logger.WriteLog($"Задача {task.Id} завершена.");
				}
		}
		catch (OperationCanceledException e)
		{
			//Logger.WriteLog(e.Message);
			Tasks.CompleteAdding();
			//throw;
		}
		catch (Exception e)
		{
			Logger.WriteLog(e.Message);
			//throw;
		}
	}
}