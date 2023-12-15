using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace ConsolePlotter;

public class Consumer
{
	private readonly Producer _producer;
	private FreeSpaceChecker _checker;
	//private readonly BlockingCollection<Task> _collection;
	private Settings _settings;
	private int _totalPlot = 0;

	public Consumer()
	{
		//_collection = taskManager.Tasks;
		_checker = new FreeSpaceChecker();
		_settings = new Settings();
		_producer = new Producer(_settings.MaxCopyThreads);
	}

	public void Run()
	{
		
		Logger.WriteLog("Для запуска процесса нажмите любую клавишу");
		Console.ReadKey();

		var drivers = DriveInfo.GetDrives()
	//.Where(x => x.DriveType == DriveType.Fixed)
	.Where(x => x.DriveType != DriveType.CDRom)
	.ToArray();

		var selectedDrive = drivers
			.Where(x => x.Name.Contains(_settings.SourceDrive))
			.FirstOrDefault();

		if (selectedDrive == null)
		{
			Logger.WriteLog($"Указанный диск {_settings.SourceDrive} не обнаружен");
			Console.ReadLine();
			return;
		}

		var destinationDrivers = drivers
			//.Where(x => x.Name != _settings.SourceDrive && !_settings.IgnoreDrives.Contains(x.Name)) //TODO: не забыть раскомментировать
			.Where(x => !_settings.IgnoreDrives.Contains(x.Name))
			.ToArray();

		Logger.WriteLog($"Найдено {destinationDrivers.Length} дисков для копирования\n");

		foreach (var driveName in destinationDrivers)
			Console.Write($"\t{driveName.Name}");

		Logger.WriteLog("\n");

		while (!_producer.Tasks.IsAddingCompleted)
			try
			{
				if (Console.KeyAvailable) Console_CancelKeyPress();

				if (_producer.Tasks.Count >= _settings.MaxCopyThreads)
					Logger.WriteLog($"Достигнут лимит копирования ({_settings.MaxCopyThreads}).");

				var file = Directory.GetFiles(Path.Combine(_settings.SourceDrive, _settings.SourceDirectory), "*.plot") //TODO
					.FirstOrDefault();

				if (file == null)
				{
					Console.ResetColor();
					var logColor = _producer.Tasks.Count >= 1 ? ConsoleColor.DarkYellow : ConsoleColor.DarkGray;
					Logger.WriteLog(
						$"\n{DateTime.Now}\tНовых файлов нет\tНа данный момент копируется {_producer.Tasks.Count} файлов",
						logColor);

					Thread.Sleep(_settings.Delay);
					continue;
				}

				foreach (var drive in destinationDrivers)
					if (drive.TotalFreeSpace >= _settings.FreeSpaceSize)
					{
						var newFileDirectory = Path.Combine(drive.Name, _settings.DestinationDirectory);
						var newFilePath = Path.Combine(drive.Name, _settings.DestinationDirectory, Path.GetFileName(file));
						var tempFilePath = Path.Combine(drive.Name, _settings.DestinationDirectory,
							Path.GetFileName(file + "Copy"));

						if (Directory.Exists(newFileDirectory) && Directory.GetFiles(Path.GetDirectoryName(tempFilePath),
								"*" + Path.GetExtension(tempFilePath)).Length > 0)
						{
							if (SearchInDirectories(drive.Name, file))
								break;
							continue;
						}

						Directory.CreateDirectory(newFileDirectory);
						_producer.AddTask(MoveFile(file, tempFilePath, newFilePath), drive.Name);

						break;
					}
					else
					{
						if (SearchInDirectories(drive.Name, file))
							break;
					}

				Thread.Sleep(_settings.Delay);
			}

			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				throw;
			}

		while (!_producer.Tasks.IsCompleted)
		{
		}

		Logger.WriteLog("Все задачи завершены", ConsoleColor.Green);
		Console.ReadKey();
	}

	private bool SearchInDirectories(string driveName, string file)
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
					"*" + Path.GetExtension(tempFilePath)).Length > 0)
			{
				Logger.WriteLog($"На диск {Path.Combine(driveName, dir)} уже идет копирование, ищем другой");
				continue;
			}

			Logger.WriteLog($"Файл есть, перемещаем в {newPath}");
			Directory.CreateDirectory(newFileDirectory);
			_producer.AddTask(MoveFile(file, tempFilePath, newPath), dir);

			return true;
		}

		return false;
	}

	private async Task MoveFile(string file, string tempPath, string newFilePath)
	{
		Task.Factory.StartNew(() =>
		{
			var tempName = file + "Copy";

			File.Move(file, tempName);
			Task.Delay(10000).Wait(); //TODO: не забыть убрать в релизе
			Logger.WriteLog($"{DateTime.Now}\tКопирование {tempName}", ConsoleColor.DarkYellow);
			File.Move(tempName, tempPath);
			_totalPlot++;
			Logger.WriteLog($"{DateTime.Now}\tФайл номер {_totalPlot} перемещен {newFilePath}", ConsoleColor.Green);
			File.Move(tempPath, newFilePath);
			_producer.RemoveTask();
		});
	}

	private void Console_CancelKeyPress()
	{
		_producer.Tasks.CompleteAdding();

		var logColor = _producer.Tasks.Count >= 1 ? ConsoleColor.DarkYellow : ConsoleColor.DarkGray;
		Logger.WriteLog(
			$"Закрытие программы\nЗавершаем все задачи по переносу файлов\nКолличество задач {_producer.Tasks.Count}",
			logColor);
	}
}