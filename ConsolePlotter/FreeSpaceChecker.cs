using System.Management;

namespace ConsolePlotter;

public class FreeSpaceChecker
{
	private long _freeSpace = 891205713;

	public bool Check(string deviceId)
	{
		var namespaceScope = new ManagementScope(@"\\.\ROOT\CIMV2");
		var diskQuery = new ObjectQuery("SELECT * FROM Win32_MountPoint");
		var mgmtObjSearcher = new ManagementObjectSearcher(namespaceScope, diskQuery);
		var mountPoints = mgmtObjSearcher.Get();
		var result = false;

		foreach (var mp in mountPoints)
		{
			//Получаем доступ к объекту Win32_Directory (папка к которой примонтирован диск)
			//var dirObj = new ManagementObject(mp.Properties["Directory"].Value as string);
			//Получаем доступ к объекту Win32_Volume (раздел диска который примонтирован к папке)
			var volumeObj = new ManagementObject(mp.Properties["Volume"].Value as string);

			if (volumeObj.Path.Path.Contains(deviceId))
			{
				var currentSpace = long.Parse(volumeObj["FreeSpace"].ToString());
				result = currentSpace >= _freeSpace;
				break;
			}
		}

		return result;
	}
}