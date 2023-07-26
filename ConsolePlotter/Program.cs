using ConsolePlotter;
using Newtonsoft.Json;

var checker = new FreeSpaceChecker();
var settings = new Settings();

Console.WriteLine("Run Plotter\n*** Поблагодарить разработчиков - кошелек XCH xch13zeze330fl05w4qq8rat3fd74h33x87p46tnmert88qjka0yjftsp97ell ***");

if (File.Exists("settings.json"))
{
	Console.WriteLine("Найден файл settings.json, загрузка настроек");

	var loadSettings = File.ReadAllText("settings.json");
	settings = JsonConvert.DeserializeObject<Settings>(loadSettings);
}
else
{
	Console.WriteLine("Укажите букву системного диска, который будет игнорироваться:");
	settings.SystemDrive = Console.ReadLine().ToUpper() + ":\\";

	Console.WriteLine("Укажите букву диска, с которого будет перенос файлов:");
	settings.SourceDrive = Console.ReadLine().ToUpper() + ":\\";

	Console.WriteLine("Укажите путь к папке из которой будет производится перенос файлов:");
	settings.SourceDirectory = Path.Combine(settings.SourceDrive, Console.ReadLine());

	Console.WriteLine("Укажите путь к папке в которую будет производится перенос файлов:");
	settings.DestinationDirectory = Path.Combine(Console.ReadLine());

	Console.WriteLine("Укажите время задержки в секундах для повторной проверки файла:");
	settings.Delay = int.Parse(Console.ReadLine()) * 1000;

	var serializeSettings = JsonConvert.SerializeObject(settings);
	File.WriteAllText("settings.json", serializeSettings);
}

var drivers = DriveInfo.GetDrives()
	//.Where(x => x.DriveType == DriveType.Fixed)
	.Where(x => x.DriveType != DriveType.CDRom)
	.ToArray();

var selectedDriver = drivers
	.Where(x => x.Name.Contains(settings.SourceDrive))
	.FirstOrDefault();

if (selectedDriver == null)
{
	Console.WriteLine("Указанный диск не обнаружен");
	Console.ReadLine();
	return;
}

var destinationDrivers = drivers
	.Where(x => x.Name != settings.SourceDrive && x.Name != settings.SystemDrive)
	.ToArray();

Console.WriteLine($"Найдено {destinationDrivers.Length} дисков для копирования");

while (true)
	try
	{
		var result = false;
		Thread.Sleep(settings.Delay);

		var file = Directory.GetFiles(Path.Combine(settings.SourceDrive, settings.SourceDirectory), "*.plot").FirstOrDefault();

		if (file == null)
		{
			Console.WriteLine("Файла нет");
			continue;
		}

		foreach (var driver in destinationDrivers)
			if (driver.TotalFreeSpace >= 89120571392)
			{
				var newFilePath = Path.Combine(driver.Name, settings.DestinationDirectory, Path.GetFileName(file));

				Console.WriteLine(
					$"Файл есть, перемещаем в {newFilePath}");

				Directory.CreateDirectory(Path.Combine(driver.Name, settings.DestinationDirectory));

				var tempName = file + "Copy";
				var tempPath = Path.Combine(driver.Name, settings.DestinationDirectory, Path.GetFileName(tempName));

				if (!File.Exists(file))
				{
					Console.WriteLine($"Не нашел файл: {file}");
					continue;
				}

				var task = Task.Run(() => { File.Move(file, tempName); });
				task.Wait();

				task = Task.Run(() =>
				{
					File.Move(tempName, tempPath);
				});

				task.Wait();

				task = Task.Run(() => { File.Move(tempPath, newFilePath); });
				task.Wait();
				result = task.IsCompletedSuccessfully;
			}
			else
			{
				var dirs = Directory.GetDirectories(driver.Name)
					.Where(x => !x.Contains(settings.DestinationDirectory))
					.Where(x => !string.IsNullOrEmpty(new DirectoryInfo(x).LinkTarget));

				foreach (var dir in dirs)
				{
					if (!checker.Check(new DirectoryInfo(dir).LinkTarget))
						continue;

					if (!File.Exists(file))
					{
						Console.WriteLine($"Не нашел файл: {file}");
						continue;
					}

					var newPath = Path.Combine(driver.Name, dir, settings.DestinationDirectory, Path.GetFileName(file));

					Console.WriteLine(
						$"Файл есть, перемещаем в {newPath}");

					Directory.CreateDirectory(Path.Combine(driver.Name, settings.DestinationDirectory));

					var tempName = file + "Copy";
					var tempPath = Path.Combine(driver.Name, dir, settings.DestinationDirectory, Path.GetFileName(tempName));

					var task = Task.Run(() => { File.Move(file, tempName); });
					task.Wait();

					task = Task.Run(() =>
					{
						File.Move(tempName, tempPath);
					});

					task.Wait();

					task = Task.Run(() => { File.Move(tempPath, newPath); });
					task.Wait();
					result = task.IsCompletedSuccessfully;
				}
			}

		if (!result)
		{
			Console.WriteLine("Файл не скопирован, не обнаружены диски со свободным объемом в 83 гигабайта");
			break;
		}
	}

	catch (Exception e)
	{
		Console.WriteLine(e.Message);
		throw;
	}

Console.WriteLine("Работа завершена");
Console.ReadKey();