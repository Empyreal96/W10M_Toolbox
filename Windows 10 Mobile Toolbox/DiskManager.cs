using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows;

namespace DiskManager
{
    public class DiskManager
    {
//--------------------------------------------------------------------------------------------------------
        [DllImport("kernel32.dll")]
        private static extern int CreateFile(
        string lpFileName, int dwDesiredAccess,
        int dwShareMode, int lpSecurityAttributes,
        int dwCreationDisposition, int dwFlagsAndAttributes,
        int hTemplateFile);

        [DllImport("kernel32.dll")]
        private static extern int CloseHandle(int hObject);

        [DllImport("kernel32.dll")]
        private static extern int ReadFile(
        int hFile,
        byte[] lpBuffer,
        int nNumberOfBytesToRead,
        ref int lpNumberOfBytesRead,
        int lpOverlapped);

        [DllImport("kernel32.dll")]
        private static extern int WriteFile(
        int hFile,
        byte[] lpBuffer,
        int nNumberOfBytesToWrite,
        ref int lpNumberOfBytesWritten,
        int lpOverlapped);

        [DllImport("kernel32.dll")]
        private static extern int SetFilePointer(
        int hFile,
        int nDistanceToMove,
        ref int lpDistanceToMoveHigh,
        uint nMoveMethod);

        const int BUFFERSIZE = 8335360;
        const int BYTES_PER_SECTOR = 4096;
        char diskLetter;
	    int diskHandle;
        string filePath;

        const int GENERIC_READ = unchecked((int)0x80000000);
        const int FILE_SHARE_READ = 1;
        const int FILE_SHARE_WRITE = 2;
        const int OPEN_EXISTING = 3;
        const Int64 INVALID_HANDLE_VALUE = -1;
        const int FILE_ATTRIBUTE_NORMAL = 0x80;
        const int FILE_BEGIN = 0;
//--------------------------------------------------------------------------------------------------------

	    public byte [] readDisk(char diskLetter)
        {
            this.diskLetter = diskLetter;
            createDiskHandle();

            int lpNumberOfBytesRead=0;
            byte [] buf = new byte[BUFFERSIZE];
            if (ReadFile(diskHandle, buf, BUFFERSIZE, ref lpNumberOfBytesRead, 0) == 0)
            {

                /*TO DO:
                throw and handle exceptions
                Terminal failure: Unable to read from disk.
                */
                CloseHandle(diskHandle);
                throw new Exception("ERROR MESSAGE");

            }
            return buf;
        }

        public void writeToDisk(byte [] sectorBuffer, int sectorNumber)
        {
            int nNumberOfBytesToWrite = BYTES_PER_SECTOR;
            int lpNumberOfBytesWritten = 0;
            int lpDistanceToMoveHigh = 0;
            
            SetFilePointer(diskHandle, BYTES_PER_SECTOR * sectorNumber, ref lpDistanceToMoveHigh, FILE_BEGIN);

            if (WriteFile(diskHandle, sectorBuffer, nNumberOfBytesToWrite, ref lpNumberOfBytesWritten, 0) == 0)
            {
                /*TO DO:
                throw and handle exceptions
                Terminal failure: Unable to write to disk.
                */
                throw new Exception("ERROR MESSAGE");

            }
        }

	    public void createImage(string filePath, byte [] buf)
        {
            this.filePath = filePath;
            FileInfo fileInfo = new FileInfo(filePath);
            bool exists = System.IO.Directory.Exists(fileInfo.Directory.ToString());
           
            if (!exists)
            {
                System.IO.Directory.CreateDirectory(fileInfo.Directory.ToString());
            }
            //File.WriteAllBytes(filePath, buf);
           BinaryWriter write = new BinaryWriter(new FileStream(filePath, FileMode.Create));
            write.Write(buf, 0, buf.Length);
            write.Flush();
            write.Close();

            }
        

	    public void createDiskHandle()
        {
            diskHandle = CreateFile(
            "\\\\.\\"+diskLetter+":",
            GENERIC_READ,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            0,
            OPEN_EXISTING,
            FILE_ATTRIBUTE_NORMAL,
            0);

            if (diskHandle == INVALID_HANDLE_VALUE)
            {
                /*TO DO:
                    throw and handle exceptions
                */
                throw new Exception("ERROR MESSAGE");
            }
        }
    }
}
