////
//// Another helper thanks to a few stackexchange posts, cann't remember the link 
////
////

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace W10M_Toolbox
{
    class RegHelper
    {
        public static bool IsRegistryMounted;
        public static string rtHiveFileName;
        public static void MountRegistryHive(string hivePath, string hiveMountName)
        {
            //Find a way to detect Mass Storage //
            rtHiveFileName = Path.GetFileNameWithoutExtension(hivePath);
            string loadedRegistryHive = RegistryInterop.Load(hivePath);
            RegistryKey registry = Registry.LocalMachine.OpenSubKey(loadedRegistryHive);
            IsRegistryMounted = true;
        }
    }


    class RegistryInterop
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public int LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_PRIVILEGES
        {
            public LUID Luid;
            public int Attributes;
            public int PrivilegeCount;
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        public static extern int OpenProcessToken(int ProcessHandle, int DesiredAccess, ref int tokenhandle);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetCurrentProcess();

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        public static extern int LookupPrivilegeValue(string lpsystemname, string lpname, [MarshalAs(UnmanagedType.Struct)] ref LUID lpLuid);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        public static extern int AdjustTokenPrivileges(int tokenhandle, int disableprivs, [MarshalAs(UnmanagedType.Struct)] ref TOKEN_PRIVILEGES Newstate, int bufferlength, int PreivousState, int Returnlength);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int RegLoadKey(uint hKey, string lpSubKey, string lpFile);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern Int32 RegUnLoadKey(UInt32 hKey, string lpSubKey);
        public const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;
        public const int TOKEN_QUERY = 0x00000008;
        public const int SE_PRIVILEGE_ENABLED = 0x00000002;
        public const string SE_RESTORE_NAME = "SeRestorePrivilege";
        public const string SE_BACKUP_NAME = "SeBackupPrivilege";
        public const uint HKEY_USERS = 0x80000003;

        //temporary hive key

        public const string HIVE_SUBKEY1 = "RTSYSTEM";
        public const string HIVE_SUBKEY2 = "RTSOFTWARE";


        static private Boolean gotPrivileges = false;

        static private void GetPrivileges()
        {
            int token = 0;
            int retval = 0;
            TOKEN_PRIVILEGES tpRestore = new TOKEN_PRIVILEGES();
            TOKEN_PRIVILEGES tpBackup = new TOKEN_PRIVILEGES();
            LUID RestoreLuid = new LUID();
            LUID BackupLuid = new LUID();

            retval = LookupPrivilegeValue(null, SE_RESTORE_NAME, ref RestoreLuid);
            tpRestore.PrivilegeCount = 1;
            tpRestore.Attributes = SE_PRIVILEGE_ENABLED;
            tpRestore.Luid = RestoreLuid;

            retval = LookupPrivilegeValue(null, SE_BACKUP_NAME, ref BackupLuid);
            tpBackup.PrivilegeCount = 1;
            tpBackup.Attributes = SE_PRIVILEGE_ENABLED;
            tpBackup.Luid = BackupLuid;

            retval = OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref token);
            retval = AdjustTokenPrivileges(token, 0, ref tpRestore, 1024, 0, 0);
            retval = AdjustTokenPrivileges(token, 0, ref tpBackup, 1024, 0, 0);

            gotPrivileges = true;
        }

        static public string Load(string file)
        {
            if (Path.GetFileName(file) == "SYSTEM")
            {
                if (!gotPrivileges)
                    GetPrivileges();
               int NTERROR = RegLoadKey(HKEY_USERS, HIVE_SUBKEY1, file);
                if (NTERROR != 0)
                {
                    Debug.WriteLine("ERROR Loading SYSTEM hive.");
                }
                return HIVE_SUBKEY1;
            }
            else if (Path.GetFileName(file) == "SOFTWARE")
            {
                if (!gotPrivileges)
                    GetPrivileges();
               int NTERROR = RegLoadKey(HKEY_USERS, HIVE_SUBKEY2, file);
                if (NTERROR != 0)
                {
                    Debug.WriteLine("ERROR Loading SYSTEM hive.");
                }
                return HIVE_SUBKEY2;
            }
            else
            {
                return null;
            }
        }

        static public string Unload()
        {

            if (!gotPrivileges)
               GetPrivileges();
            int regresult1 = RegUnLoadKey(HKEY_USERS, HIVE_SUBKEY1);
           int regresult2 =  RegUnLoadKey(HKEY_USERS, HIVE_SUBKEY2);
            if (regresult1 != 0)
            {
                return "Error unloading Registry. Try again in a minute.";
            }
            else
            {
                return $"RTSYSTEM = {regresult1}   RTSOFTWARE = {regresult2}";
            }


           // System.Diagnostics.Process process = new System.Diagnostics.Process();
            //process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
            //process.StartInfo.FileName = @$"reg.exe";
            //process.StartInfo.Arguments = "unload HKEY_USERS\\RTSYSTEM";
            //process.StartInfo.UseShellExecute = false;
            //process.StartInfo.CreateNoWindow = false ;

            //process.StartInfo.RedirectStandardOutput = true;
            //process.StartInfo.RedirectStandardInput = true;
            //process.StartInfo.RedirectStandardError = true;
            //process.Start();
        }
    }
}
