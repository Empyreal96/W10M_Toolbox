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
using System.Text.RegularExpressions;
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
using DiskManager;
using FfuConvert;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using Microsoft.WindowsAPICodePack.Dialogs;
using ReadFromDevice;
using W10M_Toolbox.DeviceHelper;
using WPDevPortal;
using static W10M_Toolbox.PartitionHelper;

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
        string MainOSDrive;
        string DataDrive;
        string EFIESPDrive;
        string MainOSPhysicalDisk;
        string MainOSLabel;
        string EFIESPLabel;
        string Datalabel;

        BackgroundWorker worker;

        public MainWindow()
        {

            InitializeComponent();
            worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;

            worker2 = new BackgroundWorker();
            worker2.DoWork += Worker2_DoWork;
            worker2.ProgressChanged += Worker2_ProgressChanged;
            worker2.RunWorkerCompleted += Worker2_RunWorkerCompleted;
            worker2.WorkerReportsProgress = true;
            worker2.WorkerSupportsCancellation = true;


            SaveFFUAsVhdx.IsEnabled = false;
            SaveFFUManifestBtn.IsEnabled = false;
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
                "- EFIESP, Data and MainOS mounted to a Drive Letter" +
                "- Internet Access (Only for downloading Application Assets below)";
            UpdaterInfotext.Text = "Click \"View Update Log\" or \"Installed Packages\" to view the respective logs.\n\nSelect a build to download and flash. ";
            BackupOutput.Text = "Here you can backup either the whole Disk or selected partitions. Make sure your phone is fully charged before a full disk backup!\n\nThis uses \"dd for windows by John Newbigin\" to make the partition backups only\n\n";

        }

        private void Worker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (IsCancelBackupClicked != true)
            {
                MessageBox.Show("Successfully dumped partitions");
            }
        }

        private void Worker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Debug.WriteLine(".");
        }
        int MAINOS_INDEX;
        int DATA_INDEX;
        int EFIESP_INDEX;
        bool IsBackupOutSelected = false;
        private void Worker2_DoWork(object sender, DoWorkEventArgs e)
        {
            var openfolder = new CommonOpenFileDialog();
            openfolder.Title = "Select output for emmc dump file";
            openfolder.IsFolderPicker = true;
            openfolder.InitialDirectory = ".\\";
            openfolder.EnsurePathExists = true;
            Dispatcher.Invoke(new Action(() =>
            {
                if (openfolder.ShowDialog() == CommonFileDialogResult.Ok)
            {
                IsBackupOutSelected = true;
            } else
            {
                IsBackupOutSelected = false;
            }
            }), DispatcherPriority.ContextIdle);


            if (IsBackupOutSelected == true)
            {


                DriveInfo[] driveInfos = DriveInfo.GetDrives();


                var diskid = Regex.Match(MAINOS_PHYSICAL, @"\d+").Value;
                
                Debug.WriteLine($"DiskID: {Int32.Parse(diskid)}");
                int partcount = 0;
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_DiskPartition");
                var diskSize = DiskInfo.GetPhysDiskSize(MAINOS_PHYSICAL);
                Console.WriteLine();
                foreach (ManagementObject queryObj in searcher.Get())
                {

                    if ((uint)queryObj["DiskIndex"] == Int32.Parse(diskid))
                    {
                        partcount++;
                        Debug.WriteLine("-----------------------------------");

                        Debug.WriteLine("Win32_DiskPartition instance");

                        Debug.WriteLine($"Name: {(string)queryObj["Name"]}");

                        Debug.WriteLine("Index:{0}", (uint)queryObj["Index"]);

                        Debug.WriteLine("DiskIndex:{0}", (uint)queryObj["DiskIndex"]);

                        Debug.WriteLine("BootPartition:{0}", (bool)queryObj["BootPartition"]);

                        Debug.WriteLine("Size: {0}", (UInt64)queryObj["Size"]);

                        
                    }




                }
                Debug.WriteLine("Sleep for 100ms");
                Thread.Sleep(500);
                


                
                string path = $"{openfolder.FileName}\\emmc_dump.img";

                Dispatcher.Invoke(new Action(() =>
                {

                BackupOutput.Text =
                    $"Disk selected: {MAINOS_PHYSICAL}\n" +
                    $"Partitions on disk: {partcount}\n" +
                    $"Total disk size: {diskSize.ToFileSize()}\n" +
                    $"Output: {path}\n\n" +
                    $"This will take a long time depending on Disk size. Please wait and DO NOT remove the disk until finished.";

                }), DispatcherPriority.ContextIdle);



                var reader = new BinaryReader(new DeviceStream(MAINOS_PHYSICAL));
                    var writer = new BinaryWriter(new FileStream(path, FileMode.Create));
                long mb = 1024;
                    var buffer = new byte[4096];
                    int count;
                    int loopcount = 0;

                    try
                    {

                        while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
                        {
                        if (IsCancelBackupClicked == true)
                        {
                            e.Cancel = true;
                            return;
                        }
                            writer.Write(buffer, 0, buffer.Length);
                            if (loopcount % 100 == 0)
                            {
                            mb++;
                            Debug.WriteLine(mb);
                                writer.Flush();
                            }
                            loopcount++;

                        }
                    }
                    catch (Exception ee)
                    {
                        Debug.WriteLine(ee.Message);
                        Debug.WriteLine("");
                        Debug.WriteLine(ee.StackTrace);
                        Debug.WriteLine("");
                        Debug.WriteLine(ee.Source);
                        //Console.ReadLine();
                    }
                    reader.Close();
                    writer.Flush();
                    writer.Close();
                
                BackupProgressBar.IsIndeterminate = false;
                MessageBox.Show($"Dumped successfully to {path}");
                BackupOutput.Text += "Finished!";


            } else
            {
                return;
            }

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
                            if (DATA_DETAIL == null)
                            {
                                MessageBox.Show("Please make sure to have EFIESP and DATA mounted to a Drive Letter, check Disk Management");
                            }
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

                DeviceInfotext.Text = "Rebooting device to Mass Storage Mode.\n\nYour device will reboot several times, If your device loads to Mass Storage but no change here, close WPInternals from the taskbar.";
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

            //UpdaterBuildOutput.Text = $"Build {selectedbuild} has been selected ";
            if (Directory.Exists($".\\AppData\\PhoneUpdates\\{currentbuild}\\{DeviceSN}"))
            {
                string[] files = System.IO.Directory.GetFiles($".\\AppData\\PhoneUpdates\\{currentbuild}\\{DeviceSN}");
                if (files.Length == 0)
                {
                    UpdaterBuildOutput.Text = $"Build {selectedbuild} has been selected.\n\n Files have already been downloaded for this device";

                }
                else
                {
                    UpdaterBuildOutput.Text = $"Build {selectedbuild} has been selected.\n\n Files need to be downloaded.";


                }
            }

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
        string EFIESP_DRIVE;
        string DATA_DRIVE;
        string MAINOS_DRIVE;
        string EFIESP_PHYSICAL;
        string DATA_PHYSICAL;
        string MAINOS_PHYSICAL;
        string EFIESP_DETAIL;
        string DATA_DETAIL;
        string MAINOS_DETAIL;
        string EFIESP_FILESYSTEM;
        string DATA_FILESYSTEM;
        string MAINOS_FILESYSTEM;
        ulong EFIESP_SIZE;
        ulong DATA_SIZE;
        ulong MAINOS_SIZE;
        string MAINOS_NAME;
        string DATA_NAME;
        string EFIESP_NAME;
        internal string Drive = null;
        public static string PhysicalDrive = null;
        internal string VolumeLabel = null;
        internal IntPtr hVolume = (IntPtr)(-1);
        internal IntPtr hDrive = (IntPtr)(-1);
        string deviceType;

        public void GetMassStorageDrive()
        {
            ManagementObjectCollection coll = new ManagementObjectSearcher("select * from Win32_LogicalDisk").Get();
            foreach (ManagementObject logical in coll)
            {
                System.Diagnostics.Debug.WriteLine(logical["Name"].ToString());

                string Label = "";
                foreach (ManagementObject partition in logical.GetRelated("Win32_DiskPartition"))
                {
                    foreach (ManagementObject drive in partition.GetRelated("Win32_DiskDrive"))
                    {

                        if (drive["PNPDeviceID"].ToString().Contains("VEN_QUALCOMM&PROD_MMC_STORAGE", StringComparison.CurrentCulture) ||
                            drive["PNPDeviceID"].ToString().Contains("VEN_MSFT&PROD_PHONE_MMC_STOR", StringComparison.CurrentCulture) || drive["PNPDeviceID"].ToString().Contains("VEN_MSFT&PROD_VIRTUAL_DISK", StringComparison.CurrentCulture))
                        {

                            Label = logical["VolumeName"] == null ? "" : logical["VolumeName"].ToString();

                            if (string.Equals(Label, "EFIESP", StringComparison.CurrentCultureIgnoreCase))
                            {
                                EFIESP_SIZE = Convert.ToUInt64(logical.Properties["Size"].Value);

                                EFIESP_DRIVE = logical["Name"].ToString();
                                EFIESP_PHYSICAL = drive["DeviceID"].ToString();
                                EFIESP_DETAIL = $"EFIESP FOUND: PhysicalDrive: {EFIESP_PHYSICAL} | Drive: {EFIESP_DRIVE} | Size: {((long)EFIESP_SIZE).ToFileSize()}";
                                BackupOutput.Text += $"EFIESP FOUND: PhysicalDrive: {EFIESP_PHYSICAL} | Drive: {EFIESP_DRIVE} | Size: {((long)EFIESP_SIZE).ToFileSize()}\n";
                                Debug.WriteLine($"EFIESP FOUND: PhysicalDrive: {EFIESP_PHYSICAL} | Drive: {EFIESP_DRIVE} | Size: {((long)EFIESP_SIZE).ToFileSize()}");
                            }

                            if (string.Equals(Label, "Data", StringComparison.CurrentCultureIgnoreCase))
                            {
                                DATA_SIZE = Convert.ToUInt64(logical.Properties["Size"].Value);
                                DATA_DRIVE = logical["Name"].ToString();
                                DATA_PHYSICAL = drive["DeviceID"].ToString();
                                DATA_DETAIL = "Data FOUND: PhysicalDrive: {DATA_PHYSICAL} | Drive: {DATA_DRIVE} | | Size: {((long)DATA_SIZE).ToFileSize()}";
                                BackupOutput.Text += $"Data FOUND: PhysicalDrive: {DATA_PHYSICAL} | Drive: {DATA_DRIVE} | | Size: {((long)DATA_SIZE).ToFileSize()}\n";
                                Debug.WriteLine($"Data FOUND: PhysicalDrive: {DATA_PHYSICAL} | Drive: {DATA_DRIVE} | | Size: {((long)DATA_SIZE).ToFileSize()}");
                            }

                            if ((Drive == null) || string.Equals(Label, "MainOS", StringComparison.CurrentCultureIgnoreCase)) // Always prefer the MainOS drive-mapping
                            {
                                MAINOS_SIZE = Convert.ToUInt64(logical.Properties["Size"].Value);
                                Drive = logical["Name"].ToString();
                                MainOSDrive = Drive;
                                PhysicalDrive = drive["DeviceID"].ToString();
                                MainOSPhysicalDisk = PhysicalDrive;
                                MAINOS_DRIVE = logical["Name"].ToString();
                                MAINOS_PHYSICAL = PhysicalDrive;
                                MAINOS_NAME = Label;
                                VolumeLabel = Label;
                                MainOSLabel = VolumeLabel;
                                IsBootedToMSC = true;



                                MAINOS_DETAIL = $"MAINOS FOUND: PhysicalDrive: {MAINOS_PHYSICAL} | Drive: {MAINOS_DRIVE} | Size: {((long)MAINOS_SIZE).ToFileSize()}";
                                BackupOutput.Text += $"MAINOS FOUND: PhysicalDrive: {MAINOS_PHYSICAL} | Drive: {MAINOS_DRIVE} | Size: {((long)MAINOS_SIZE).ToFileSize()}\n";
                                Debug.WriteLine($"MAINOS FOUND: PhysicalDrive: {MAINOS_PHYSICAL} | Drive: {MAINOS_DRIVE} | Size: {((long)MAINOS_SIZE).ToFileSize()}");
                            }
                            if (string.Equals(Label, "MainOS", StringComparison.CurrentCultureIgnoreCase))
                            {
                                Drive = logical["Name"].ToString();
                                MainOSDrive = Drive;
                                PhysicalDrive = drive["DeviceID"].ToString();
                                MainOSPhysicalDisk = PhysicalDrive;
                                MAINOS_DRIVE = logical["Name"].ToString();
                                MAINOS_PHYSICAL = PhysicalDrive;
                                MAINOS_NAME = Label;
                                VolumeLabel = Label;
                                MainOSLabel = VolumeLabel;
                                IsBootedToMSC = true;
                                MAINOS_DETAIL = $"MAINOS FOUND: PhysicalDrive: {MAINOS_PHYSICAL} | Drive: {MAINOS_DRIVE} | Size: {((long)MAINOS_SIZE).ToFileSize()}";
                                // BackupOutput.Text += $"MAINOS FOUND: PhysicalDrive: {MAINOS_PHYSICAL} | Drive: {MAINOS_DRIVE} | Size: {((long)MAINOS_SIZE).ToFileSize()}\n";
                                Debug.WriteLine($"MAINOS FOUND: PhysicalDrive: {MAINOS_PHYSICAL} | Drive: {MAINOS_DRIVE} | Size: {((long)MAINOS_SIZE).ToFileSize()}");
                                break;
                            }
                        }
                    }

                    if (string.Equals(Label, "MainOS", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Drive = logical["Name"].ToString();
                        MainOSDrive = Drive;
                        // PhysicalDrive = drive["DeviceID"].ToString();
                        MAINOS_DRIVE = logical["Name"].ToString();
                        // MAINOS_PHYSICAL = PhysicalDrive;
                        MAINOS_NAME = Label;
                        MainOSPhysicalDisk = "N/A";
                        VolumeLabel = Label;
                        MainOSLabel = VolumeLabel;
                        IsBootedToMSC = true;
                        // Debug.WriteLine($"{Drive}\n{VolumeLabel}");
                        break;
                    }

                }
            }
        }


        /// <summary>
        /// Mass Storage config/settings function (Registry Backup and Editing)
        /// </summary>
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
                    RegistryKey CamSoundSetting = registry2.CreateSubKey(@"Microsoft\Photos\OEM", RegistryKeyPermissionCheck.ReadSubTree);
                    RegistryKey WifiSoundSettingon = registry2.CreateSubKey(@"Microsoft\EventSounds\Sounds\WiFiConnected", RegistryKeyPermissionCheck.ReadSubTree);
                    RegistryKey WifiSoundSettingoff = registry2.CreateSubKey(@"Microsoft\EventSounds\Sounds\WiFiDisonnected", RegistryKeyPermissionCheck.ReadSubTree);
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

                    // Sound Settings
                    if (CamSoundSetting != null)
                    {
                        if (WifiSoundSettingon != null)
                        {

                            if (CamSoundSetting.GetValue("ShutterSoundUnlocked").ToString() != "1")
                            {
                                if (WifiSoundSettingon.GetValue("Disabled").ToString() != "0")
                                {
                                    ExtraSoundSetting.IsChecked = false;
                                }
                            }
                            else
                            {

                                ExtraSoundSetting.IsChecked = true;
                            }
                        }
                    }
                    else
                    {
                        ExtraSoundSetting.IsChecked = false;
                    }
                }

            }
            catch
            {
                IsRegistryMounted = false;
                BasicTweakPageHeader.Text = "Device not found, Make sure you are in Mass Storage Mode";
            }

        }

        // EFIESP_PHYSICAL, 0
        // EFIESP_NAME, 1
        // EFIESP_DRIVE, 2
        // EFIESP_FILESYSTEM, 3
        // DATA_PHYSICAL, 4
        // DATA_NAME, 5
        // DATA_DRIVE, 6
        // DATA_FILESYSTEM, 7
        // MAINOS_PHYSICAL, 8
        // MAINOS_NAME, 9
        // MAINOS_DRIVE, 10
        // MAINOS_FILESYSTEM 11
        List<string> foundDriveLetters;
        BackgroundWorker worker2 = new BackgroundWorker();
        private string[] CheckForWPDisks(string physicaldrive, string mainosdrive, string mainosname)
        {
            foundDriveLetters = null;

            string[] foundPartitions = WPDiskChecker.CheckForDiskInfo();
            if (foundPartitions == null)
            {
                Debug.WriteLine("No Partitions Detected");
            }
            else
            {
                string EFIESP_DRIVE = foundPartitions[0];
                string DATA_DRIVE = foundPartitions[1];
                string MAINOS_DRIVE = foundPartitions[2];
                string EFIESP_PHYSICAL = foundPartitions[3];
                string DATA_PHYSICAL = foundPartitions[4];
                string MAINOS_PHYSICAL = foundPartitions[5];
                string EFIESP_NAME = foundPartitions[6];
                string DATA_NAME = foundPartitions[7];
                string MAINOS_NAME = foundPartitions[8];
                string EFIESP_FILESYSTEM = foundPartitions[9];
                string DATA_FILESYSTEM = foundPartitions[10];
                string MAINOS_FILESYSTEM = foundPartitions[11];
                if (MAINOS_PHYSICAL != MainOSPhysicalDisk)
                {
                    MessageBox.Show("PhysicalDrive doesn't match already found PhysicalDrive");
                    return null;
                }
                else
                {
                    if (MAINOS_DRIVE != MainOSDrive)
                    {
                        MessageBox.Show("Found MainOS Drive doesn't match already detected MainOS Info");
                        return null;
                    }
                    else
                    {
                        foundDriveLetters.Add(EFIESP_DRIVE);
                        foundDriveLetters.Add(MAINOS_DRIVE);
                        foundDriveLetters.Add(DATA_DRIVE);

                    }
                }
            }
            return foundDriveLetters.ToArray();
        }
        #endregion

        private async void StartBackupBtn_Click(object sender, RoutedEventArgs e)
        {


            if (worker.IsBusy == true)
            {
                MessageBox.Show("Please try again later, Background Tasks are still running");
            } else
            {
                BackupProgressBar.IsIndeterminate = true;
                DisableUIButtons();
                worker2.RunWorkerAsync();
            }


            
           
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (IsCancelBackupClicked != true)
            {
                MessageBox.Show("Successfully dumped partitions");
                BackupOutput.Text = "Finished.\n\n" +
                    "You can now close this app/Safely remove your device as usual";
            }
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            BackupOutput.Text = e.UserState as string;
        }
        Process dmainos = new Process();
        Process ddata = new Process();
        Process defi = new Process();
        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {


            string outputfolder = ChosenDumpPath;
            try
            {


                if (IsMainOSSelected == true)
                {

                    Dispatcher.Invoke(new Action(() =>
                    {
                        BackupOutput.Text = 
                        $"Starting MainOS Dump:\n\n" +
                        $"Output: {outputfolder}\\MainOS.img\n" +
                        $"Partition Size: {((long)MAINOS_SIZE).ToFileSize()}\n" +
                        $"\nThis will take time, Please Wait";
                        DisableUIButtons();
                        BackupProgressBar.IsIndeterminate = true;
                    }), DispatcherPriority.ContextIdle);

                    Debug.WriteLine("Starting MAINOS Dump");


                    dmainos.StartInfo.FileName = @".\\AppData\\bin\\dd.exe";
                    dmainos.StartInfo.Arguments = $@" if=\\.\{MAINOS_DRIVE.ToLower()}" + $" of=\"{outputfolder}\\MainOS.img\" bs=1M --progress";
                    Debug.WriteLine($".\\AppData\\bin\\dd.exe" + $@" if=\\.\{MAINOS_DRIVE.ToLower()}" + $" of=\"{outputfolder}\\MainOS.img\" bs=1M --progress");
                  /*  dmainos.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    dmainos.StartInfo.CreateNoWindow = false;
                    dmainos.StartInfo.RedirectStandardOutput = false;
                    dmainos.OutputDataReceived += new DataReceivedEventHandler(Dmainos_OutputDataReceived);
                    dmainos.StartInfo.RedirectStandardInput = false;
                    dmainos.ErrorDataReceived += Dmainos_OutputDataReceived;
                    dmainos.StartInfo.RedirectStandardError = false; */
                    dmainos.Start();
                    


                    while(dmainos.HasExited != true)
                    {
                        if (IsCancelBackupClicked == true)
                        {
                            e.Cancel = true;
                            dmainos.Close();
                            dmainos.Kill();
                           
                        }
                    }

                    dmainos.WaitForExit();

                  
                    dmainos.Close();
                   

                }
                



                if (IsDataSelected == true)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        BackupOutput.Text =
                        $"Starting Data Dump:\n\n" +
                        $"Output: {outputfolder}\\Data.img\n" +
                        $"Partition Size: {((long)DATA_SIZE).ToFileSize()}\n" +
                        $"\nThis will take time, Please Wait";
                        DisableUIButtons();
                        BackupProgressBar.IsIndeterminate = true;
                    }), DispatcherPriority.ContextIdle);

                    Debug.WriteLine("Starting DATA Dump");


                    ddata.StartInfo.FileName = @".\AppData\bin\dd.exe";
                    ddata.StartInfo.Arguments = $@" if=\\.\{DATA_DRIVE.ToLower()}" + $" of=\"{outputfolder}\\Data.img\" bs=1M --progress";
                    Debug.WriteLine(@$"Launching dd.exe  if=\\.\{DATA_DRIVE.ToLower()} of={ outputfolder}\Data.img");
                    /*ddata.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    ddata.StartInfo.CreateNoWindow = false;
                    ddata.StartInfo.RedirectStandardOutput = false; */
                    ddata.Start();

                    while (ddata.HasExited != true)
                    {
                        if (IsCancelBackupClicked == true)
                        {
                            e.Cancel = true;
                            ddata.Kill();
                            ddata.Close();
                        }
                    }

                    ddata.WaitForExit();


                }


                if (IsEFISelected == true)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        BackupOutput.Text =
                        $"Starting EFIESP Dump:\n\n" +
                        $"Output: {outputfolder}\\EFIESP.img\n" +
                        $"Partition Size: {((long)EFIESP_SIZE).ToFileSize()}\n" +
                        $"\nThis will take time, Please Wait";
                        DisableUIButtons();
                        BackupProgressBar.IsIndeterminate = true;
                    }), DispatcherPriority.ContextIdle);

                    Debug.WriteLine("Starting EFIESP Dump");


                    defi.StartInfo.FileName = @".\AppData\bin\dd.exe";
                    defi.StartInfo.Arguments = $@" if=\\.\{EFIESP_DRIVE.ToLower()}" + $" of=\"{outputfolder}\\EFIESP.img\" bs=1M --progress";
                    Debug.WriteLine(@$"Launching dd.exe if=\\.\{EFIESP_DRIVE.ToLower()} of={ outputfolder}\EFIESP.img");
                    //defi.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                   // defi.StartInfo.CreateNoWindow = false;
                    defi.Start();


                    while (defi.HasExited != true)
                    {
                        if (IsCancelBackupClicked == true)
                        {
                            e.Cancel = true;
                            defi.Kill();
                            defi.Close();
                        }
                    }
                    defi.WaitForExit();

                }

                Dispatcher.Invoke(new Action(() =>
                {
                    EnableUIButtons();
                    BackupProgressBar.IsIndeterminate = false;
                }), DispatcherPriority.ContextIdle);

            }
            catch (Exception ex)
            {
                e.Cancel = true;
                dmainos.Kill();
                ddata.Kill();
                defi.Kill();
            }



        }

        private void Automator_StandardInputRead(object sender, ConsoleInputReadEventArgs e)
        {
            string result = e.Input;
            BackupOutput.Text = result;
        }

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
            RegistryKey CamSoundSetting = registry2.CreateSubKey(@"Microsoft\Photos\OEM", RegistryKeyPermissionCheck.ReadWriteSubTree);
            RegistryKey WifiSoundSettingon = registry2.CreateSubKey(@"Microsoft\EventSounds\Sounds\WiFiConnected", RegistryKeyPermissionCheck.ReadWriteSubTree);
            RegistryKey WifiSoundSettingoff = registry2.CreateSubKey(@"Microsoft\EventSounds\Sounds\WiFiDisonnected", RegistryKeyPermissionCheck.ReadWriteSubTree);
            if (ExtraSoundSetting.IsChecked == true)
            {
                CamSoundSetting.SetValue("ShutterSoundUnlocked", 1, RegistryValueKind.DWord);
                WifiSoundSettingon.SetValue("Disabled", 0, RegistryValueKind.DWord);
                WifiSoundSettingoff.SetValue("Disabled", 0, RegistryValueKind.DWord);
            }
            else
            {
                CamSoundSetting.SetValue("ShutterSoundUnlocked", 0, RegistryValueKind.DWord);
                WifiSoundSettingon.SetValue("Disabled", 1, RegistryValueKind.DWord);
                WifiSoundSettingoff.SetValue("Disabled", 1, RegistryValueKind.DWord);
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
            /*
            if (worker2.IsBusy)
            {
                MessageBox.Show("Full Disk Backup is still running");
                e.Cancel = true;
            }
            if (worker.IsBusy)
            {
                MessageBox.Show("Partition Backup is still running");
                e.Cancel = true;
            } */
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {

        }






        string FFUFilePath;
        private void FFULoadBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFFU = new OpenFileDialog();
            openFFU.Filter = "Full Flash Update File (*.ffu)|*.ffu";
            openFFU.RestoreDirectory = true;
            openFFU.Title = "Load Windows Phone FFU File";
            openFFU.DefaultExt = "ffu";
            openFFU.ShowDialog();
            if (openFFU == null)
            {

            }
            else
            {
                FFUFilePath = openFFU.FileName;
                LoadFFU(openFFU.FileName);
            }

        }

        private void LoadFFU(string ffufile)
        {
            var stream = ffufile;
            var selectedFFU = new FfuFile(stream);
            ReadFFU(selectedFFU);
        }
        string FFUManifestData;
        FfuFile file;
        private void ReadFFU(FfuFile ffuFile)
        {
            FFUManifestData = String.Empty;
            file = ffuFile;
            //Console.WriteLine("FFU input file: " + input);
            FFUInfoOutput.Text = " Catalog Size: " + file.SignedCatalog.Length;
            FFUInfoOutput.Text += "\n Chunk Size In Kb: " + file.ChunkSizeInKb;
            FFUInfoOutput.Text += "\n Hash Algorithm Type: " + file.HashAlgorithmType;
            FFUInfoOutput.Text += "\n Hash Table Size:" + file.HashTable.Length;
            FFUInfoOutput.Text += "\n Platform Id: " + file.PlatformId;
            FFUInfoOutput.Text += "\n FFU Version: " + file.Version;
            FFUInfoOutput.Text += "\n Storage Version: " + file.StoreVersion;
            FFUInfoOutput.Text += "\n Stores Count: " + file.Stores.Count;
            FFUInfoOutput.Text += "\n Block Size In Bytes: " + file.BlockSizeInBytes;
            FFUInfoOutput.Text += "\n First Store Payload Size: " + file.FirstStore.PayloadSizeInBytes;
            FFUInfoOutput.Text += "\n First Store Block Count: " + file.FirstStore.BlockCount;
            FFUInfoOutput.Text += "\n First Store Max Block Index: " + file.FirstStore.MaxBlockIndex;
            FFUInfoOutput.Text += "\n First Store Target Size In Bytes: " + file.FirstStore.TargetSizeInBytes;
            FFUManifestOutput.Text = file.Manifest;
            FFUManifestData = file.Manifest;
            if (FFUManifestData.Contains("Windows Phone 8"))
            {

                SaveFFUAsVhdx.IsEnabled = true;
            }
            else
            {
                FFUInfoOutput.Text += $"\n\n Cannot convert W10M images to VHDX yet,\n Please use WPInternals to dump";
                SaveFFUAsVhdx.IsEnabled = false;
            }

            SaveFFUManifestBtn.IsEnabled = true;

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SaveFFUAsVhdx_Click(object sender, RoutedEventArgs e)
        {

            try
            {

                //#########//
                Stream stream;
                SaveFileDialog saveFile = new SaveFileDialog();
                saveFile.Filter = "VHDX Files (*.vhdx)|*.vhdx";
                saveFile.RestoreDirectory = true;
                saveFile.DefaultExt = ".vhdx";
                Nullable<bool> result = saveFile.ShowDialog();
                if (result == true)
                {

                    if (saveFile != null)
                    {
                        if (file == null)
                        {
                            throw new NullReferenceException(nameof(file));
                        }
                        int percent = 0;
                        file.FirstStore.ProgressChanged += (sender, e) =>
                        {
                            var block = (FfuFileBlock)e.UserState;
                            if (e.ProgressPercentage != percent)
                            {
                                percent = e.ProgressPercentage;
                                if ((percent % 1) == 0)
                                {
                                    Dispatcher.Invoke(new Action(() =>
                                    {
                                        FFUProgress.Value = percent;
                                    }), DispatcherPriority.ContextIdle);

                                }
                                else
                                {
                                    //FFUInfoOutput.Text = ".";
                                }
                            }
                        };

                        string ext = System.IO.Path.GetExtension(saveFile.FileName).ToLowerInvariant();
                        if (ext == ".img")
                        {
                            // file.FirstStore.WriteRaw(output);
                        }
                        else
                        {

                            file.FirstStore.WriteVirtualDisk(saveFile.FileName);

                        }
                        Console.WriteLine("100%");
                        MessageBox.Show(saveFile.FileName + " file was written successfully.");
                    }
                }
                else

                {

                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine(ex.Message);
                MessageBox.Show($"An error occured.\n\nIs the FFU W10M, Corrupt or Custom?\n\n Only WP8 Images wre supported\n\n\n{ex.Message}");
            }
        }

        /// <summary>
        /// Onn Click, Ask save location and call process to start
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveFFUAsRaw_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.Filter = "IMG Files (*.img)|*.img";
            saveFile.RestoreDirectory = true;
            saveFile.DefaultExt = ".img";
            Nullable<bool> result = saveFile.ShowDialog();
            if (result == true)
            {
                SaveFFUAsRaw(FFUFilePath, saveFile.FileName);
            }


        }


        /// <summary>
        /// Save FFU as RAW Image, Functions for reading Security Header, Image Header and Store Header are provided by "FFUConvert" on Github
        /// </summary>
        /// <param name="FFUfile"></param>
        /// <param name="output"></param>
        public void SaveFFUAsRaw(string FFUfile, string output)
        {
            var ffupath = FFUfile;

            var imgpath = output;

            Debug.WriteLine("Input File: {0}", ffupath);
            Debug.WriteLine("Output File: {0}", imgpath);

            if (File.Exists(imgpath))
                File.Delete(imgpath);

            using (var ffufp = new BinaryReader(File.OpenRead(ffupath)))
            using (var imgfp = new BinaryWriter(File.OpenWrite(imgpath)))
            using (
              var logfp = new StreamWriter(File.Open("sharpffu2img.log", FileMode.Append, FileAccess.Write),
                Encoding.Default))
            {
                var ffuSecHeader = FFUImageHelper.ReadSecurityHeader(ffufp, logfp);

                FFUImageHelper.ReadImageHeader(ffufp, ffuSecHeader, logfp);

                var ffuStoreHeader = FFUImageHelper.ReadStoreHeader(ffufp, logfp);

                Debug.WriteLine("Block data entries begin: {0:x8}", ffufp.BaseStream.Position);
                logfp.WriteLine("Block data entries begin: {0:x8}", ffufp.BaseStream.Position);

                Debug.WriteLine("Block data entries end: {0:x8}",
                  ffufp.BaseStream.Position + ffuStoreHeader.dwWriteDescriptorLength);
                logfp.WriteLine("Block data entries end: {0:x8}",
                  ffufp.BaseStream.Position + ffuStoreHeader.dwWriteDescriptorLength);

                var blockdataaddress = ffufp.BaseStream.Position + ffuStoreHeader.dwWriteDescriptorLength;
                blockdataaddress = blockdataaddress + (ffuSecHeader.dwChunkSizeInKb * 1024) -
                                   (blockdataaddress % ((int)(ffuSecHeader.dwChunkSizeInKb * 1024)));

                logfp.WriteLine("Block data chunks begin: {0:x8}", blockdataaddress);
                Debug.WriteLine("Block data chunks begin: {0:x8}", blockdataaddress);

                var iBlock = 0u;
                var oldblockcount = 0u;

                while (iBlock < ffuStoreHeader.dwWriteDescriptorCount)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        int ii = (int)(iBlock * ffuStoreHeader.dwBlockSizeInBytes / 1024);
                        FFUProgress.Value = ii;
                    }), DispatcherPriority.ContextIdle);


                    Console.Write("\r{0} blocks, {1}KB written", iBlock, (iBlock * ffuStoreHeader.dwBlockSizeInBytes) / 1024);
                    logfp.WriteLine("Block data entry from: {0:x8}", ffufp.BaseStream.Position);

                    var currentBlockDataEntry = FFUImageHelper.ReadBlockDataEntry(ffufp);

                    if (Math.Abs(currentBlockDataEntry.dwBlockCount - oldblockcount) > 1)
                        Console.Write("\r{0} blocks, {1}KB written - Delay expected. Please wait.", iBlock,
                          (iBlock * ffuStoreHeader.dwBlockSizeInBytes) / 1024);

                    oldblockcount = currentBlockDataEntry.dwBlockCount;

                    FFUImageHelper.LogPropertyValues(currentBlockDataEntry, logfp);

                    var curraddress = ffufp.BaseStream.Position;

                    ffufp.BaseStream.Seek(blockdataaddress + (iBlock * ffuStoreHeader.dwBlockSizeInBytes), SeekOrigin.Begin);
                    imgfp.BaseStream.Seek(
                      (Convert.ToInt64(currentBlockDataEntry.dwBlockCount) * Convert.ToInt64(ffuStoreHeader.dwBlockSizeInBytes)),
                      SeekOrigin.Begin);
                    imgfp.Write(ffufp.ReadBytes((int)ffuStoreHeader.dwBlockSizeInBytes));

                    ffufp.BaseStream.Seek(curraddress, SeekOrigin.Begin);

                    iBlock = iBlock + 1;
                }

                Console.Write("\nWrite complete.");
                MessageBox.Show(ffupath + " was written successfully.");
            }

        }


        /// <summary>
        /// When Clicked, Save the Manifest data to plain text file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveFFUManifestBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.Filter = "CSV Files (*.csv)|*.csv";
            saveFile.RestoreDirectory = true;
            saveFile.DefaultExt = ".csv";
            Nullable<bool> result = saveFile.ShowDialog();
            if (result == true)
            {
                File.WriteAllText(saveFile.FileName, FFUManifestData);
                MessageBox.Show($"FFU Manifest saved to \"{saveFile.FileName}\"");
            }


        }


        bool IsMainOSSelected;
        bool IsDataSelected;
        bool IsEFISelected;
        string ChosenDumpPath;
        // https://github.com/simonkaps/ddresize/blob/master/Program.cs
        private async void PartitionsBackupBtn_Click(object sender, RoutedEventArgs e)
        {
            if (IsEFISelected != true && IsMainOSSelected != true && IsDataSelected != true)
            {
                MessageBox.Show("No Partitions selected");
            }
            else
            {
                BackupProgressBar.IsIndeterminate = true;
                BackupProgressBar.IsEnabled = true;
                var openfolder = new CommonOpenFileDialog();
                openfolder.Title = "Select output for saved partitions";
                openfolder.IsFolderPicker = true;
                openfolder.InitialDirectory = ".\\";
                openfolder.EnsurePathExists = true;

                if (openfolder.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    ChosenDumpPath = openfolder.FileName;
                    BackupOutput.Text = "Starting Partition Dumping.. Please Wait";
                    DisableUIButtons();
                    worker.RunWorkerAsync();
                    BackupProgressBar.IsIndeterminate = false;
                }

            }

        }





        private void Dmainos_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine(e.Data);

        }
        string backupmessage;
        private void Dmainos_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {

        }

        private void PartData_Checked(object sender, RoutedEventArgs e)
        {
            
                if (DATA_DRIVE == null)
                {
                    MessageBox.Show("Please assign Data a Drive Letter first");
                    IsDataSelected = false;
                    PartData.IsChecked = false;
                }
                else
                {
                    IsDataSelected = true;
                    Debug.WriteLine("Data Partition Selected");
                }

           
        }

        private void PartMainOS_Checked(object sender, RoutedEventArgs e)
        {
            if (MAINOS_DRIVE == null)
            {
                MessageBox.Show("Please assign MainOS a Drive Letter first");
                IsMainOSSelected = false;
                PartMainOS.IsChecked = false;
                Debug.WriteLine("MainOS Partition Deselected");
            }
            else
            {
                IsMainOSSelected = true;
                Debug.WriteLine("MainOS Partition Selected");
               
            }
        }

        private void PartEFIESP_Checked(object sender, RoutedEventArgs e)
        {
           
                if (EFIESP_DRIVE == null)
                {
                    MessageBox.Show("Please assign EFIESP a Drive Letter first");
                    IsEFISelected = false;
                    PartEFIESP.IsChecked = false;
                }
                else
                {
                    IsEFISelected = true;
                    Debug.WriteLine("EFIESP Partition Selected");
                }

        }



        private void StartWholeDiskDump(string PhysicalDisk, string output)
        {
            long diskSize = DiskInfo.GetPhysDiskSize(PhysicalDisk);
            Debug.WriteLine($"Selected Disk: {PhysicalDisk}");
            Debug.WriteLine($"Disk Size: {diskSize.ToFileSize().ToString()}");
            //Debug.WriteLine($"No. of Partitions = {partcount}");
            Debug.WriteLine("");
            Debug.WriteLine($"Selected Output: {output}");

            var reader = new BinaryReader(new DeviceStream(PhysicalDisk));

            var writer = new BinaryWriter(new FileStream(output, FileMode.Create));
            BackupProgressBar.Maximum = diskSize;
            var buffer = new byte[4096];

            int count;
            int loopcount = 0;
            try
            {

                //diskStream.CopyTo(fileStream);
                System.Console.WriteLine($"Writing Data to file, this will take several minutes");
                while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    writer.Write(buffer, 0, buffer.Length);
                    // System.Console.Write('.');
                    if (loopcount % 100 == 0)
                    {
                        BackupProgressBar.Value = (long)count;


                        // writer.Flush();
                    }
                    loopcount++;

                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine("");
                Debug.WriteLine(e.StackTrace);
                Debug.WriteLine("");
                Debug.WriteLine(e.Source);
                MessageBox.Show($"ERROR: Issue while writing to file {output}\n\n" +
                    $"{e.Message}\n\n{e.StackTrace}\n\n{e.Source}");
            }
            reader.Close();
            writer.Flush();
            writer.Close();
            Debug.WriteLine("Finished");

        }












        /// <summary>
        /// Enable UI Elements for Mass Storage Mode
        /// </summary>
        private void DisableUIButtons()
        {
            StartBackupBtn.IsEnabled = false;
            PartMainOS.IsEnabled = false;
            PartData.IsEnabled = false;
            PartEFIESP.IsEnabled = false;
            PartitionsBackupBtn.IsEnabled = false;
            OpenDiskMgmt.IsEnabled = false;
            HelpBtn.IsEnabled = false;
        }

        private void EnableUIButtons()
        {
            StartBackupBtn.IsEnabled = true;
            PartMainOS.IsEnabled = true;
            PartData.IsEnabled = true;
            PartEFIESP.IsEnabled = true;
            PartitionsBackupBtn.IsEnabled = true;
            OpenDiskMgmt.IsEnabled = true;
            HelpBtn.IsEnabled = true;
        }






        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
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

        private void HelpBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("To backup your entire Phone Storage to file, make sure you are connected in Mass Storage Mode.\n\n\nTo backup individual partitions, select the ones you want.\nMake sure the partitions ARE mounted in Disk Management");
        }

        private void OpenDiskMgmt_Click(object sender, RoutedEventArgs e)
        {
            Process diskmgmt = new Process();

            diskmgmt.StartInfo.FileName = @"cmd.exe";
            diskmgmt.StartInfo.Arguments = @$"/C diskmgmt.msc";
            diskmgmt.StartInfo.CreateNoWindow = true;
            diskmgmt.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            Debug.WriteLine(@$"Opening Windows Disk Management");
            diskmgmt.Start();

        }
        public bool IsCancelBackupClicked = false;
        private void CancelBackup_Click(object sender, RoutedEventArgs e)
        {
            IsCancelBackupClicked = true;
            if (worker.IsBusy)
            {
                worker.CancelAsync();
                worker.Dispose();
                try
                {
                    dmainos.Kill();
                    dmainos.Dispose();
                }
                catch (InvalidOperationException iv)
                {
                    Debug.WriteLine("MAINOS Dump not started");
                }

                try
                {

                    ddata.Kill();
                    ddata.Dispose();
                }
                catch (InvalidOperationException iv)
                {
                    Debug.WriteLine("DATA Dump not started");
                }

                try
                {
                    defi.Kill();
                    defi.Dispose();
                }
                catch (InvalidOperationException iv)
                {
                    Debug.WriteLine("EFIESP Dump not started");
                }


                
            }
            if (worker2.IsBusy)
            {
                worker2.CancelAsync();
                worker2.Dispose();
            }

            Dispatcher.Invoke(new Action(() =>
            {
                EnableUIButtons();
                BackupProgressBar.IsIndeterminate = false;
            }), DispatcherPriority.ContextIdle);



            MessageBox.Show("Process has been cancelled");
            IsCancelBackupClicked = false;
        }





        public class ConsoleInputReadEventArgs : EventArgs
        {
            public ConsoleInputReadEventArgs(string input)
            {
                this.Input = input;
            }

            public string Input { get; private set; }
        }

        public interface IConsoleAutomator
        {
            StreamWriter StandardInput { get; }

            event EventHandler<ConsoleInputReadEventArgs> StandardInputRead;
        }

        public abstract class ConsoleAutomatorBase : IConsoleAutomator
        {
            protected readonly StringBuilder inputAccumulator = new StringBuilder();

            protected readonly byte[] buffer = new byte[256];

            protected volatile bool stopAutomation;

            public StreamWriter StandardInput { get; protected set; }

            protected StreamReader StandardOutput { get; set; }

            protected StreamReader StandardError { get; set; }

            public event EventHandler<ConsoleInputReadEventArgs> StandardInputRead;

            protected void BeginReadAsync()
            {
                if (!this.stopAutomation)
                {
                    this.StandardOutput.BaseStream.BeginRead(this.buffer, 0, this.buffer.Length, this.ReadHappened, null);
                }
            }

            protected virtual void OnAutomationStopped()
            {
                this.stopAutomation = true;
                this.StandardOutput.DiscardBufferedData();
            }

            private void ReadHappened(IAsyncResult asyncResult)
            {
                var bytesRead = this.StandardOutput.BaseStream.EndRead(asyncResult);
                if (bytesRead == 0)
                {
                    this.OnAutomationStopped();
                    return;
                }

                var input = this.StandardOutput.CurrentEncoding.GetString(this.buffer, 0, bytesRead);
                this.inputAccumulator.Append(input);

                if (bytesRead < this.buffer.Length)
                {
                    this.OnInputRead(this.inputAccumulator.ToString());
                }

                this.BeginReadAsync();
            }

            private void OnInputRead(string input)
            {
                var handler = this.StandardInputRead;
                if (handler == null)
                {
                    return;
                }

                handler(this, new ConsoleInputReadEventArgs(input));
                this.inputAccumulator.Clear();
            }
        }

        public class ConsoleAutomator : ConsoleAutomatorBase, IConsoleAutomator
        {
            public ConsoleAutomator(StreamWriter standardInput, StreamReader standardOutput)
            {
                this.StandardInput = standardInput;
                this.StandardOutput = standardOutput;
            }

            public void StartAutomate()
            {
                this.stopAutomation = false;
                this.BeginReadAsync();
            }

            public void StopAutomation()
            {
                this.OnAutomationStopped();
            }
        }

        private void PartEFIESP_Unchecked(object sender, RoutedEventArgs e)
        {
            IsEFISelected = false;
        }

        private void PartMainOS_Unchecked(object sender, RoutedEventArgs e)
        {
            IsMainOSSelected = false;
        }

        private void PartData_Unchecked(object sender, RoutedEventArgs e)
        {
            IsDataSelected = false;
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


    public class DiskInfo
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateFile(
             [MarshalAs(UnmanagedType.LPTStr)] string filename,
             [MarshalAs(UnmanagedType.U4)] FileAccess access,
             [MarshalAs(UnmanagedType.U4)] FileShare share,
                    IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
             [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
             [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
                    IntPtr templateFile);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode,
            IntPtr lpInBuffer, uint nInBufferSize,
            IntPtr lpOutBuffer, uint nOutBufferSize,
            out uint lpBytesReturned, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        struct GET_LENGTH_INFORMATION
        {
            public long Length;
        };

        public static long GetPhysDiskSize(string physDeviceID)
        {
            uint IOCTL_DISK_GET_LENGTH_INFO = 0x0007405C;
            uint dwBytesReturned;

            //Example, physDeviceID == @"\\.\PHYSICALDRIVE1"
            IntPtr hVolume = CreateFile(physDeviceID, FileAccess.ReadWrite,
                FileShare.None, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);

            GET_LENGTH_INFORMATION outputInfo = new GET_LENGTH_INFORMATION();
            outputInfo.Length = 0;

            IntPtr outBuff = Marshal.AllocHGlobal(Marshal.SizeOf(outputInfo));

            bool devIOPass = DeviceIoControl(hVolume,
                                IOCTL_DISK_GET_LENGTH_INFO,
                                IntPtr.Zero, 0,
                                outBuff, (uint)Marshal.SizeOf(outputInfo),
                                out dwBytesReturned,
                                IntPtr.Zero);

            CloseHandle(hVolume);

            outputInfo = (GET_LENGTH_INFORMATION)Marshal.PtrToStructure(outBuff, typeof(GET_LENGTH_INFORMATION));

            Marshal.FreeHGlobal(hVolume);
            Marshal.FreeHGlobal(outBuff);

            return outputInfo.Length;
        }
    }

}
