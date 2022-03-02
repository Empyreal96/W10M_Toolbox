using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace W10M_Toolbox.DeviceHelper
{
    class DeviceHelper
    {
        public static string wpifilename;
        public static bool IsDeviceConnected { get; set; }
        public static bool IsDeviceLogsPresent { get; set; }
        public const int SC_MINIMIZE = 6;
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        public static string CheckWPIPath()
        {
            if (System.Environment.Is64BitOperatingSystem == true)
            {

                
                
                if (File.Exists(@$".\AppData\bin\wpinternals\win-x64\wpinternals.exe") == true)
                {
                    wpifilename = @$".\AppData\bin\wpinternals\win-x64\wpinternals.exe";
                }
                return wpifilename;
            }
            else
            {

                
                if (File.Exists(@$".\AppData\bin\wpinternals\win-x86\wpinternals.exe") == true)
                {
                    wpifilename = @$".\AppData\bin\wpinternals\win-x86\wpinternals.exe";
                }
                return wpifilename;
            }

        }
        /// <summary>
        /// Reboot the connected Windows Phone device to Mass Storage
        /// </summary>
        public static string RebootToMassStorage(string wpiPath)
        {
            if (File.Exists(CheckWPIPath()) == true)
            {
                IsDeviceConnected = false;
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
                process.StartInfo.FileName = wpiPath;
                process.StartInfo.Arguments = @"-SwitchToMassStorageMode";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = false;

                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();
                while (string.IsNullOrEmpty(process.MainWindowTitle))
                {
                    Thread.Sleep(50);
                    process.Refresh();
                }
                ShowWindow(process.MainWindowHandle, SC_MINIMIZE);
                string a = process.StandardOutput.ReadToEnd();
                process.WaitForExit(60000);
                process.Close();
                process.Dispose();
                IsDeviceConnected = true;
                return a;
            }
            else
            {
                return "WPInternals could not be found! Make sure to check the Assets page";
            }
        }

        /// <summary>
        /// Reboot the connected Windows Phone to Flash Mode
        /// </summary>
        public static string RebootToFlash(string wpiPath)
        {
            if (File.Exists(CheckWPIPath()) == true)
            {
                IsDeviceConnected = false;
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
                process.StartInfo.FileName = wpiPath;
                process.StartInfo.Arguments = @"-ShowPhoneInfo";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = false;

                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();
                while (string.IsNullOrEmpty(process.MainWindowTitle))
                {
                    Thread.Sleep(50);
                    process.Refresh();
                }
                ShowWindow(process.MainWindowHandle, SC_MINIMIZE);
                string a = process.StandardOutput.ReadToEnd();
                process.WaitForExit(60000);
                process.Close();
                process.Dispose();
                IsDeviceConnected = true;
                return a;
            }
            else
            {
                return "WPInternals could not be found! Make sure to check the Assets page";
            }
        }

        public static void RebootToNormal()
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
            process.StartInfo.FileName = @$".\AppData\bin\thor2\thor2.exe";
            process.StartInfo.Arguments = @"-mode rnd -reboot";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = false;

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            while (string.IsNullOrEmpty(process.MainWindowTitle))
            {
                Thread.Sleep(50);
                process.Refresh();
            }
            ShowWindow(process.MainWindowHandle, SC_MINIMIZE);
            string a = process.StandardOutput.ReadToEnd();
            process.WaitForExit(60000);
            process.Close();
            process.Dispose();
        }


        private static string result;
        /// <summary>
        /// Fetch connected Device info
        /// </summary>
        public static string GetDeviceInfo()
        {
            IsDeviceConnected = false;
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            process.StartInfo.FileName = @".\AppData\bin\iutool\iutool.exe";
            process.StartInfo.Arguments = @"-l";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.Start();
            string q = process.StandardOutput.ReadToEnd();
            if (q.Contains("Command executed successfully."))
            {
                if (!q.Contains("Serial:"))
                {
                    IsDeviceConnected = false;
                   q = "Connect a Device in 'Normal Mode' to begin.\n\n" +
                "- If your device is already in Flash Mode, Click Flash Mode to view details or Mass Storage to reboot\n" +
                "- If your device is in Normal Mode, Click Flash Mode or Mass Storage to reboot";
                } else
                {
                    IsDeviceConnected = true;
                }
                
                result = q.Replace("Command executed successfully.", "");
                
            }
            else
            {
                result = q;
            }
           /* if (q.Contains("No Device Found"))
            {
                result = RebootToFlash(CheckWPIPath());
            } */
            return result;
            
        }


        private static TextBlock IUOutputBox;
        private static Process IUProcess { get; set; }
        public static void PushUpdateCabs(string CabPath, TextBlock OutputBox)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            
            IUOutputBox = OutputBox;
            OutputBox.Text = $"\"{CabPath}\"";
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            process.StartInfo.FileName = @".\AppData\bin\iutool\iutool.exe";
            process.StartInfo.Arguments = $"-V -p \"{CabPath}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.OutputDataReceived += Process_OutputDataReceived;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            IUProcess = process;
            process.Start();
            process.BeginOutputReadLine();
           // string q = process.StandardOutput.ReadToEnd();
        }

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Task.Run(() =>
            {
                IUOutputBox.Text += $"{e.Data}\n";
            });
        }

        public static string GetInstalledPackages()
        {
            
            if (File.Exists(@".\AppData\Temp\DeviceLog\InstalledPackages.csv") == true)
            {
                string Logs = File.ReadAllText(@".\AppData\Temp\DeviceLog\InstalledPackages.csv");
                return Logs;
            }
            else
            {
                try
                {
                    GetDeviceLogs();
                    string Logs = File.ReadAllText(@".\AppData\Temp\DeviceLog\InstalledPackages.csv");
                    return Logs;
                }
                catch
                {
                    return "List not available.";
                }
            }
        }

        public static void GetDeviceLogs()
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
            process.StartInfo.FileName = @$".\AppData\bin\iutool\getdulogs.exe";
            process.StartInfo.Arguments = @"-o .\AppData\Temp\DeviceLog.cab";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = false;

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            while (string.IsNullOrEmpty(process.MainWindowTitle))
            {
                Thread.Sleep(50);
                process.Refresh();
            }
            ShowWindow(process.MainWindowHandle, SC_MINIMIZE);
            string a = process.StandardOutput.ReadToEnd();
            process.WaitForExit(60000);
            
            ExtractCabinet(@".\AppData\Temp\DeviceLog.cab", @".\AppData\Temp\DeviceLog\");

        }

       
        public static void ExtractCabinet(string cabpath, string output)
        {
            
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
            process.StartInfo.FileName = @$"expand.exe";
            process.StartInfo.Arguments = @".\AppData\Temp\DeviceLog.cab -F:* .\AppData\Temp\DeviceLog\";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = false;

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            while (string.IsNullOrEmpty(process.MainWindowTitle))
            {
                Thread.Sleep(50);
                process.Refresh();
            }
            ShowWindow(process.MainWindowHandle, SC_MINIMIZE);
            string a = process.StandardOutput.ReadToEnd();
            process.WaitForExit(60000);
            if (File.Exists(@".\AppData\Temp\DeviceLog\ImgUpd.log"))
            {
                IsDeviceLogsPresent = true;
            }
            else
            {
                IsDeviceLogsPresent = false;
            }
        }

       


        public static string ReadDULogs()
        {
            if (File.Exists(@".\AppData\Temp\DeviceLog\ImgUpd.log") == true)
            {
                string Logs = File.ReadAllText(@".\AppData\Temp\DeviceLog\ImgUpd.log");
                return Logs;
            }
            else
            {
                try
                {
                    GetDeviceLogs();
                    string Logs = File.ReadAllText(@".\AppData\Temp\DeviceLog\ImgUpd.log");
                    return Logs;
                }
                catch
                {
                    return "Logs not available.";

                }
            }
        }



        public static void UpdateInMassStorage(List<string> fileList, string cabFolder, Process ProcessHook)
        {

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
            process.StartInfo.FileName = @$"AppData\bin\iutool\updateapp.exe";
            process.StartInfo.Arguments = $"install \"{cabFolder}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = false;
            process.OutputDataReceived += Process_OutputDataReceived1;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardError = true;
            ProcessHook = process;
            process.Start();
            process.WaitForExit();
        }

        private static void Process_OutputDataReceived1(object sender, DataReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }

}
