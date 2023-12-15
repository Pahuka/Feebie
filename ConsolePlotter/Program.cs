using ConsolePlotter;

#region MyRegion

//var _version = new Version(2, 0, 0);
//var _checker = new FreeSpaceChecker();
//var _settings = new Settings();
//var _totalPlot = 0;

//Logger.WriteLog(
//	$"Plotter\nВерсия: {_version}\n\n*** Поблагодарить разработчиков - кошелек XCH xch1hgl5mj53yj73q54lwhr72qd5gzskh6eu9cswj065y3y5crhw2q6q9yz0r7 ***" +
//	$"\n------------------------------------------------------------------------------------------------");

//if (File.Exists("settings.json"))
//{
//	Logger.WriteLog("Найден файл settings.json, загрузка настроек");

//	var loadSettings = File.ReadAllText("settings.json");
//	_settings = JsonConvert.DeserializeObject<Settings>(loadSettings);
//	_checker.FreeSpaceSize = _settings.FreeSpaceSize;
//}
//else
//{
//	Console.WriteLine("Не найден файл settings.json, укажите данные:");

//	Console.WriteLine("Укажите буквы дисков через пробел, которые будут игнорироваться:");
//	_settings.IgnoreDrives = Console.ReadLine().ToUpper().Split(" ")
//		.Select(x => x + ":\\").ToList();

//	Console.WriteLine("Укажите букву диска, с которого будет перенос файлов:");
//	_settings.SourceDrive = Console.ReadLine().ToUpper() + ":\\";

//	Console.WriteLine("Укажите путь к папке из которой будет производится перенос файлов:");
//	_settings.SourceDirectory = Path.Combine(_settings.SourceDrive, Console.ReadLine());

//	Console.WriteLine("Укажите путь к папке в которую будет производится перенос файлов:");
//	_settings.DestinationDirectory = Path.Combine(Console.ReadLine());

//	Console.WriteLine("Укажите целое число в секундах для повторной проверки файла:");
//	_settings.Delay = int.Parse(Console.ReadLine()) * 1000;

//	Console.WriteLine("Укажите целое число для ограничения максимального количества одновременного переноса файлов:");
//	_settings.MaxCopyThreads = int.Parse(Console.ReadLine());

//	Console.WriteLine("Укажите целое число в гигабайтах для минимального свободного места на диске для копирования:");
//	_settings.FreeSpaceSize = long.Parse(Console.ReadLine()) * 1024 * 1024 * 1024;
//	_checker.FreeSpaceSize = _settings.FreeSpaceSize;

//	var serializeSettings = JsonConvert.SerializeObject(_settings);
//	File.WriteAllText("settings.json", serializeSettings);
//}

//Logger.WriteLog("Для запуска процесса нажмите любую клавишу");
//Console.ReadKey();

//var drivers = DriveInfo.GetDrives()
//	//.Where(x => x.DriveType == DriveType.Fixed)
//	.Where(x => x.DriveType != DriveType.CDRom)
//	.ToArray();

//var selectedDrive = drivers
//	.Where(x => x.Name.Contains(_settings.SourceDrive))
//	.FirstOrDefault();

//if (selectedDrive == null)
//{
//	Logger.WriteLog($"Указанный диск {_settings.SourceDrive} не обнаружен");
//	Console.ReadLine();
//	return;
//}

//var destinationDrivers = drivers
//	//.Where(x => x.Name != _settings.SourceDrive && !_settings.IgnoreDrives.Contains(x.Name))
//	.Where(x => !_settings.IgnoreDrives.Contains(x.Name))
//	.ToArray();

//Logger.WriteLog($"Найдено {destinationDrivers.Length} дисков для копирования\n");

//foreach (var driveName in destinationDrivers)
//	Console.Write($"\t{driveName.Name}");

//var taskManager = new Producer(_settings.MaxCopyThreads);
////var consumer = new Consumer(taskManager.Tasks);
////consumer.Run();

//Logger.WriteLog("\n");

//while (!taskManager.Tasks.IsAddingCompleted)
//	try
//	{
//		if (Console.KeyAvailable) Console_CancelKeyPress();

//		if (taskManager.Tasks.Count >= _settings.MaxCopyThreads)
//			Logger.WriteLog($"Достигнут лимит копирования ({_settings.MaxCopyThreads}).");

//		var file = Directory.GetFiles(Path.Combine(_settings.SourceDrive, _settings.SourceDirectory), "*.plot") //TODO
//			.FirstOrDefault();

