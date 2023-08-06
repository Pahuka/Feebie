using System.Runtime.CompilerServices;
using ConsolePlotter;
using Newtonsoft.Json;

var _version = new Version(1, 7, 0);
var _checker = new FreeSpaceChecker();
var _settings = new Settings();
var _totalPlot = 0;

Console.WriteLine(
	$"Plotter\nВерсия: {_version}\n\n*** Поблагодарить разработчиков - кошелек XCH xch1hgl5mj53yj73q54lwhr72qd5gzskh6eu9cswj065y3y5crhw2q6q9yz0r7 ***");
Console.WriteLine("\n------------------------------------------------------------------------------------------------");

if (File.Exists("settings.json"))
{
	Console.WriteLine("Найден файл settings.json, загрузка настроек");

	var loadSettings = File.ReadAllText("settings.json");
	_settings = JsonConvert.DeserializeObject<Settings>(loadSettings);
	_checker.FreeSpaceSize = _settings.FreeSpaceSize;
}
else
{
	Console.WriteLine("Не найден файл settings.json, укажите данные:");

	Console.WriteLine("Укажите буквы дисков через пробел, которые будут игнорироваться:");
	_settings.IgnoreDrives = Console.ReadLine().ToUpper().Split(" ")
		.Select(x=> x + ":\\").ToList();

	Console.WriteLine("Укажите букву диска, с которого будет перенос файлов:");
	_settings.SourceDrive = Console.ReadLine().ToUpper() + ":\\";

	Console.WriteLine("Укажите путь к папке из которой будет производится перенос файлов:");
	_settings.SourceDirectory = Path.Combine(_settings.SourceDrive, Console.ReadLine());

	Console.WriteLine("Укажите путь к папке в которую будет производится перенос файлов:");
	_settings.DestinationDirectory = Path.Combine(Console.ReadLine());

	Console.WriteLine("Укажите целое число в секундах для повторной проверки файла:");
	_settings.Delay = int.Parse(Console.ReadLine()) * 1000;

	Console.WriteLine("Укажите целое число для ограничения максимального количества одновременного переноса файлов:");
	_settings.MaxCopyThreads = int.Parse(Console.ReadLine());

	Console.WriteLine("Укажите целое число в гигабайтах для минимального свободного места на диске для копирования:");
	_settings.FreeSpaceSize = long.Parse(Console.ReadLine()) * 1024 * 1024 * 1024;
	_checker.FreeSpaceSize = _settings.FreeSpaceSize;

	var serializeSettings = JsonConvert.SerializeObject(_settings);
	File.WriteAllText("settings.json", serializeSettings);
}

Console.WriteLine("Для запуска процесса нажмите любую клавишу");
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
	Console.WriteLine($"Указанный диск {_settings.SourceDrive} не обнаружен");
	Console.ReadLine();
	return;
}

var destinationDrivers = drivers
	.Where(x => x.Name != _settings.SourceDrive && !_settings.IgnoreDrives.Contains(x.Name))
	.ToArray();

Console.WriteLine($"Найдено {destinationDrivers.Length} дисков для копирования\n");

foreach (var driveName in destinationDrivers)
{
	Console.Write($"\t{driveName.Name}");
}

Console.WriteLine("\n");
Console.CancelKeyPress += Console_CancelKeyPress;

