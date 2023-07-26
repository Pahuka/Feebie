using ConsolePlotter;

var checker = new FreeSpaceChecker();

Console.WriteLine("Run Plotter");

var drivers = DriveInfo.GetDrives()
	//.Where(x => x.DriveType == DriveType.Fixed)
	.Where(x => x.DriveType != DriveType.CDRom)
	.ToArray();

Console.WriteLine("Укажите букву системного диска, который будет игнорироваться:");

var systemDriver = Console.ReadLine().ToUpper() + ":\\";

Console.WriteLine("Укажите букву диска, с которого будет перенос файлов:");

var driverName = Console.ReadLine().ToUpper() + ":\\";

var selectedDriver = drivers
	.Where(x => x.Name == driverName)
	.FirstOrDefault();

if (selectedDriver == null)
{
	Console.WriteLine("Указанный диск не обнаружен");
	Console.ReadLine();
	return;
}

Console.WriteLine("Укажите путь к папке из которой будет производится перенос файлов:");

var sourceDirectory = Path.Combine(selectedDriver.Name, Console.ReadLine());

Console.WriteLine("Укажите путь к папке в которую будет производится перенос файлов:");

var destDirectory = Path.Combine(Console.ReadLine());

Console.WriteLine("Укажите время задержки в секундах для повторной проверки файла:");

var pause = int.Parse(Console.ReadLine()) * 1000;

var destinationDrivers = drivers
	.Where(x => x.Name != driverName && x.Name != systemDriver)
	.ToArray();

Console.WriteLine($"Найдено {destinationDrivers.Length} дисков для копирования");

while (true)
{
	var result = false;
	Thread.Sleep(pause);

	var file = Directory.GetFiles(sourceDirectory, "*.plot").FirstOrDefault();

	if (file == null)
	{
		Console.WriteLine("Файла нет");
		continue;
	}

	foreach (var driver in destinationDrivers)
	{
		if (driver.TotalFreeSpace >= 89120571392)
		{
			var newFilePath = Path.Combine(driver.Name, destDirectory, Path.GetFileName(file));

			Console.WriteLine(
				$"Файл есть, перемещаем в {newFilePath}");

			Directory.CreateDirectory(Path.Combine(driver.Name, destDirectory));

			var tempName = file + "Copy";
			var tempPath = Path.Combine(driver.Name, destDirectory, Path.GetFileName(tempName));

			using (var task = Task.Run(()=> { File.Move(file, tempName); }))
			{
				task.Wait();
			}


			using (var task = Task.Run(() => { File.Move(tempName, tempPath); }))
			{
				task.Wait();
			}

			using (var task = Task.Run(() => { File.Move(tempPath, newFilePath); }))
			{
				task.Wait();
				result = task.IsCompletedSuccessfully;
			}
		}
		else
		{
			var dirs = Directory.GetDirectories(driver.Name)
				.Where(x => !x.Contains(destDirectory))
				.Where(x => !String.IsNullOrEmpty(new DirectoryInfo(x).LinkTarget));

			foreach (var dir in dirs)
			{
				if(!checker.Check(new DirectoryInfo(dir).LinkTarget))
					continue;

				var newPath = Path.Combine(driver.Name, dir, destDirectory, Path.GetFileName(file));

				Console.WriteLine(
					$"Файл есть, перемещаем в {newPath}");

				Directory.CreateDirectory(Path.Combine(driver.Name, destDirectory));

				var tempName = file + "Copy";
				var tempPath = Path.Combine(driver.Name, dir, destDirectory, Path.GetFileName(tempName));

				using (var task = Task.Run(() => { File.Move(file, tempName); }))
				{
					task.Wait();
				}


				using (var task = Task.Run(() => { File.Move(tempName, tempPath); }))
				{
					task.Wait();
				}

				using (var task = Task.Run(() => { File.Move(tempPath, newPath); }))
				{
					task.Wait();
					result = task.IsCompletedSuccessfully;
				}
			}
		}
	}

	if (!result)
	{
		Console.WriteLine("Файл не скопирован, не обнаружены диски со свободным объемом в 83 гигабайта");
		break;
	}
}

Console.WriteLine("Работа завершена");
Console.ReadKey();