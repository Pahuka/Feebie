using System.Management;

namespace ConsolePlotter;

public class FreeSpaceChecker
{
	public long FreeSpaceSize { get; set; }

	public bool Check(string deviceId)
	{
		var namespaceScope = new ManagementScope(@"\\.\ROOT\CIMV2");
		var diskQuery = new ObjectQuery("SELECT * FROM Win32_Volume");
		//var diskQuery = new ObjectQuery("SELECT * FROM Win32_MountPoint");
		//var disk = new ObjectQuery("SELECT * FROM Win32_DiskDrive");
		//var diskObjSearcher = new ManagementObjectSearcher(namespaceScope, disk);
		//var disks = diskObjSearcher.Get();
		var mgmtObjSearcher = new ManagementObjectSearcher(namespaceScope, diskQuery);
		var mountPoints = mgmtObjSearcher.Get();
		var result = false;

		foreach (ManagementObject mp in mountPoints)
			//Получаем доступ к объекту Win32_Directory (папка к которой примонтирован диск)
			//var dirObj = new ManagementObject(mp.Properties["Directory"].Value as string);
			//Получаем доступ к объекту Win32_Volume (раздел диска который примонтирован к папке)
			//var volumeObj = new ManagementObject(mp.Properties["Volume"].Value as string);
			if (mp.Path.Path.Contains(deviceId))
			{
				var currentSpace = long.Parse(mp["FreeSpace"].ToString());
				result = currentSpace >= FreeSpaceSize;
				break;
			}

		return result;
	}
}