while (TaskManager.IsNotStopped)
	try
	{
		var result = false;

		if (TaskManager.Tasks.Count == _settings.MaxCopyThreads)
		{
			var completeTask = await Task.WhenAny(TaskManager.Tasks);
			TaskManager.Tasks.Remove(completeTask);
		}

		var file = Directory.GetFiles(Path.Combine(_settings.SourceDrive, _settings.SourceDirectory), "*.plot") //TODO
			.FirstOrDefault();

		if (file == null)
		{
			Console.ForegroundColor = TaskManager.Tasks.Count >= 1 ? ConsoleColor.DarkYellow : ConsoleColor.DarkGray;
			Console.WriteLine(
				$"\n{DateTime.Now}\tНовых файлов нет\tНа данный момент копируется {TaskManager.Tasks.Count} файлов");

			if (TaskManager.Tasks.Count > 0)
			{
				var completeTask = await Task.WhenAny(TaskManager.Tasks);
				TaskManager.Tasks.Remove(completeTask);
			}

			Thread.Sleep(_settings.Delay);
			continue;
		}

		foreach (var drive in destinationDrivers)
		{
			//Console.ForegroundColor = ConsoleColor.Cyan;
			//Console.WriteLine($"Min free space drive {_settings.FreeSpaceSize}");
			//Console.WriteLine($"{drive.Name} : {drive.TotalFreeSpace} - {drive.TotalFreeSpace >= _settings.FreeSpaceSize}");

			if (drive.TotalFreeSpace >= _settings.FreeSpaceSize)
			{
				var newFileDirectory = Path.Combine(drive.Name, _settings.DestinationDirectory);
				var newFilePath = Path.Combine(drive.Name, _settings.DestinationDirectory, Path.GetFileName(file));
				var tempFilePath = Path.Combine(drive.Name, _settings.DestinationDirectory,
					Path.GetFileName(file + "Copy"));

				if (Directory.Exists(newFileDirectory) && Directory.GetFiles(Path.GetDirectoryName(tempFilePath),
					    "*" + Path.GetExtension(tempFilePath)).Length > 0)
				{
					//Console.WriteLine($"На диск {drive.Name} уже идет копирование, ищем другой");
					if (SearchInDirectories(drive.Name, file))
						break;
					else
						continue;
				}

				//Console.WriteLine($"Файл есть, перемещаем в {newFilePath}");

				Directory.CreateDirectory(newFileDirectory);

				TaskManager.Tasks.Add(MoveFile(file, tempFilePath, newFilePath));
				//Console.ForegroundColor = ConsoleColor.DarkMagenta;
				//Console.WriteLine($"Добавляем новую задачу номер {TaskManager.Tasks.Count} для диска {drive.Name}");

				break;
			}
			else
			{
				if(SearchInDirectories(drive.Name, file))
					break;
				else
					continue;
			}
		}

		Thread.Sleep(_settings.Delay);
	}

	catch (Exception e)
	{
		Console.WriteLine(e.Message);
		throw;
	}

//Console.WriteLine($"{DateTime.Now}\tРабота завершена");
Console.ReadKey();

bool SearchInDirectories(string driveName, string file)
{
	IEnumerable<string> dirs;

	dirs = String.IsNullOrEmpty(_settings.DestinationDirectory) ?
		Directory.GetDirectories(driveName)
			.Where(x => !string.IsNullOrEmpty(new DirectoryInfo(x).LinkTarget)) :
		Directory.GetDirectories(driveName)
		.Where(x => !x.Contains(_settings.DestinationDirectory))
		.Where(x => !string.IsNullOrEmpty(new DirectoryInfo(x).LinkTarget)); //TODO надо предусмотреть ситуацию, когда не указывают папку куда копировать

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
			//Console.WriteLine($"На диск {Path.Combine(driveName, dir)} уже идет копирование, ищем другой");
			continue;

		//Console.WriteLine($"Файл есть, перемещаем в {newPath}");

		Directory.CreateDirectory(newFileDirectory);

		TaskManager.Tasks.Add(MoveFile(file, tempFilePath, newPath));
		//Console.ForegroundColor = ConsoleColor.DarkMagenta;
		//Console.WriteLine($"Добавляем новую задачу номер {TaskManager.Tasks.Count} для диска {dir}");

		return true;
	}

	return false;
}

async Task MoveFile(string file, string tempPath, string newFilePath)
{
	var tempName = file + "Copy";

	await Task.Run(() => { File.Move(file, tempName); });
	await Task.Run(() =>
	{
		Console.ForegroundColor = ConsoleColor.DarkYellow;
		Console.WriteLine($"{DateTime.Now}\tКопирование {tempName}");
		File.Move(tempName, tempPath);
	});
	await Task.Run(() =>
	{
		Console.ForegroundColor = ConsoleColor.Green;
		_totalPlot++;
		Console.WriteLine($"{DateTime.Now}\tФайл номер {_totalPlot} перемещен {newFilePath}");
		File.Move(tempPath, newFilePath);
	});
}

static void Console_CancelKeyPress(object sender, EventArgs e)
{
	TaskManager.IsNotStopped = false;
	Console.ForegroundColor = TaskManager.Tasks.Count >= 1 ? ConsoleColor.DarkYellow : ConsoleColor.DarkGray;
	Console.WriteLine($"Закрытие программы\nЗавершаем все задачи по переносу файлов\nКолличество задач {TaskManager.Tasks.Count}");
	Task.WaitAll(TaskManager.Tasks.ToArray());
	Console.ForegroundColor = ConsoleColor.Green;
	Console.WriteLine("Все задачи завершены");
	Console.ReadKey();
}