//		if (file == null)
//		{
//			Console.ResetColor();
//			var logColor = taskManager.Tasks.Count >= 1 ? ConsoleColor.DarkYellow : ConsoleColor.DarkGray;
//			Logger.WriteLog(
//				$"\n{DateTime.Now}\tНовых файлов нет\tНа данный момент копируется {taskManager.Tasks.Count} файлов",
//				logColor);

//			Thread.Sleep(_settings.Delay);
//			continue;
//		}

//		foreach (var drive in destinationDrivers)
//			if (drive.TotalFreeSpace >= _settings.FreeSpaceSize)
//			{
//				var newFileDirectory = Path.Combine(drive.Name, _settings.DestinationDirectory);
//				var newFilePath = Path.Combine(drive.Name, _settings.DestinationDirectory, Path.GetFileName(file));
//				var tempFilePath = Path.Combine(drive.Name, _settings.DestinationDirectory,
//					Path.GetFileName(file + "Copy"));

//				if (Directory.Exists(newFileDirectory) && Directory.GetFiles(Path.GetDirectoryName(tempFilePath),
//					    "*" + Path.GetExtension(tempFilePath)).Length > 0)
//				{
//					if (SearchInDirectories(drive.Name, file))
//						break;
//					continue;
//				}

//				Directory.CreateDirectory(newFileDirectory);
//				taskManager.AddTask(MoveFile(file, tempFilePath, newFilePath), drive.Name);

//				break;
//			}
//			else
//			{
//				if (SearchInDirectories(drive.Name, file))
//					break;
//			}

//		Thread.Sleep(_settings.Delay);
//	}

//	catch (Exception e)
//	{
//		Console.WriteLine(e.Message);
//		throw;
//	}

//while (!taskManager.Tasks.IsCompleted)
//{
//}

//Logger.WriteLog("Все задачи завершены", ConsoleColor.Green);
//Console.ReadKey();

//bool SearchInDirectories(string driveName, string file)
//{
//	IEnumerable<string> dirs;

//	dirs = string.IsNullOrEmpty(_settings.DestinationDirectory)
//		? Directory.GetDirectories(driveName)
//			.Where(x => !string.IsNullOrEmpty(new DirectoryInfo(x).LinkTarget))
//		: Directory.GetDirectories(driveName)
//			.Where(x => !x.Contains(_settings.DestinationDirectory))
//			.Where(x => !string.IsNullOrEmpty(new DirectoryInfo(x).LinkTarget));

//	foreach (var dir in dirs)
//	{
//		if (!_checker.Check(new DirectoryInfo(dir).LinkTarget))
//			continue;

//		var newFileDirectory = Path.Combine(driveName, dir, _settings.DestinationDirectory);
//		var newPath = Path.Combine(driveName, dir, _settings.DestinationDirectory, Path.GetFileName(file));
//		var tempFilePath =
//			Path.Combine(driveName, dir, _settings.DestinationDirectory, Path.GetFileName(file) + "Copy");

//		if (Directory.Exists(newFileDirectory) && Directory.GetFiles(Path.GetDirectoryName(tempFilePath),
//			    "*" + Path.GetExtension(tempFilePath)).Length > 0)
//		{
//			Logger.WriteLog($"На диск {Path.Combine(driveName, dir)} уже идет копирование, ищем другой");
//			continue;
//		}

//		Logger.WriteLog($"Файл есть, перемещаем в {newPath}");
//		Directory.CreateDirectory(newFileDirectory);
//		taskManager.AddTask(MoveFile(file, tempFilePath, newPath), dir);

//		return true;
//	}

//	return false;
//}

//async Task MoveFile(string file, string tempPath, string newFilePath)
//{
//	Task.Factory.StartNew(() =>
//	{
//		var tempName = file + "Copy";

//		File.Move(file, tempName);
//		//Task.Delay(10000).Wait();
//		Logger.WriteLog($"{DateTime.Now}\tКопирование {tempName}", ConsoleColor.DarkYellow);
//		File.Move(tempName, tempPath);
//		_totalPlot++;
//		Logger.WriteLog($"{DateTime.Now}\tФайл номер {_totalPlot} перемещен {newFilePath}", ConsoleColor.Green);
//		File.Move(tempPath, newFilePath);
//		taskManager.RemoveTask();
//	});
//}

//void Console_CancelKeyPress()
//{
//	taskManager.Tasks.CompleteAdding();

//	var logColor = taskManager.Tasks.Count >= 1 ? ConsoleColor.DarkYellow : ConsoleColor.DarkGray;
//	Logger.WriteLog(
//		$"Закрытие программы\nЗавершаем все задачи по переносу файлов\nКолличество задач {taskManager.Tasks.Count}",
//		logColor);
//}

#endregion

var cancellationToken = new CancellationToken();
var consumer = new Consumer();

consumer.Run();