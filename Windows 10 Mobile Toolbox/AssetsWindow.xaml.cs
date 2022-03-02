using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WPDevPortal;

namespace W10M_Toolbox
{
    /// <summary>
    /// Interaction logic for AssetsWindow.xaml
    /// </summary>
    public partial class AssetsWindow : Window
    {
        public AssetsWindow()
        {
            InitializeComponent();
            WPInternalsHelp.Text = "WPInternals is an Open-Source tool to allow users to Unlock/Relock Bootloader, Enable/Disable Root Access, Flash FFUs and more!";
        }
        static string wpiurl;
        static string wpifilename;
        static bool IsDLCompleted;
        private void WPInternalsDL_Click(object sender, RoutedEventArgs e)
        {
            if (System.Environment.Is64BitOperatingSystem == true)
            {
                
                wpiurl = "https://github.com/ReneLergner/WPinternals/releases/download/2.9.2/win-x64.zip";
                wpifilename = "win-x64.zip";
                if (File.Exists(@$".\AppData\bin\wpinternals\{wpifilename}") == true)
                {
                    File.Delete(@$".\AppData\bin\wpinternals\{wpifilename}");
                }
            }
            else
            {
                wpiurl = "https://github.com/ReneLergner/WPinternals/releases/download/2.9.2/win-x86.zip";
                wpifilename = "win-x86.zip";
                if (File.Exists(@$".\AppData\bin\wpinternals\{wpifilename}") == true)
                {
                    File.Delete(@$".\AppData\bin\wpinternals\{wpifilename}");
                }
            }
           

            AssetsProgress.IsEnabled = true;
            DLProgressText.IsEnabled = true;

            
                WebClient client = new WebClient();
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);
                client.DownloadFileAsync(new Uri(wpiurl), @$".\AppData\bin\wpinternals\{wpifilename}");
            

           

        }
        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            
                double bytesIn = double.Parse(e.BytesReceived.ToString());
                double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
                double percentage = bytesIn / totalBytes * 100;
                DLProgressText.Text = "Downloaded " + e.BytesReceived.ToFileSize() + " of " + e.TotalBytesToReceive.ToFileSize() + $" from: {wpiurl}";
            AssetsProgress.Value = int.Parse(Math.Truncate(percentage).ToString());
            
        }
        void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {

            DLProgressText.Text = "Download Completed";
            UnpackDownloadedFile(@$".\AppData\bin\wpinternals\{wpifilename}", @$".\AppData\bin\wpinternals\{wpifilename.Replace(".zip", "")}", ".zip");

            IsDLCompleted = true;
            
        }

        /// <summary>
        /// Unpack Downloaded File to AppData
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="outputpath"></param>
        /// <param name="filetype"></param>
        public async void UnpackDownloadedFile(string filepath, string outputpath, string filetype)
        {
            DLProgressText.Text = "Extracting to AppData";
            AssetsProgress.IsIndeterminate = true;
            if (!Directory.Exists(outputpath))
                {
                ZipFile.ExtractToDirectory(filepath, outputpath);
                DLProgressText.Text = "Finished";
                WPInternalsDL.IsEnabled = false;
                AssetsProgress.IsEnabled = false;
                DLProgressText.IsEnabled = true;
            } else
            {
                DLProgressText.Text = "Removing old data";
                Directory.Delete(outputpath, true);
                ZipFile.ExtractToDirectory(filepath, outputpath);
                DLProgressText.Text = "Finished";
                WPInternalsDL.IsEnabled = false;
                AssetsProgress.IsIndeterminate = false;
                AssetsProgress.IsEnabled = false;
            }

        }
    }
}
