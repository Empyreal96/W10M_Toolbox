using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
//using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using W10M_Toolbox.DeviceHelper;
using WPDevPortal;

namespace W10M_Toolbox
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        string windowName;
        string DULogs;
        DispatcherTimer timer;
        private static TextBlock IUOutputBox;
        private static Process IUProcess { get; set; }
        public bool IsIUStarted;
        public bool IsRegistryMounted;
        string installedPkgs;
        List<string> updateFiles;
        string currentbuild;
        string q;
        string fullpath;
        string installedpkglog;
        string selectedbuild;
        string selectedBuildList;
        int filetotal;
        string DeviceSN;
        string DeviceModel;
        string DeviceManufacturer;
        string hivepath;
        string hivepath2;
        string loadedHive;
        RegistryKey registry;
        string loadedHive2;
        RegistryKey registry2;
        bool IsDefaultWUGUID;
        bool IsDevModeDefaults;
        string modelName;
        string modelvariant;
        string manufacturer;
        bool IsRebooting;
        bool IsBootedToMSC;
        string FlashOutput;
        Process Updateapp;
        static int PagingSize;
        bool IsFlightingEnabled;
        public MainWindow()
        {
            InitializeComponent();

            var proc = Process.GetCurrentProcess();
            windowName = proc.MainWindowTitle;
            IntPtr hWnd = (IntPtr)FindWindow(windowName, null);
            UsbNotification.RegisterUsbDeviceNotification(hWnd);
            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 1);
            //timer.Start();
            DownloadUpdate.IsEnabled = false;
            PushUpdate.IsEnabled = false;
            HomeAbout.Text = "Welcome, This tool will let you modify certain setting on your Windows 10 based Phone. This tool uses WPInternals to manage Reboots to different modes\n\n" +
                "Requirements:\n" +
                "- Access to Mass Storage Mode on your Phone\n" +
                "- Internet Access (Only for downloading Application Assets below)";
            UpdaterInfotext.Text = "Click \"View Update Log\" or \"Installed Packages\" to view the respective logs.\n\nSelect a build to download and flash. ";

        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            /* if (IsIUStarted == false)
             {

             } else
             {

                 await Task.Run(() =>
                 {
                     outp = IUProcess.StandardOutput.ReadToEnd();
                 });
                 UpdaterBuildOutput.Text = outp;
             } */
        }

        private async void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(@".\AppData\Temp\DeviceLog\") == false)
            {
                Directory.CreateDirectory(@".\AppData\Temp\DeviceLog\");
            }
            else
            {
                Directory.Delete(@".\AppData\Temp\DeviceLog\", true);
                Directory.CreateDirectory(@".\AppData\Temp\DeviceLog\");
            }
            if (File.Exists(@".\AppData\Temp\DeviceLog.cab") == true)
            {
                File.Delete(@".\AppData\Temp\DeviceLog.cab");
            }
            CheckForDevice();


        }




        private async void CheckForDevice()
        {
            if (IsRebooting == true)
            {

            }
            else
            {
                DeviceInfotext.Text = "Checking for connected devices, Please wait for IUTool and GetDuLogs to finish.\n\nIf your device isn't detected click \"Rescan for Device\"";
                string info = DeviceHelper.DeviceHelper.GetDeviceInfo();
                //string logs = DeviceHelper.DeviceHelper.ReadDULogs();

                if (DeviceHelper.DeviceHelper.IsDeviceConnected == true)
                {
                    string[] devinfo = info.Split("\n");

                    DeviceSN = devinfo[5].Replace("Serial: ", "").Replace("\r", "");

                    DeviceInfotext.Text = info;
                }
                else
                {
                    if (info.Contains("HRESULT = 0x80070490"))
                    {

                        GetMassStorageDrive();
                        if (IsBootedToMSC == true)
                        {
                            DeviceInfotext.Text = "Device is in Mass Storage Mode";
                        }
                        else
                        {
                            DeviceInfotext.Text = info;
                        }
                    }

                }
            }

        }










        private void AssetsButton_Click(object sender, RoutedEventArgs e)
        {
            AssetsWindow assetsWindows = new AssetsWindow();
            assetsWindows.Show();
        }



        private void RefreshDevice_Click(object sender, RoutedEventArgs e)
        {
            CheckForDevice();
        }




        private async void RebootToFlash_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceHelper.DeviceHelper.CheckWPIPath() != null)
            {
                HomeProgress.IsIndeterminate = true;
                IsRebooting = true;

                DeviceInfotext.Text = "Rebooting device to Mass Storage Mode.\n\nYour device will reboot several times, please wait.";
                await Task.Run(() =>
                {
                    FlashOutput = DeviceHelper.DeviceHelper.RebootToFlash(DeviceHelper.DeviceHelper.CheckWPIPath());
                });
                HomeProgress.IsIndeterminate = false;
                IsRebooting = false;
                DeviceInfotext.Text = FlashOutput;
            }
            else
            {
                DeviceInfotext.Text = "WPInternals could not be found! Make sure to check the Assets page";
            }
        }




        private async void RebootToMSC_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceHelper.DeviceHelper.CheckWPIPath() != null)
            {
                HomeProgress.IsIndeterminate = true;
                IsRebooting = true;
                DeviceInfotext.Text = "Rebooting device to Mass Storage Mode.\n\nYour device will reboot several times, please wait.";
                await Task.Run(() =>
                {
                    string MSCOutput = DeviceHelper.DeviceHelper.RebootToMassStorage(DeviceHelper.DeviceHelper.CheckWPIPath());
                });
                GetMassStorageDrive();
                if (IsBootedToMSC == true)
                {
                    HomeProgress.IsIndeterminate = false;
                    IsRebooting = false;
                    DeviceInfotext.Text = "Device is in Mass Storage Mode";
                }
                else
                {
                    DeviceInfotext.Text = "Check to see if your device has booted to Mass Storage correctly.";
                    IsRebooting = false;
                }
            }
            else
            {
                DeviceInfotext.Text = "WPInternals could not be found! Make sure to check the Assets page";
                IsRebooting = false;
            }

        }




        private async void RebootToNormal_Click(object sender, RoutedEventArgs e)
        {
            DeviceInfotext.Text = "Waiting for device";
            await Task.Run(() =>
            {
                DeviceHelper.DeviceHelper.RebootToNormal();
            });

            Thread.Sleep(50000);
            CheckForDevice();
        }





        private async void UpdaterBuildList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            selectedbuild = (sender as ComboBox).SelectedItem.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "");
            string[] build = selectedbuild.Split(".");
            currentbuild = build[2];
            DownloadUpdate.IsEnabled = true;
            PushUpdate.IsEnabled = true;

            UpdaterBuildOutput.Text = $"Build {selectedbuild} has been selected ";

        }




        private void TabUpdater_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private async void CheckPkgs_Click(object sender, RoutedEventArgs e)
        {

            UpdaterInfotext.Text = "Checking device logs, please wait.";
            await Task.Run(() =>
            {
                installedpkglog = DeviceHelper.DeviceHelper.GetInstalledPackages();
            });
            UpdaterInfotext.Text = installedpkglog;


        }




        private async void ViewErrorLog_Click(object sender, RoutedEventArgs e)
        {
            UpdaterInfotext.Text = "Checking device logs, please wait.";
            await Task.Run(() =>
            {
                DULogs = DeviceHelper.DeviceHelper.ReadDULogs();
            });

            UpdaterInfotext.Text = DULogs;
        }



        private void PushUpdate_Click(object sender, RoutedEventArgs e)
        {
            PushUpdateCabs($@".\AppData\PhoneUpdates\{currentbuild}\{DeviceSN}", UpdaterBuildOutput);
        }




        public bool IsDirectoryEmpty(string path)
        {


            return !Directory.EnumerateFileSystemEntries(path).Any();

        }




        public async void PushUpdateCabs(string CabPath, TextBlock OutputBox)
        {
            if (Directory.Exists(CabPath) == false)
            {
                UpdaterBuildOutput.Text = "Please download the files first";
            }
            else
            {
                if (IsDirectoryEmpty(CabPath) == true)
                {
                    UpdaterBuildOutput.Text = "Please download the files first";
                }
                else
                {

                    int num = 0;
                    fullpath = System.IO.Path.GetFullPath(CabPath);
                    var dir = Directory.EnumerateFiles(fullpath);

                    foreach (var i in dir)
                    {
                        num++;
                    }

                    // timer.Start();
                    UpdaterBuildOutput.Text = $"Sending {num} files to device from:\n\"{fullpath}\"\n\nThis will take several minutes. Please Wait";
                    UpdaterProgBar.IsIndeterminate = true;
                    await Task.Run(() =>
                    {
                        PushUpdateCabs();
                    });

                    UpdaterProgBar.IsIndeterminate = false;
                    // UpdaterBuildOutput.Text = q + "\n" + fullpath;
                    UpdaterBuildOutput.Text = $"Process finished. Progress will show on device shortly.. 'Settings > Update & Security > Phone Update'\n\n" +
                        $"If the update hasn't started after 5 minutes make sure device is detected by the app, and that you have downloaded the Update here first.\n\n" +
                        $"If you recieve errors, you can view the Update Log on the right.\n\n\n";

                }

            }
        }



        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            string data = IUProcess.StandardOutput.ReadLine();
            if (!File.Exists($@".\AppData\iutool.txt"))
            {
                File.Create($@".\AppData\iutool.txt");
            }
            File.WriteAllText(@".\AppData\iutool.txt", data);
            UpdaterBuildOutput.Text += e.Data.ToString();
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {

        }

        private void MetroTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }





        private async void DownloadUpdate_Click(object sender, RoutedEventArgs e)
        {
            //List<string> enumBuildList = ;
            List<string> filteredBuildList = new List<string>();

            UpdaterInfotext.Text = "Reading device logs";
            await Task.Run(() =>
            {
                installedPkgs = DeviceHelper.DeviceHelper.GetInstalledPackages().ToLower();
            });
            UpdaterInfotext.Text = installedPkgs;



            if (selectedbuild.Contains("10.0.10549.4"))
            {
                currentbuild = "10549";
                updateFiles = new List<string>();
                string[] list = File.ReadAllLines(@".\AppData\bin\buildlists\10549.4.txt");
                UpdaterBuildOutput.Text = "";
                foreach (string item in list)
                {

                    string altfiletype = item.Replace(".cab", "");
                    string filename1 = System.IO.Path.GetFileNameWithoutExtension(altfiletype);
                    //UpdaterBuildOutput.Text += $"{filename1}\n";
                    if (installedPkgs.Contains(filename1))
                    {

                        updateFiles.Add(item);
                        UpdaterBuildOutput.Text += $"{item}\n\n";

                    }

                }



            }


            if (selectedbuild.Contains("10.0.10570.0"))
            {
                currentbuild = "10570";
                updateFiles = new List<string>();
                string[] list = File.ReadAllLines(@".\AppData\bin\buildlists\10579.0.txt");
                UpdaterBuildOutput.Text = "";
                foreach (string item in list)
                {

                    string altfiletype = item.Replace(".cab", "");
                    string filename1 = System.IO.Path.GetFileNameWithoutExtension(altfiletype);
                    //UpdaterBuildOutput.Text += $"{filename1}\n";
                    if (installedPkgs.Contains(filename1))
                    {

                        updateFiles.Add(item);
                        UpdaterBuildOutput.Text += $"{item}\n\n";

                    }

                }
            }
            if (selectedbuild.Contains("10.0.10586.107"))
            {
                currentbuild = "10586";
                updateFiles = new List<string>();
                string[] list = File.ReadAllLines(@".\AppData\bin\buildlists\10586.107.txt");
                UpdaterBuildOutput.Text = "";
                foreach (string item in list)
                {

                    string altfiletype = item.Replace(".cab", "");
                    string filename1 = System.IO.Path.GetFileNameWithoutExtension(altfiletype);
                    //UpdaterBuildOutput.Text += $"{filename1}\n";
                    if (installedPkgs.Contains(filename1))
                    {

                        updateFiles.Add(item);
                        UpdaterBuildOutput.Text += $"{item}\n\n";

                    }

                }
            }
            if (selectedbuild.Contains("10.0.14393.1066"))
            {
                currentbuild = "14393";
                updateFiles = new List<string>();
                string[] list = File.ReadAllLines(@".\AppData\bin\buildlists\14393.1066.txt");
                UpdaterBuildOutput.Text = "";
                foreach (string item in list)
                {

                    string altfiletype = item.Replace(".cab", "");
                    string filename1 = System.IO.Path.GetFileNameWithoutExtension(altfiletype);
                    //UpdaterBuildOutput.Text += $"{filename1}\n";
                    if (installedPkgs.Contains(filename1))
                    {

                        updateFiles.Add(item);
                        UpdaterBuildOutput.Text += $"{item}\n\n";

                    }

                }
            }
            if (selectedbuild.Contains("10.0.15063.297"))
            {
                currentbuild = "15063";
                updateFiles = new List<string>();
                string[] list = File.ReadAllLines(@".\AppData\bin\buildlists\15063.297.txt");
                UpdaterBuildOutput.Text = "";
                foreach (string item in list)
                {

                    string altfiletype = item.Replace(".cab", "");
                    string filename1 = System.IO.Path.GetFileNameWithoutExtension(altfiletype);
                    //UpdaterBuildOutput.Text += $"{filename1}\n";
                    if (installedPkgs.Contains(filename1))
                    {
                        updateFiles.Add(item);
                        UpdaterBuildOutput.Text += $"{item}\n\n";

                    }

                }
            }
            if (selectedbuild.Contains("10.0.15254.603"))
            {
                currentbuild = "15254";
                updateFiles = new List<string>();
                string[] list = File.ReadAllLines(@".\AppData\bin\buildlists\15254.603.txt");
                UpdaterBuildOutput.Text = "";
                foreach (string item in list)
                {

                    string altfiletype = item.Replace(".cab", "");
                    string filename1 = System.IO.Path.GetFileName(altfiletype).ToLower();
                    //UpdaterBuildOutput.Text += $"{filename1}\n";
                    if (installedPkgs.Contains(filename1))
                    {
                        updateFiles.Add(item);
                        UpdaterBuildOutput.Text += $"{item}\n\n";

                    }

                }
            }

            if (!Directory.Exists($@".\AppData\PhoneUpdates\{currentbuild}\{DeviceSN}\"))
            {
                Directory.CreateDirectory($@".\AppData\PhoneUpdates\{currentbuild}\{DeviceSN}\");
            }
            else
            {

                Directory.Delete($@".\AppData\PhoneUpdates\{currentbuild}\{DeviceSN}\", true);
                Directory.CreateDirectory($@".\AppData\PhoneUpdates\{currentbuild}\{DeviceSN}\");

            }

            filetotal = updateFiles.Count;
            UpdaterBuildOutput.Text = "";
            updateFiles.Sort();
            int i = 0;
            foreach (string link in updateFiles)
            {
                i++;
                string dropboxlink = $"{link}?dl=1";
                string filename = System.IO.Path.GetFileName(link);
                WebClient client = new WebClient();

                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(Client_DownloadProgressChanged);
                client.DownloadFileCompleted += new AsyncCompletedEventHandler(Client_DownloadFileCompleted);
                UpdaterBuildOutput.Text = $"Downloading ({i}/{filetotal}): {dropboxlink}";
                await client.DownloadFileTaskAsync(new Uri(dropboxlink), $@".\AppData\PhoneUpdates\{currentbuild}\{DeviceSN}\{filename}");

            }


        }




        private async void PushUpdateCabs()
        {
            IsIUStarted = true;
            using (System.Diagnostics.Process process = new System.Diagnostics.Process())
            {
                IUProcess = process;

                //process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                process.StartInfo.FileName = @".\AppData\bin\iutool\iutool.exe";
                process.StartInfo.Arguments = $"-V -p \"{fullpath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.OutputDataReceived += Process_OutputDataReceived;
                process.ErrorDataReceived += Process_ErrorDataReceived;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardInput = false;
                process.StartInfo.RedirectStandardError = true;



                process.Start();
                //process.BeginOutputReadLine();
                while (process.StandardOutput.EndOfStream == false)
                {

                    var line = await process.StandardOutput.ReadToEndAsync();
                    Dispatcher.Invoke(DispatcherPriority.Normal,
                  new Action(() =>
                    UpdaterBuildOutput.Text += $"{line}\n"));

                }

                process.WaitForExit();
            }
        }

        void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            PushUpdate.IsEnabled = true;
            UpdaterBuildOutput.Text = "Downloads finished";
        }

        void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (currentbuild == "15254")
            {
                UpdaterProgBar.Value = e.ProgressPercentage;
            }
            else
            {
                double bytesIn = double.Parse(e.BytesReceived.ToString());
                double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
                double percentage = bytesIn / totalBytes * 100;

                UpdaterProgBar.Value = int.Parse(Math.Truncate(percentage).ToString());
            }
        }






        private void MountRegBtn_Click(object sender, RoutedEventArgs e)
        {

            CheckForReg();
            TweaksProgBar.IsIndeterminate = true;
            MassStorage();
            TweaksProgBar.IsIndeterminate = false;

        }

        private void CheckForReg()
        {
            try
            {

                RegistryInterop.Unload();
            }
            catch
            {

            }
        }

        private void UnMountRegBtn_Click(object sender, RoutedEventArgs e)
        {
            TweaksProgBar.IsIndeterminate = true;
            registry.Close();
            registry2.Close();
            //
            string regunloadstring = RegistryInterop.Unload();
            if (regunloadstring == "Error unloading Registry. Try again in a minute.")
            {
                BasicTweakPageHeader.Text = "Error unloading Registry. Try again in a minute.";
            }
            else
            {
                BasicTweakPageHeader.Text = $"{regunloadstring}  Unload Successful";
                IsRegistryMounted = false;
                UnMountRegBtn.IsEnabled = false;
                MountRegBtn.IsEnabled = true;
                UnMountRegBtn.Visibility = Visibility.Hidden;
                MountRegBtn.Visibility = Visibility.Visible;
            }
            TweaksProgBar.IsIndeterminate = false;

        }












        #region WPInternals_Models_MassStorage
        /// <summary>
        /// 
        /// This code below originates from "https://github.com/ReneLergner/WPinternals/blob/master/WPinternals/Models/MassStorage.cs"
        /// and is used to help detection of the MainOS Partition on a Windows Phone Device
        /// 
        /// The original code has been adapted for use with this software so will differ from original code from the respective owners
        ///
        /// 
        /// 
        ///
        /// 
        ///
        ///
        /// 
        ///
        ///
        internal string Drive = null;
        internal string PhysicalDrive = null;
        internal string VolumeLabel = null;
        internal IntPtr hVolume = (IntPtr)(-1);
        internal IntPtr hDrive = (IntPtr)(-1);
        string deviceType;

        public void GetMassStorageDrive()
        {
            ManagementObjectCollection coll = new ManagementObjectSearcher("select * from Win32_LogicalDisk").Get();
            foreach (ManagementObject logical in coll)
            {
                System.Diagnostics.Debug.Print(logical["Name"].ToString());

                string Label = "";
                foreach (ManagementObject partition in logical.GetRelated("Win32_DiskPartition"))
                {
                    foreach (ManagementObject drive in partition.GetRelated("Win32_DiskDrive"))
                    {

                        if (drive["PNPDeviceID"].ToString().Contains("VEN_QUALCOMM&PROD_MMC_STORAGE", StringComparison.CurrentCulture) ||
                            drive["PNPDeviceID"].ToString().Contains("VEN_MSFT&PROD_PHONE_MMC_STOR", StringComparison.CurrentCulture))
                        {
                            Debug.WriteLine("Found WP Device in Mass Storage.\n");
                            Label = logical["VolumeName"] == null ? "" : logical["VolumeName"].ToString();
                            if ((Drive == null) || string.Equals(Label, "MainOS", StringComparison.CurrentCultureIgnoreCase)) // Always prefer the MainOS drive-mapping
                            {
                                Drive = logical["Name"].ToString();
                                PhysicalDrive = drive["DeviceID"].ToString();
                                VolumeLabel = Label;
                                IsBootedToMSC = true;
                                Debug.WriteLine($"{Drive}\n{PhysicalDrive}\n{VolumeLabel}");
                            }
                            if (string.Equals(Label, "MainOS", StringComparison.CurrentCultureIgnoreCase))
                            {
                                Drive = logical["Name"].ToString();
                                PhysicalDrive = drive["DeviceID"].ToString();
                                VolumeLabel = Label;
                                IsBootedToMSC = true;
                                Debug.WriteLine($"{Drive}\n{PhysicalDrive}\n{VolumeLabel}");
                                break;
                            }
                        }
                    }
                    if (string.Equals(Label, "MainOS", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Drive = logical["Name"].ToString();

                        //PhysicalDrive = drive["DeviceID"].ToString();
                        VolumeLabel = Label;
                        IsBootedToMSC = true;
                        Debug.WriteLine($"{Drive}\n{VolumeLabel}");
                        break;
                    }
                }
            }
        }

        public void MassStorage()
        {
            try
            {

                GetMassStorageDrive();
                if (!File.Exists($"{Drive}\\Windows\\System32\\Config\\SYSTEM"))
                {
                    Debug.WriteLine($"{Drive}\\Windows\\System32\\Config\\SYSTEM  >  NOT FOUND\n");
                    BasicTweakPageHeader.Text = "Cannot load Phone Registry. Is the device connected and in Mass Storage Mode?";
                }
                else
                {
                    File.Copy($"{Drive}\\DPP\\MMO\\product.dat", ".\\AppData\\Temp\\product.dat", true);
                    Debug.WriteLine("Copied product.dat to TEMP\n");
                    string[] productinfo = File.ReadAllLines(".\\AppData\\Temp\\product.dat");

                    foreach (var info in productinfo)
                    {
                        if (info.Contains("TYPE:"))
                        {
                            deviceType = info.Replace("TYPE:", "");
                        }
                    }
                    hivepath = $"{Drive}\\Windows\\System32\\Config\\SYSTEM";
                    Debug.WriteLine("SYSTEM hive path set.\n");
                    hivepath2 = $"{Drive}\\Windows\\System32\\Config\\SOFTWARE";
                    Debug.WriteLine("SOFTWARE hive path set.\n");
                    string currentdate = DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");
                    if (!Directory.Exists(@".\AppData\RegistryBackups"))
                    {
                        Directory.CreateDirectory(@".\AppData\RegistryBackups");
                        Debug.WriteLine(@".\AppData\RegistryBackups  CREATED.\n");
                        if (!Directory.Exists($@".\AppData\RegistryBackups\{deviceType}\{currentdate}"))
                        {
                            Directory.CreateDirectory($@".\AppData\RegistryBackups\{deviceType}\{currentdate}");
                            Debug.WriteLine($".\\AppData\\RegistryBackups\\{deviceType}\\{currentdate}  >  CREATED.\n");
                        }
                    }
                    else
                    {
                        if (!Directory.Exists($@".\AppData\RegistryBackups\{deviceType}\{currentdate}"))
                        {
                            Directory.CreateDirectory($@".\AppData\RegistryBackups\{deviceType}\{currentdate}");
                            Debug.WriteLine($".\\AppData\\RegistryBackups\\{deviceType}\\{currentdate}  >  CREATED. \n");
                        }
                    }
                    File.Copy($"{Drive}\\Windows\\System32\\Config\\SYSTEM", $@".\AppData\RegistryBackups\{deviceType}\{currentdate}\SYSTEM.hive");
                    Debug.WriteLine($".\\AppData\\RegistryBackups\\{deviceType}\\{currentdate}\\SYSTEM.hive  >   CREATED\n");
                    File.Copy($"{Drive}\\Windows\\System32\\Config\\SOFTWARE", $@".\AppData\RegistryBackups\{deviceType}\{currentdate}\SOFTWARE.hive");
                    Debug.WriteLine($".\\AppData\\RegistryBackups\\{deviceType}\\{currentdate}\\SOFTWARE.hive   >  CREATED\n");
                    File.Copy($"{Drive}\\EFIESP\\EFI\\Microsoft\\BOOT\\BCD", $@".\AppData\RegistryBackups\{deviceType}\{currentdate}\BCD.backup");

                    loadedHive = RegistryInterop.Load(hivepath);
                    registry = Registry.Users.OpenSubKey(loadedHive, true);

                    Debug.Write($"SYSTEM Hive loaded from: {hivepath}\n");

                    loadedHive2 = RegistryInterop.Load(hivepath2);
                    registry2 = Registry.Users.OpenSubKey(loadedHive2, true);
                    Debug.WriteLine($"SOFTWARE hive loaded from: {hivepath2}");
                    BasicTweakPageHeader.Text = "Registry Loaded, View/Modify the values below. Click 'Save Changes' to apply edits";
                    IsRegistryMounted = true;
                    RegistryKey DeviceBranding = registry.CreateSubKey(@"Platform\DeviceTargetingInfo", RegistryKeyPermissionCheck.ReadSubTree);
                    RegistryKey WUSettings = registry2.CreateSubKey(@"Microsoft\Windows\CurrentVersion\DeviceUpdate\Agent\Settings", RegistryKeyPermissionCheck.ReadSubTree);
                    RegistryKey DevModeConfig = registry2.CreateSubKey(@"Microsoft\Windows\CurrentVersion\AppModelUnlock", RegistryKeyPermissionCheck.ReadSubTree);
                    RegistryKey MTPSettings = registry2.CreateSubKey(@"Microsoft\MTP", RegistryKeyPermissionCheck.ReadSubTree);
                    RegistryKey DevicePortalSetting = registry.CreateSubKey(@"ControlSet001\Services\WebManagement", RegistryKeyPermissionCheck.ReadSubTree);
                    RegistryKey DevicePortalAuthSetting = registry2.CreateSubKey(@"Microsoft\Windows\CurrentVersion\WebManagement\Authentication", RegistryKeyPermissionCheck.ReadSubTree);
                    RegistryKey LocalCrashDumpsSetting = registry2.CreateSubKey(@"Microsoft\Windows\Windows Error Reporting\LocalDumps", RegistryKeyPermissionCheck.ReadSubTree);
                    RegistryKey WindowsFirewallSetting = registry.CreateSubKey(@"ControlSet001\Services\MpsSvc", RegistryKeyPermissionCheck.ReadSubTree);
                    RegistryKey PagingSettings = registry.CreateSubKey(@"ControlSet001\Control\Session Manager\Memory Management", RegistryKeyPermissionCheck.ReadSubTree);
                    if (DeviceBranding != null)
                    {
                        modelName = DeviceBranding.GetValue("PhoneModelName").ToString();
                        DeviceModelBox.Text = modelName;
                        modelvariant = DeviceBranding.GetValue("PhoneHardwareVariant").ToString();
                        DeviceVariantBox.Text = modelvariant;
                        manufacturer = DeviceBranding.GetValue("PhoneManufacturerDisplayName").ToString();
                        DeviceOEMNameBox.Text = manufacturer;
                        string socversion = DeviceBranding.GetValue("PhoneSOCVersion").ToString();
                        SOCVersionLabel.Content = socversion;


                        UnMountRegBtn.IsEnabled = true;
                        MountRegBtn.IsEnabled = false;
                        UnMountRegBtn.Visibility = Visibility.Visible;
                        MountRegBtn.Visibility = Visibility.Hidden;
                        SaveBasicChangesBtn.Visibility = Visibility.Visible;
                    }
                    if (WUSettings != null)
                    {
                        string WUGUID = WUSettings.GetValue("GuidOfCategoryToScan").ToString();
                        if (WUGUID == "00000000-0000-0000-0000-000000000000")
                        {
                            WUCheckBox.IsChecked = false;
                        }
                        else
                        {
                            if (!File.Exists(@$".\AppData\RegistryBackups\{deviceType}\WUGUID.txt"))
                            {
                                File.AppendAllText(@$".\AppData\RegistryBackups\{deviceType}\WUGUID.txt", $"WUGUID:{WUGUID}");
                            }
                            IsDefaultWUGUID = true;
                            WUCheckBox.IsChecked = true;

                        }
                    }
                    if (DevModeConfig != null)
                    {
                        // string AllowAllTrustedApps = .ToString();
                        if (DevModeConfig.GetValue("AllowAllTrustedApps") == null)
                        {
                            IsDevModeDefaults = true;
                        }
                        else
                        {
                            if (DevModeConfig.GetValue("AllowAllTrustedApps").ToString() == "1")
                            {
                                DevModeCheckBox.IsChecked = true;
                                IsDevModeDefaults = false;
                            }
                            else
                            {
                                DevModeCheckBox.IsChecked = false;
                                IsDevModeDefaults = true;
                            }
                        }

                        if (IsDevModeDefaults == true)
                        {
                            DevModeCheckBox.IsChecked = false;
                        }
                        else
                        {
                            DevModeCheckBox.IsChecked = true;
                        }


                    }
                    if (MTPSettings != null)
                    {
                        MTPUSBButton.IsChecked = false;
                    }
                    else
                    {
                        if (MTPSettings.GetValue("DataStore").ToString() == @"C:\Data\Users\PUBLIC")
                        {
                            MTPUSBButton.IsChecked = false;
                        }
                        if (MTPSettings.GetValue("DataStore").ToString() == @"C:")
                        {
                            MTPUSBButton.IsChecked = true;
                        }
                        else
                        {
                            MTPUSBButton.IsChecked = false;
                        }
                    }



                    if (DevicePortalSetting != null)
                    {

                        if (DevicePortalSetting.GetValue("Start").ToString() == "2")
                        {
                            DevPortalBtn.IsChecked = true;
                            Debug.WriteLine($"{DevicePortalSetting.GetValue("Start").ToString()}    < Device Portal \"start\"");
                        }
                        else
                        {
                            DevPortalBtn.IsChecked = false;
                        }
                    }
                    else
                    {
                        DevPortalBtn.IsChecked = false;

                    }


                    if (DevicePortalAuthSetting != null)
                    {
                        if (DevicePortalAuthSetting.GetValue("Disabled").ToString() == "1")
                        {
                            DevPortalAuthBtn.IsChecked = false;
                        }
                        else
                        {
                            DevPortalAuthBtn.IsChecked = true;
                        }


                    }
                    else
                    {
                        DevPortalAuthBtn.IsChecked = true;

                    }

                    if (LocalCrashDumpsSetting != null)
                    {
                        if (LocalCrashDumpsSetting.GetValue("DumpType") != null)
                        {
                            if (LocalCrashDumpsSetting.GetValue("DumpType").ToString() == "2")
                            {
                                LocalCrashDumpsCheck.IsChecked = true;
                            }
                            else
                            {
                                LocalCrashDumpsCheck.IsChecked = false;
                            }

                        }
                        else
                        {
                            LocalCrashDumpsCheck.IsChecked = false;

                        }

                    }
                    else
                    {
                        LocalCrashDumpsCheck.IsChecked = false;
                    }

                    if (WindowsFirewallSetting != null)
                    {
                        if (WindowsFirewallSetting.GetValue("Start").ToString() == "2")
                        {
                            FirewallCheck.IsChecked = true;
                        }
                        else
                        {
                            FirewallCheck.IsChecked = false;
                        }
                    }


                    if (PagingSettings != null)
                    {
                        string[] dataset = (string[])PagingSettings.GetValue("PagingFiles");
                        string datastring = String.Join("", dataset);
                        Debug.WriteLine($"\n{datastring}");
                        if (datastring == @"u:\pagefile.sys 256 256")
                        {
                            PagingSlider.Value = (double)256;
                            PagingLabel.Text = "Page File Size (256MB)";


                        }
                        if (datastring == @"u:\pagefile.sys 512 512")
                        {
                            PagingSlider.Value = (double)512;
                            PagingLabel.Text = "Page File Size (512MB)";

                        }
                        if (datastring == @"u:\pagefile.sys 1024 1024")
                        {
                            PagingSlider.Value = (double)1024;
                            PagingLabel.Text = "Page File Size (1024MB)";

                        }
                        if (datastring == @"u:\pagefile.sys 2048 2048")
                        {
                            PagingSlider.Value = (double)2048;
                            PagingLabel.Text = "Page File Size (2048MB)";

                        }

                        // Destination :- HKEY_LOCAL_MACHINE \ SYSTEM \ CurrentControlSet \ Control \ Session Manager \ Memory Management/ Pagingfiles

                        // Then you can change the value  according to what amount of paging files you wish to have.

                        //256mb - Input 256 256
                        //512mb - Input 512 512
                        //1GB - Input 1024 1024
                        //2GB - Input 1980 1980
                    }
                    else
                    {
                        BasicTweakPageHeader.Text = "Didn't work";
                        // Debug.WriteLine($"\n{(string[])PagingSettings.GetValue("PagingFiles")[1]}");
                    }

                    CheckBCD();
                    if (IsFlightingEnabled == false)
                    {
                        FlightSigningCheck.IsChecked = false;
                    }
                    else
                    {
                        FlightSigningCheck.IsChecked = true;
                    }
                }

            }
            catch
            {
                IsRegistryMounted = false;
                BasicTweakPageHeader.Text = "Device not found, Make sure you are in Mass Storage Mode";
            }

        }

        #endregion

        private void CheckBCD()
        {
            //flightsigning           Yes
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
            process.StartInfo.FileName = @$"bcdedit.exe";
            process.StartInfo.Arguments = $"/store \"{Drive}\\EFIESP\\EFI\\Microsoft\\BOOT\\BCD\" /enum";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardError = true;

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            if (output.Contains("flightsigning           Yes"))
            {
                if (File.Exists($"{Drive}\\EFIESP\\EFI\\Microsoft\\BOOT\\policies\\SbcpFlightToken.p7b"))
                {
                    IsFlightingEnabled = true;
                }
                else
                {
                    IsFlightingEnabled = false;
                }

            }
            else
            {
                IsFlightingEnabled = false;
            }
            process.WaitForExit();
        }

        private void ModifyBCD(string OnOff)
        {


            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
            process.StartInfo.FileName = @$"bcdedit.exe";
            process.StartInfo.Arguments = $"/store \"{Drive}\\EFIESP\\EFI\\Microsoft\\BOOT\\BCD\" /set " + "{default} flightsigning " + OnOff;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardError = true;

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }


        private void DevModeCheckBox_Checked(object sender, RoutedEventArgs e)
        {


        }

        private void WUCheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void WUCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void DevModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {

        }


        private void SaveBasicChangesBtn_Click(object sender, RoutedEventArgs e)
        {
            TweaksProgBar.IsIndeterminate = true;
            BasicTweakPageHeader.Text = "Writing settings to Phone Registry";
            // Save Reg Keys
            RegistryKey DevModeConfig = registry2.CreateSubKey(@"Microsoft\Windows\CurrentVersion\AppModelUnlock", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (DevModeCheckBox.IsChecked == true)
            {

                DevModeConfig.SetValue("AllowAllTrustedApps", 1, RegistryValueKind.DWord);
                DevModeConfig.SetValue("AllowDevelopmentWithoutDevLicense", 1, RegistryValueKind.DWord);
            }
            else //false
            {
                DevModeConfig.SetValue("AllowAllTrustedApps", 0, RegistryValueKind.DWord);
                DevModeConfig.SetValue("AllowDevelopmentWithoutDevLicense", 0, RegistryValueKind.DWord);
            }


            RegistryKey WUSettings = registry2.CreateSubKey(@"Microsoft\Windows\CurrentVersion\DeviceUpdate\Agent\Settings", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (WUCheckBox.IsChecked == true)
            {
                string originalValue = File.ReadAllText(@$".\AppData\RegistryBackups\{deviceType}\WUGUID.txt").Replace("WUGUID:", "");
                WUSettings.SetValue("GuidOfCategoryToScan", originalValue, RegistryValueKind.String);

            }
            else //false
            {
                WUSettings.SetValue("GuidOfCategoryToScan", "00000000-0000-0000-0000-000000000000", RegistryValueKind.String);
            }


            RegistryKey DeviceBranding = registry.CreateSubKey(@"Platform\DeviceTargetingInfo", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (DeviceModelBox.Text != modelName)
            {
                DeviceBranding.SetValue("PhoneModelName", DeviceModelBox.Text, RegistryValueKind.String);
            }
            if (DeviceVariantBox.Text != modelvariant)
            {
                DeviceBranding.SetValue("PhoneHardwareVariant", DeviceVariantBox.Text, RegistryValueKind.String);
            }
            if (DeviceOEMNameBox.Text != manufacturer)
            {
                DeviceBranding.SetValue("PhoneManufacturerDisplayName", DeviceOEMNameBox.Text, RegistryValueKind.String);
            }

            // reg add HKLM\Software\Microsoft\MTP /V DataStore /t REG_SZ /d C: /f)
            // (reg add HKLM\Software\ /V DataStore /t REG_SZ /d  /f
            RegistryKey MTPSettings = registry2.CreateSubKey(@"Microsoft\MTP", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (MTPUSBButton.IsChecked == true)
            {
                MTPSettings.SetValue("DataStore", "C:", RegistryValueKind.String);
            }
            else
            {
                MTPSettings.SetValue("DataStore", @"C:\Data\Users\PUBLIC", RegistryValueKind.String);
            }


            // reg add "HKEY_LOCAL_MACHINE\RTSystem\" /v Start /t REG_DWORD /d 2 /f
            // reg add "HKEY_LOCAL_MACHINE\RTSoftware\" / v Disabled / t REG_DWORD / d 1 / f
            RegistryKey DevicePortalSetting = registry.CreateSubKey(@"ControlSet001\Services\WebManagement", RegistryKeyPermissionCheck.ReadWriteSubTree);
            RegistryKey DevicePortalAuthSetting = registry2.CreateSubKey(@"Microsoft\Windows\CurrentVersion\WebManagement\Authentication", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (DevPortalBtn.IsChecked == true)
            {
                DevicePortalSetting.SetValue("Start", 2, RegistryValueKind.DWord);
            }
            else
            {
                DevicePortalSetting.SetValue("Start", 4, RegistryValueKind.DWord);
            }
            if (DevPortalAuthBtn.IsChecked == true)
            {
                DevicePortalAuthSetting.SetValue("Disabled", 0, RegistryValueKind.DWord);
            }
            else
            {
                DevicePortalAuthSetting.SetValue("Disabled", 1, RegistryValueKind.DWord);
            }


            RegistryKey WindowsFirewallSetting = registry.CreateSubKey(@"ControlSet001\Services\MpsSvc", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (FirewallCheck.IsChecked == true)
            {
                WindowsFirewallSetting.SetValue("Start", 2, RegistryValueKind.DWord);
            }
            else
            {
                WindowsFirewallSetting.SetValue("Start", 4, RegistryValueKind.DWord);
            }


            RegistryKey LocalCrashDumpsSetting = registry2.CreateSubKey(@"Microsoft\Windows\Windows Error Reporting\LocalDumps", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (LocalCrashDumpsCheck.IsChecked == true)
            {
                string dumppath1 = @"C:\Data\Users\Public\CrashDumps";
                /* string dumppath = "43,00,3a,00,5c,00,44,00,61,00,74,00,61,00,5c,00,55," +
                                              "00,73,00,65,00,72,00,73,00,5c,00,50,00,75,00,62,00," +
                                              "6c,00,69,00,63,00,5c,00,44,00,6f,00,63,00,75,00,6d," +
                                              "00,65,00,6e,00,74,00,73,00,00,00"; */
                //var dumpdata = dumppath.Split(",").Select(x => Convert.ToByte(x, 16)).ToArray();
                if (LocalCrashDumpsSetting == null)
                {
                    registry2.CreateSubKey(@"Microsoft\Windows\Windows Error Reporting\LocalDumps", true);
                    Thread.Sleep(2000);
                    if (registry2.OpenSubKey(@"Microsoft\Windows\Windows Error Reporting\LocalDumps", true) != null)
                    {
                        LocalCrashDumpsSetting.SetValue("DumpType", 2, RegistryValueKind.DWord);

                        LocalCrashDumpsSetting.SetValue("DumpFolder", dumppath1, RegistryValueKind.ExpandString);

                        LocalCrashDumpsSetting.SetValue("DumpCount", 10, RegistryValueKind.DWord);
                    }
                }
                else
                {
                    LocalCrashDumpsSetting.SetValue("DumpType", 2, RegistryValueKind.DWord);

                    LocalCrashDumpsSetting.SetValue("DumpFolder", dumppath1, RegistryValueKind.ExpandString);

                    LocalCrashDumpsSetting.SetValue("DumpCount", 10, RegistryValueKind.DWord);
                }




            }
            else
            {
                LocalCrashDumpsSetting.SetValue("DumpType", 0, RegistryValueKind.DWord);
                LocalCrashDumpsSetting.SetValue("DumpCount", 0, RegistryValueKind.DWord);

            }

            RegistryKey PagingSettings = registry.OpenSubKey(@"ControlSet001\Control\Session Manager\Memory Management", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (PagingSlider.Value == 256)
            {
                string[] newvalue = { @"u:\pagefile.sys 256 256" };
                PagingSettings.SetValue("PagingFiles", newvalue, RegistryValueKind.MultiString);
            }
            if (PagingSlider.Value == 512)
            {
                string[] newvalue = { @"u:\pagefile.sys 512 512" };
                PagingSettings.SetValue("PagingFiles", newvalue, RegistryValueKind.MultiString);
            }
            if (PagingSlider.Value == 1024)
            {
                string[] newvalue = { @"u:\pagefile.sys 1024 1024" };
                PagingSettings.SetValue("PagingFiles", newvalue, RegistryValueKind.MultiString);
            }
            if (PagingSlider.Value == 2048)
            {
                string[] newvalue = { @"u:\pagefile.sys 2048 2048" };
                PagingSettings.SetValue("PagingFiles", newvalue, RegistryValueKind.MultiString);
            }

            if (FlightSigningCheck.IsChecked == true)
            {
                ModifyBCD("on");
                if (File.Exists($"{Drive}\\EFIESP\\EFI\\Microsoft\\BOOT\\policies\\SbcpFlightToken.p7b"))
                {

                }
                else
                {
                    File.Copy(@".\AppData\bin\res\flightcert\SbcpFlightToken.p7b", $"{Drive}\\EFIESP\\EFI\\Microsoft\\BOOT\\policies\\SbcpFlightToken.p7b");
                }
            }
            else
            {
                ModifyBCD("off");
            }

            Thread.Sleep(1000);
            BasicTweakPageHeader.Text = "Saved settings to device";
            TweaksProgBar.IsIndeterminate = false;
        }



        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            if (IsRegistryMounted == true)
            {
                MessageBox.Show("Make sure to unload your Phone Registry before closing!");
                e.Cancel = true;
            }
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {

        }

























        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Adds the windows message processing hook and registers USB device add/removal notification.
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            if (source != null)
            {
                var windowHandle = source.Handle;
                source.AddHook(HwndHandler);
                UsbNotification.RegisterUsbDeviceNotification(windowHandle);
            }
        }

        /// <summary>
        /// Method that receives window messages.
        /// </summary>
        private IntPtr HwndHandler(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            if (msg == UsbNotification.WmDevicechange)
            {
                switch ((int)wparam)
                {
                    case UsbNotification.DbtDeviceremovecomplete:
                        Usb_DeviceRemoved(); // this is where you do your magic
                        break;
                    case UsbNotification.DbtDevicearrival:
                        Usb_DeviceAdded(); // this is where you do your magic
                        break;
                }
            }

            handled = false;
            return IntPtr.Zero;
        }

        private void Usb_DeviceRemoved()
        {
            Thread.Sleep(2500);
            CheckForDevice();
        }

        private void Usb_DeviceAdded()
        {
            Thread.Sleep(2500);
            CheckForDevice();
        }



    }


    /// <summary>
    /// USB Notifier
    /// </summary>
    internal static class UsbNotification
    {
        public const int DbtDevicearrival = 0x8000; // system detected a new device        
        public const int DbtDeviceremovecomplete = 0x8004; // device is gone      
        public const int WmDevicechange = 0x0219; // device change event      
        private const int DbtDevtypDeviceinterface = 5;
        private static readonly Guid GuidDevinterfaceUSBDevice = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED"); // USB devices
        private static IntPtr notificationHandle;

        /// <summary>
        /// Registers a window to receive notifications when USB devices are plugged or unplugged.
        /// </summary>
        /// <param name="windowHandle">Handle to the window receiving notifications.</param>
        public static void RegisterUsbDeviceNotification(IntPtr windowHandle)
        {
            DevBroadcastDeviceinterface dbi = new DevBroadcastDeviceinterface
            {
                DeviceType = DbtDevtypDeviceinterface,
                Reserved = 0,
                ClassGuid = GuidDevinterfaceUSBDevice,
                Name = 0
            };

            dbi.Size = Marshal.SizeOf(dbi);
            IntPtr buffer = Marshal.AllocHGlobal(dbi.Size);
            Marshal.StructureToPtr(dbi, buffer, true);

            notificationHandle = RegisterDeviceNotification(windowHandle, buffer, 0);
        }

        /// <summary>
        /// Unregisters the window for USB device notifications
        /// </summary>
        public static void UnregisterUsbDeviceNotification()
        {
            UnregisterDeviceNotification(notificationHandle);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(IntPtr recipient, IntPtr notificationFilter, int flags);

        [DllImport("user32.dll")]
        private static extern bool UnregisterDeviceNotification(IntPtr handle);

        [StructLayout(LayoutKind.Sequential)]
        private struct DevBroadcastDeviceinterface
        {
            internal int Size;
            internal int DeviceType;
            internal int Reserved;
            internal Guid ClassGuid;
            internal short Name;
        }
    }



}
