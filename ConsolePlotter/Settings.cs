using Newtonsoft.Json;

namespace ConsolePlotter;

public class Settings
{
	public string SourceDrive { get; set; }
	public DriveInfo[] DriverInfo { get; set; }
	public List<string> IgnoreDrives { get; set; }
	public string SourceDirectory { get; set; }
	public string DestinationDirectory { get; set; }
	public int Delay { get; set; }
	public int MaxCopyThreads { get; set; }
	public long FreeSpaceSize { get; set; }
	private bool _isInit { get; set; }

	public void Init()
	{
		_isInit = true;
		Logger.WriteLog(
		                $"Plotter\nВерсия: {new Version(2, 0, 0)}\n\n*** Поблагодарить разработчиков - кошелек XCH xch1hgl5mj53yj73q54lwhr72qd5gzskh6eu9cswj065y3y5crhw2q6q9yz0r7 ***" +
		                $"\n------------------------------------------------------------------------------------------------");

		if (File.Exists("settings.json"))
		{
			Logger.WriteLog("Найден файл settings.json, загрузка настроек");

			var loadSettings = File.ReadAllText("settings.json");
			var _settings = JsonConvert.DeserializeObject<Settings>(loadSettings);
			FreeSpaceSize = _settings.FreeSpaceSize;
			Delay = _settings.Delay;
			MaxCopyThreads = _settings.MaxCopyThreads;
			DestinationDirectory = _settings.DestinationDirectory;
			IgnoreDrives = _settings.IgnoreDrives;
			SourceDirectory = _settings.SourceDirectory;
			SourceDrive = _settings.SourceDrive;
		}
		else
		{
			Console.WriteLine("Не найден файл settings.json, укажите данные:");

			Console.WriteLine("Укажите буквы дисков через пробел, которые будут игнорироваться:");
			IgnoreDrives = Console.ReadLine().ToUpper().Split(" ")
				.Select(x => x + ":\\").ToList();

			Console.WriteLine("Укажите букву диска, с которого будет перенос файлов:");
			SourceDrive = Console.ReadLine().ToUpper() + ":\\";

			Console.WriteLine("Укажите путь к папке из которой будет производится перенос файлов:");
			SourceDirectory = Path.Combine(SourceDrive, Console.ReadLine());

			Console.WriteLine("Укажите путь к папке в которую будет производится перенос файлов:");
			DestinationDirectory = Path.Combine(Console.ReadLine());

			Console.WriteLine("Укажите целое число в секундах для повторной проверки файла:");
			Delay = int.Parse(Console.ReadLine()) * 1000;

			Console.WriteLine(
			                  "Укажите целое число для ограничения максимального количества одновременного переноса файлов:");
			MaxCopyThreads = int.Parse(Console.ReadLine());

			Console.WriteLine(
			                  "Укажите целое число в гигабайтах для минимального свободного места на диске для копирования:");
			FreeSpaceSize = long.Parse(Console.ReadLine()) * 1024 * 1024 * 1024;
			FreeSpaceSize = FreeSpaceSize;

			var serializeSettings = JsonConvert.SerializeObject(this);
			File.WriteAllText("settings.json", serializeSettings);
		}

		Run();
	}

	private void Run()
	{
		var drivers = DriveInfo.GetDrives()
			.Where(x => x.DriveType != DriveType.CDRom)
			.ToArray();

		var selectedDrive = drivers
			.Where(x => x.Name.Contains(SourceDrive))
			.FirstOrDefault();

		if (selectedDrive == null)
		{
			Logger.WriteLog($"Указанный диск {SourceDrive} не обнаружен");
			Console.ReadLine();
			return;
		}

		DriverInfo = drivers
			.Where(x => x.Name != SourceDrive && !IgnoreDrives.Contains(x.Name))
			//.Where(x => !IgnoreDrives.Contains(x.Name)) //Для дебага источник не игнорируем
			.ToArray();

		Logger.WriteLog($"Найдено {DriverInfo.Length} дисков для копирования\n");

		foreach (var driveName in DriverInfo)
			Console.Write($"\t{driveName.Name}");

		Logger.WriteLog("\n");
	}
}