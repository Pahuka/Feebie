using System.Threading.Channels;

namespace ConsolePlotter.Channels;

public class ChannelProducer
{
	private readonly CancellationToken _cancellationToken;

	private readonly ChannelWriter<Task> _channelWriter;
	private readonly FreeSpaceChecker _checker;
	private readonly Settings _settings;
	public readonly Channel<Task> Channel;
	private int _totalPlot;

	public ChannelProducer(int channelCount, CancellationToken cancellationToken, Settings settings)
	{
		_checker = new FreeSpaceChecker();
		_cancellationToken = cancellationToken;
		_settings = settings;
		Channel = System.Threading.Channels.Channel.CreateBounded<Task>(channelCount);
		_channelWriter = Channel.Writer;
	}

	public async Task Run()
	{
		try
		{
			while (await _channelWriter.WaitToWriteAsync(_cancellationToken))
			{
				var file = Directory
					.GetFiles(Path.Combine(_settings.SourceDrive, _settings.SourceDirectory), "*.plot")
					.FirstOrDefault();

				if (file == null)
				{
					var logColor = Channel.Reader.Count >= 1 ? ConsoleColor.DarkYellow : ConsoleColor.DarkGray;
					Logger.WriteLog(
					                $"\n{DateTime.Now}\tНовых файлов нет\tНа данный момент копируется {Channel.Reader.Count} файлов",
					                logColor);

					Thread.Sleep(_settings.Delay);
					continue;
				}

				foreach (var drive in _settings.DriverInfo)
					if (drive.TotalFreeSpace >= _settings.FreeSpaceSize)
					{
						var newFileDirectory = Path.Combine(drive.Name, _settings.DestinationDirectory);
						var newFilePath = Path.Combine(drive.Name, _settings.DestinationDirectory,
						                               Path.GetFileName(file));
						var tempFilePath = Path.Combine(drive.Name, _settings.DestinationDirectory,
						                                Path.GetFileName(file + "Copy"));

						if (Directory.Exists(newFileDirectory) && Directory.GetFiles(
						     Path.GetDirectoryName(tempFilePath),
						     "*" + Path.GetExtension(tempFilePath)).Length > 0)
						{
							if (await SearchInDirectories(drive.Name, file))
								break;
							continue;
						}

						Directory.CreateDirectory(newFileDirectory);

						var task = MoveFile(file, tempFilePath, newFilePath);

						if (_channelWriter.TryWrite(task))
							Logger.WriteLog($"Добавлена задача {task.Id}.");

						break;
					}
					else
					{
						if (await SearchInDirectories(drive.Name, file))
							break;
					}

				Thread.Sleep(_settings.Delay);
			}
		}
		catch (OperationCanceledException)
		{
			Logger.WriteLog("Формирование задач отменено", ConsoleColor.DarkRed);
			_channelWriter.Complete();
		}
		catch (Exception e)
		{
			Logger.WriteLog(e.Message);
			//throw;
		}
	}

	private async Task<bool> SearchInDirectories(string driveName, string file)
	{
		IEnumerable<string> dirs;

		dirs = string.IsNullOrEmpty(_settings.DestinationDirectory)
			? Directory.GetDirectories(driveName)
				.Where(x => !string.IsNullOrEmpty(new DirectoryInfo(x).LinkTarget))
			: Directory.GetDirectories(driveName)
				.Where(x => !x.Contains(_settings.DestinationDirectory))
				.Where(x => !string.IsNullOrEmpty(new DirectoryInfo(x).LinkTarget));

		foreach (var dir in dirs)
		{
			if (!_checker.Check(new DirectoryInfo(dir).LinkTarget))
				continue;

			var newFileDirectory = Path.Combine(driveName, dir, _settings.DestinationDirectory);
			var newPath = Path.Combine(driveName, dir, _settings.DestinationDirectory, Path.GetFileName(file));
			var tempFilePath =
				Path.Combine(driveName, dir, _settings.DestinationDirectory, Path.GetFileName(file) + "Copy");

			if (Directory.Exists(newFileDirectory) && Directory.GetFiles(Path.GetDirectoryName(tempFilePath),
			                                                             "*" + Path.GetExtension(tempFilePath)).Length >
			    0)
			{
				Logger.WriteLog($"На диск {Path.Combine(driveName, dir)} уже идет копирование, ищем другой");
				continue;
			}

			Logger.WriteLog($"Файл есть, перемещаем в {newPath}");
			Directory.CreateDirectory(newFileDirectory);

			var task = MoveFile(file, tempFilePath, newPath);

			if (_channelWriter.TryWrite(MoveFile(file, tempFilePath, newPath)))
				Logger.WriteLog($"Добавлена задача {task.Id}.");

			return true;
		}

		return false;
	}

	private Task MoveFile(string file, string tempPath, string newFilePath)
	{
		return Task.Run(() =>
		{
			var tempName = file + "Copy";

			File.Move(file, tempName);
			_totalPlot++;
			Logger.WriteLog($"{DateTime.Now}\tКопирование файла номер {_totalPlot} {tempName}",
			                ConsoleColor.DarkYellow);
			File.Move(tempName, tempPath);
			Logger.WriteLog($"{DateTime.Now}\tФайл номер {_totalPlot} перемещен {newFilePath}", ConsoleColor.Green);
			File.Move(tempPath, newFilePath);
		});
	}
}