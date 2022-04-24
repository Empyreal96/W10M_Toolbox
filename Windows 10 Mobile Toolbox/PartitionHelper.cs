using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace W10M_Toolbox
{
    class PartitionHelper
    {
        class USBDrive
        {
            public string mediatype { get; set; }
            public string interfacetype { get; set; }
            public Int64 size { get; set; }
            public string name { get; set; }
            public string model { get; set; }
            public Int32 index { get; set; }
        }
        internal static class Dump
        {
            const int FILE_ATTRIBUTE_SYSTEM = 0x4;
            const int FILE_FLAG_SEQUENTIAL_SCAN = 0x8;
            private static readonly string progloc = System.Reflection.Assembly.GetEntryAssembly()?.Location;

            [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            private static extern SafeFileHandle CreateFile(string fileName,
                [MarshalAs(UnmanagedType.U4)] FileAccess fileAccess, [MarshalAs(UnmanagedType.U4)] FileShare fileShare,
                IntPtr securityAttributes, [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition, int flags, IntPtr template);

            private static void create_imagefile(string srcDrive, string destFile, long bytes)
            {
                using (SafeFileHandle device = CreateFile(srcDrive,
                    FileAccess.Read, FileShare.Write | FileShare.Read | FileShare.Delete, IntPtr.Zero, FileMode.Open,
                    FILE_ATTRIBUTE_SYSTEM | FILE_FLAG_SEQUENTIAL_SCAN, IntPtr.Zero))
                {
                    if (device.IsInvalid)
                    {
                        throw new IOException("Unable to access drive. Win32 Error Code " + Marshal.GetLastWin32Error());
                    }
                    using (FileStream dest = File.Open(destFile, FileMode.Create))
                    {
                        using (FileStream src = new FileStream(device, FileAccess.Read))
                        {
                            //src.CopyTo(dest);
                            CopyFileStream(src, dest, bytes);
                        }
                    }
                }
            }

            private static void CopyFileStream(FileStream input, FileStream output, long bytes)
            {
                byte[] buffer = new byte[81920];
                int read;
                long bytescount = 0;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, read);
                    bytescount += read;
                    //drawTextProgressBar(bytescount, bytes);
                    if (bytescount >= bytes) break;
                }
            }


            private static string sizesuf(long value, string format, bool displayformat)
            {
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                double len = value;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    if (format == sizes[order]) break;
                    order++;
                    len = len / 1024;
                }
                return displayformat ? $"{Math.Floor(len):0.#}{sizes[order]}" : $"{Math.Floor(len):0.#}";
            }

            private static List<USBDrive> nuDetectUSB()
            {
                ConnectionOptions connOptions = new ConnectionOptions
                {
                    Impersonation = ImpersonationLevel.Impersonate,
                    Authentication = AuthenticationLevel.PacketPrivacy,
                    EnablePrivileges = true
                };
                ManagementScope scope = new ManagementScope("root\\CIMV2", connOptions);
                scope.Connect();
                ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_DiskDrive");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);

                var results = new List<USBDrive>();
                foreach (ManagementBaseObject queryObj in searcher.Get())
                {
                    var res = new USBDrive
                    {
                        mediatype = (string)queryObj["MediaType"],
                        index = Int32.Parse(queryObj["Index"].ToString()),
                        interfacetype = (string)queryObj["InterfaceType"],
                        model = (string)queryObj["Model"],
                        name = (string)queryObj["Name"],
                        size = Int64.Parse(queryObj["Size"].ToString())
                    };
                    if (res.mediatype == "Removable Media" && res.interfacetype == "USB")
                    {
                        results.Add(res);
                    }
                }

                return results;
            }

            public static string verifyFile(string file)
            {
                string newfile = Path.Combine(Path.GetDirectoryName(progloc), file);
                if (!File.Exists(newfile)) return newfile;
                return String.Empty;
            }

            private static string GetPhysicalDevicePath(string DriveLetter)
            {
                char DrvLetter = DriveLetter.Substring(0, 1).ToCharArray()[0];
                ManagementClass devs = new ManagementClass(@"Win32_Diskdrive");
                {
                    ManagementObjectCollection moc = devs.GetInstances();
                    foreach (ManagementObject mo in moc)
                    {
                        foreach (ManagementObject b in mo.GetRelated("Win32_DiskPartition"))
                        {
                            foreach (ManagementBaseObject c in b.GetRelated("Win32_LogicalDisk"))
                            {
                                string DevName = string.Format("{0}", c["Name"]);
                                if (DevName[0] == DrvLetter) return string.Format("{0}", mo["DeviceId"]);
                            }
                        }
                    }
                }
                return "";
            }
        }
    }
}
