using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Management;
using System.Diagnostics;

namespace DiskManager
{
    public class WPDiskChecker
    {
        static string EFIESP_DRIVE;
        static string DATA_DRIVE;
        static string MAINOS_DRIVE;
        static string EFIESP_PHYSICAL;
        static string DATA_PHYSICAL;
        static string MAINOS_PHYSICAL;
        static string EFIESP_NAME;
        static string DATA_NAME;
        static string MAINOS_NAME;
        static string EFIESP_FILESYSTEM;
        static string DATA_FILESYSTEM;
        static string MAINOS_FILESYSTEM;

        public static string[] CheckForDiskInfo()
        {
            StringBuilder sbmain = new StringBuilder();
            sbmain.Append("");
            StringBuilder sbdata = new StringBuilder();
            sbdata.Append("");
            StringBuilder sbefi = new StringBuilder();
            sbefi.Append("");

            
            // ADD FREE SPACE AND CAPACITY //

            var driveQuery = new ManagementObjectSearcher("select * from Win32_DiskDrive");
            foreach (ManagementObject d in driveQuery.Get())
            {
                var deviceId = d.Properties["DeviceId"].Value;
                //Console.WriteLine("Device");
                //Console.WriteLine(d);
                var partitionQueryText = string.Format("associators of {{{0}}} where AssocClass = Win32_DiskDriveToDiskPartition", d.Path.RelativePath);
                var partitionQuery = new ManagementObjectSearcher(partitionQueryText);
                foreach (ManagementObject p in partitionQuery.Get())
                {
                    //Console.WriteLine("Partition");
                    //Console.WriteLine(p);
                    var logicalDriveQueryText = string.Format("associators of {{{0}}} where AssocClass = Win32_LogicalDiskToPartition", p.Path.RelativePath);
                    var logicalDriveQuery = new ManagementObjectSearcher(logicalDriveQueryText);
                    foreach (ManagementObject ld in logicalDriveQuery.Get())
                    {
                        //Console.WriteLine("Logical drive");
                        //Console.WriteLine(ld);

                        var physicalName = Convert.ToString(d.Properties["Name"].Value); // \\.\PHYSICALDRIVE2
                        var diskName = Convert.ToString(d.Properties["Caption"].Value); // WDC WD5001AALS-xxxxxx
                        var diskModel = Convert.ToString(d.Properties["Model"].Value); // WDC WD5001AALS-xxxxxx
                        var diskInterface = Convert.ToString(d.Properties["InterfaceType"].Value); // IDE
                        var capabilities = (UInt16[])d.Properties["Capabilities"].Value; // 3,4 - random access, supports writing
                        var mediaLoaded = Convert.ToBoolean(d.Properties["MediaLoaded"].Value); // bool
                        var mediaType = Convert.ToString(d.Properties["MediaType"].Value); // Fixed hard disk media
                        var mediaSignature = Convert.ToUInt32(d.Properties["Signature"].Value); // int32
                        var mediaStatus = Convert.ToString(d.Properties["Status"].Value); // OK

                        var driveName = Convert.ToString(ld.Properties["Name"].Value); // C:
                        var driveId = Convert.ToString(ld.Properties["DeviceId"].Value); // C:
                        var driveCompressed = Convert.ToBoolean(ld.Properties["Compressed"].Value);
                        var driveType = Convert.ToUInt32(ld.Properties["DriveType"].Value); // C: - 3
                        var fileSystem = Convert.ToString(ld.Properties["FileSystem"].Value); // NTFS
                        var freeSpace = Convert.ToUInt64(ld.Properties["FreeSpace"].Value); // in bytes
                        var totalSpace = Convert.ToUInt64(ld.Properties["Size"].Value); // in bytes
                        var driveMediaType = Convert.ToUInt32(ld.Properties["MediaType"].Value); // c: 12
                        var volumeName = Convert.ToString(ld.Properties["VolumeName"].Value); // System
                        var volumeSerial = Convert.ToString(ld.Properties["VolumeSerialNumber"].Value); // 12345678
                        if (volumeName == "EFIESP")
                        {
                            Debug.WriteLine($"EFIESP: {volumeName}");
                            EFIESP_NAME = volumeName;
                            Debug.WriteLine($"{physicalName}");
                            EFIESP_PHYSICAL = physicalName;
                            Debug.WriteLine($"{driveName}");
                            EFIESP_DRIVE = driveName;
                            Debug.WriteLine($"{fileSystem}");
                            EFIESP_FILESYSTEM = fileSystem;
                            Debug.WriteLine(new string('-', 79));
                        }
                        if (volumeName == "Data")
                        {
                            Debug.WriteLine($"Data: {volumeName}");
                            DATA_NAME = volumeName;
                            Debug.WriteLine($"{physicalName}");
                            DATA_PHYSICAL = physicalName;
                            Debug.WriteLine($"{driveName}");
                            DATA_DRIVE = driveName;
                            Debug.WriteLine($"{fileSystem}");
                            DATA_FILESYSTEM = fileSystem;
                            Debug.WriteLine(new string('-', 79));
                        }
                        if (volumeName == "MainOS")
                        {
                            Debug.WriteLine($"MainOS: {volumeName}");
                            MAINOS_NAME = volumeName;
                            Debug.WriteLine($"{physicalName}");
                            MAINOS_PHYSICAL = physicalName;
                            Debug.WriteLine($"{driveName}");
                            MAINOS_DRIVE = driveName;
                            Debug.WriteLine($"{fileSystem}");
                            MAINOS_FILESYSTEM = fileSystem;
                            Debug.WriteLine(new string('-', 79));
                        }
                    }
                }
            }

            List<string> noresult = null;
            if (EFIESP_NAME != "EFIESP")
            {
                noresult.Add("EFIESP NOT FOUND ABORTING");
                return noresult.ToArray();
                
            }
            if (DATA_NAME != "DATA")
            {
                noresult.Add("DATA NOT FOUND ABORTING");
                return noresult.ToArray();
            }
            if (MAINOS_NAME != "MainOS")
            {
                noresult.Add("MAINOS NOT FOUND ABORTING");
                return noresult.ToArray();
            }

            string[] result =
            {
                EFIESP_PHYSICAL,
                EFIESP_NAME,
                EFIESP_DRIVE,
                EFIESP_FILESYSTEM,
                DATA_PHYSICAL,
                DATA_NAME,
                DATA_DRIVE,
                DATA_FILESYSTEM,
                MAINOS_PHYSICAL,
                MAINOS_NAME,
                MAINOS_DRIVE,
                MAINOS_FILESYSTEM
            };


            return result;



        }
    }
}
