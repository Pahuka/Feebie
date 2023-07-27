using ConsolePlotter;
using Newtonsoft.Json;

var _version = new Version(1, 0, 0);
var _checker = new FreeSpaceChecker();
var _settings = new Settings();
var _taskList = new List<Task>();

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

	Console.WriteLine("Укажите букву системного диска, который будет игнорироваться:");
	_settings.SystemDrive = Console.ReadLine().ToUpper() + ":\\";

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
	.Where(x => x.Name != _settings.SourceDrive && x.Name != _settings.SystemDrive)
	.ToArray();

Console.WriteLine($"Найдено {destinationDrivers.Length} дисков для копирования");

while (true)
	try
	{
		var result = false;
		Thread.Sleep(_settings.Delay);

		if (_taskList.Count == _settings.MaxCopyThreads)
		{
			var completeTask = await Task.WhenAny(_taskList);
			_taskList.Remove(completeTask);
		}

		var file = Directory.GetFiles(Path.Combine(_settings.SourceDrive, _settings.SourceDirectory), "*.plot") //TODO
			.FirstOrDefault();

		if (file == null)
		{
			Console.WriteLine("Файла нет");
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
					Console.WriteLine($"На диск {drive.Name} уже идет копирование, ищем другой");
					SearchInDirectories(drive.Name, file);
					continue;
				}

				Console.WriteLine($"Файл есть, перемещаем в {newFilePath}");

				Directory.CreateDirectory(newFileDirectory);

				_taskList.Add(MoveFile(file, tempFilePath, newFilePath));

				break;
			}
			else
			{
				SearchInDirectories(drive.Name, file);

				break;
			}

		//if (!result)
		//{
		//	Console.WriteLine("Файл не скопирован, не обнаружены диски со свободным объемом в 83 гигабайта");
		//	break;
		//}

		Console.WriteLine("\nНовый цикл");
	}

	catch (Exception e)
	{
		Console.WriteLine(e.Message);
		throw;
	}

Console.WriteLine("Работа завершена");
Console.ReadKey();

void SearchInDirectories(string driveName, string file)
{
	var dirs = Directory.GetDirectories(driveName)
		.Where(x => !x.Contains(_settings.DestinationDirectory))
		.Where(x => !string.IsNullOrEmpty(new DirectoryInfo(x).LinkTarget));

	foreach (var dir in dirs)
	{
		if (!_checker.Check(new DirectoryInfo(dir).LinkTarget))
			continue;

		var newFileDirectory = Path.Combine(driveName, dir, _settings.DestinationDirectory);
		var newPath = Path.Combine(driveName, dir, _settings.DestinationDirectory, Path.GetFileName(file));
		var tempFilePath = Path.Combine(driveName, dir, _settings.DestinationDirectory, Path.GetFileName(file) + "Copy");

		if (Directory.Exists(tempFilePath) && Directory.GetFiles(Path.GetDirectoryName(tempFilePath),
			    "*" + Path.GetExtension(tempFilePath)).Length > 0)
		{
			Console.WriteLine($"На диск {Path.Combine(driveName, dir)} уже идет копирование, ищем другой");
			continue;
		}

		Console.WriteLine($"Файл есть, перемещаем в {newPath}");

		Directory.CreateDirectory(newFileDirectory);

		_taskList.Add(MoveFile(file, tempFilePath, newPath));

		break;
	}
}

async Task MoveFile(string file, string tempPath, string newFilePath)
{
	var tempName = file + "Copy";

	await Task.Run(() => { File.Move(file, tempName); });
	await Task.Run(() =>
	{
		Console.WriteLine($"Копирование {tempName}");
		File.Move(tempName, tempPath);
	});
	await Task.Run(() =>
	{
		Console.WriteLine($"Переименовывание {tempPath}");
		File.Move(tempPath, newFilePath);
	});
}