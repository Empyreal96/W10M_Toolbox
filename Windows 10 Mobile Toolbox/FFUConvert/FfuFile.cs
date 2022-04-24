using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text;

namespace FfuConvert
{
    public sealed class FfuFile : IDisposable
    {
        private HashAlgorithmType _hashAlgorithmType = HashAlgorithmType.None;
        private int _chunkSizeInKb = -1;
        private int _catalogSize = -1;
        private int _hashTableSize = -1;
        private int _storeMajorVersion = -1;
        private int _storeMinorVersion = -1;
        private int _blockSizeInBytes = -1;
        public int _majorVersion = -1;
        public int _minorVersion = -1;
        private bool _leaveOpen;
        private byte[] _signedCatalog;
        private byte[] _hashtable;
        private string _manifest;
        private string _platformId;
        private List<FfuFileStore> _stores;

        public FfuFile(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            Open(File.OpenRead(filePath), false);
        }

        public FfuFile(Stream stream)
            : this(stream, false)
        {
        }

        public FfuFile(Stream stream, bool leaveOpen)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            Open(stream, leaveOpen);
        }

        private void Open(Stream stream, bool leaveOpen)
        {
            _leaveOpen = leaveOpen;
            BaseStream = stream;
        }

        public static Stream BaseStream { get; private set; }
        public HashAlgorithmType HashAlgorithmType { get { LoadSecurityHeader(); return _hashAlgorithmType; } }
        public int ChunkSizeInKb { get { LoadSecurityHeader(); return _chunkSizeInKb; } }
        public int StoreMajorVersion { get { LoadStoreHeaders(); return _storeMajorVersion; } }
        public int StoreMinorVersion { get { LoadStoreHeaders(); return _storeMinorVersion; } }
        public int MajorVersion { get { LoadStoreHeaders(); return _majorVersion; } }
        public int MinorVersion { get { LoadStoreHeaders(); return _minorVersion; } }
        public int BlockSizeInBytes { get { LoadStoreHeaders(); return _blockSizeInBytes; } }
        public string PlatformId { get { LoadStoreHeaders(); return _platformId; } }
        public byte[] SignedCatalog { get { LoadSignedCatalog(); return _signedCatalog; } }
        public byte[] HashTable { get { LoadHashTable(); return _hashtable; } }
        public string Manifest { get { LoadImageHeader(); return _manifest; } }
        public Version Version => new Version(MajorVersion, MinorVersion);
        public Version StoreVersion => new Version(StoreMajorVersion, StoreMinorVersion);
        public IReadOnlyList<FfuFileStore> Stores => _stores;
        public FfuFileStore FirstStore => _stores.FirstOrDefault(); // should always be there

        public void Dispose()
        {
            if (!_leaveOpen)
            {
                var stream = BaseStream;
                if (stream != null)
                {
                    stream.Dispose();
                    BaseStream = null;
                }
            }
        }

        private Stream CheckDisposed()
        {
            var stream = BaseStream;
            if (stream == null)
                throw new ObjectDisposedException(null);

            return stream;
        }

        private static T ToStructure<T>(byte[] bytes) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
        }

        private T Read<T>() where T : struct
        {
            if (!TryRead(out T value))
                throw new InvalidDataException();

            return value;
        }

        private bool TryRead<T>(out T value) where T : struct
        {
            if (!TryRead(Marshal.SizeOf<T>(), out byte[] bytes))
            {
                value = default(T);
                return false;
            }

            value = ToStructure<T>(bytes);
            return true;
        }

        private byte[] Read(int count)
        {
            if (!TryRead(count, out byte[] bytes))
                throw new InvalidDataException();

            return bytes;
        }

        private bool TryRead(int count, out byte[] bytes)
        {
            bytes = new byte[count];
            int total = 0;
            do
            {
                int read = CheckDisposed().Read(bytes, 0, bytes.Length);
                if (read == 0)
                {
                    bytes = null;
                    return false;
                }

                total += read;
            }
            while (total < count);
            return true;
        }

        internal bool TryRead(byte[] bytes)
        {
            int total = 0;
            do
            {
                int read = CheckDisposed().Read(bytes, 0, bytes.Length);
                if (read == 0)
                {
                    bytes = null;
                    return false;
                }

                total += read;
            }
            while (total < bytes.Length);
            return true;
        }

        internal void SeekBegin(long offset)
        {
            Debug.WriteLine($"(Seek) Offset {offset}  |  {SeekOrigin.Begin}");
            if (CheckDisposed().Seek(offset, SeekOrigin.Begin) != offset)
                  throw new InvalidDataException();
        }

        private void SeekCurrent(long offset) => SeekBegin(CheckDisposed().Position + offset);

        private void LoadSecurityHeader()
        {
            if (_hashAlgorithmType != HashAlgorithmType.None)
                return;

            var header = Read<SECURITY_HEADER>();
            if (Encoding.ASCII.GetString(header.signature) != "SignedImage ")
                throw new InvalidDataException();

            _hashAlgorithmType = (HashAlgorithmType)header.dwAlgId;
            _chunkSizeInKb = header.dwChunkSizeInKb;
            _catalogSize = header.dwCatalogSize;
            _hashTableSize = header.dwHashTableSize;
        }

        private void LoadSignedCatalog()
        {
            if (_signedCatalog != null)
                return;

            LoadSecurityHeader();
            _signedCatalog = Read(_catalogSize);
        }

        private void ReadPadding()
        {
            long pos = BaseStream.Position;
            long padding = pos % (_chunkSizeInKb * 1024);
            if (padding == 0)
                return;

            padding = _chunkSizeInKb * 1024 - padding;
            SeekCurrent(padding);
        }

        private void LoadHashTable()
        {
            if (_hashtable != null)
                return;

            LoadSignedCatalog();
            _hashtable = Read(_hashTableSize);
            ReadPadding();
        }

        private void LoadImageHeader()
        {
            if (_manifest != null)
                return;

            var header = Read<IMAGE_HEADER>();
            if (Encoding.ASCII.GetString(header.Signature) != "ImageFlash  ")
                throw new InvalidDataException();

            var manifest = Read(header.ManifestLength);
            _manifest = Encoding.ASCII.GetString(manifest).TrimEnd('\0');
            ReadPadding();
        }

        private void LoadStoreHeaders()
        {
            if (_stores != null)
                return;

            LoadImageHeader();
            _stores = new List<FfuFileStore>();
            var header2 = new STORE_HEADER_V_2_0();
            var header1 = new STORE_HEADER_V_1_0();
            int storeCount = 1;
            do
            {
                var store = new FfuFileStore(this);
                _stores.Add(store);
                store.Index = _stores.Count;

                // read store header, handle V1 and V2+
                if (_storeMajorVersion < 0)
                {
                    header1 = Read<STORE_HEADER_V_1_0>();
                    _storeMajorVersion = header1.MajorVersion;
                    if (_storeMajorVersion > 1)
                    {
                        SeekCurrent(-Marshal.SizeOf<STORE_HEADER_V_1_0>());
                        header2 = Read<STORE_HEADER_V_2_0>();
                        storeCount = header2.NumOfStores;
                    }

                    // read ffu global properties
                    _storeMinorVersion = _storeMajorVersion > 1 ? header2.MinorVersion : header1.MinorVersion;
                    _majorVersion = _storeMajorVersion > 1 ? header2.FullFlashMajorVersion : header1.FullFlashMajorVersion;
                    _minorVersion = _storeMajorVersion > 1 ? header2.FullFlashMinorVersion : header1.FullFlashMinorVersion;
                    _blockSizeInBytes = _storeMajorVersion > 1 ? header2.dwBlockSizeInBytes : header1.dwBlockSizeInBytes;
                    _platformId = Encoding.ASCII.GetString(_storeMajorVersion > 1 ? header2.szPlatformId : header1.szPlatformId).TrimEnd('\0');
                }
                else
                {
                    if (_storeMajorVersion > 1)
                    {
                        header2 = Read<STORE_HEADER_V_2_0>();
                    }
                    else
                    {
                        header1 = Read<STORE_HEADER_V_1_0>();
                    }
                }

                // read ffu store properties
                if (_storeMajorVersion > 1)
                {
                    var pathBytes = Read(header2.DevicePathLength * 2);
                    store.DevicePath = Encoding.Unicode.GetString(pathBytes).TrimEnd('\0');
                }

                var validateDescriptorCount = _storeMajorVersion > 1 ? header2.dwValidateDescriptorCount : header1.dwValidateDescriptorCount;
                var writeDescriptorCount = _storeMajorVersion > 1 ? header2.dwWriteDescriptorCount : header1.dwWriteDescriptorCount;

                for (int i = 0; i < validateDescriptorCount; i++)
                {
                    // VALIDATION_ENTRY
                    int sectorIndex = Read<int>();
                    int sectorOffset = Read<int>();
                    int byteCount = Read<int>();
                    // right now, we don't use this
                    SeekCurrent(byteCount);
                }

                for (int i = 0; i < writeDescriptorCount; i++)
                {
                    // BLOCK_DATA_ENTRY
                    var desc = new WriteDescriptor();
                    int locationCount = Read<int>();
                    desc.BlockCount = Read<int>();
                    for (int j = 0; j < locationCount; j++)
                    {
                        desc.Locations.Add(Read<DISK_LOCATION>());
                    }
                    store._descriptors.Add(desc);
                }

                ReadPadding();
            }
            while (_stores.Count < storeCount);

            long position = BaseStream.Position;
            foreach (var store in _stores)
            {
                store.PayloadPosition = position;
                position += store.PayloadSizeInBytes;
            }
        }

        internal class WriteDescriptor
        {
            public int BlockCount;
            public List<DISK_LOCATION> Locations = new List<DISK_LOCATION>();

            public override string ToString() => BlockCount + ":" + string.Join(", ", Locations);
        }

        // https://docs.microsoft.com/en-us/windows-hardware/manufacture/mobile/ffu-image-format
        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_HEADER
        {
            public int cbSize;              // size of struct, overall
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            public byte[] signature;        // "SignedImage "
            public int dwChunkSizeInKb;     // size of a hashed chunk within the image
            public int dwAlgId;             // algorithm used to hash
            public int dwCatalogSize;       // size of catalog to validate
            public int dwHashTableSize;     // size of hash table
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_HEADER
        {
            public int cbSize;              // sizeof(ImageHeader)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            public byte[] Signature;        // "ImageFlash  "
            public int ManifestLength;      // in bytes
            public int dwChunkSize;         // Used only during image generation.
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct VALIDATION_ENTRY
        {
            public int dwSectorIndex;
            public int dwSectorOffset;
            public int dwByteCount;
            //BYTE rgCompareData[1];          // size is dwByteCount
        }

        internal enum DISK_ACCESS_METHOD
        {
            DISK_BEGIN = 0,
            DISK_END = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DISK_LOCATION
        {
            public DISK_ACCESS_METHOD dwDiskAccessMethod;
            public int dwBlockIndex;

            public override string ToString() => dwDiskAccessMethod + ":" + dwBlockIndex;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BLOCK_DATA_ENTRY
        {
            public int dwLocationCount;
            public int dwBlockCount;
            //DISK_LOCATION rgDiskLocations[1];
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct STORE_HEADER_V_1_0
        {
            public int dwUpdateType;                    // indicates partial or full flash
            public short MajorVersion;
            public short MinorVersion;                  // used to validate struct
            public short FullFlashMajorVersion;
            public short FullFlashMinorVersion;         // FFU version, i.e. the image format
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 192)]
            public byte[] szPlatformId;                 // string which indicates what device this FFU is intended to be written to
            public int dwBlockSizeInBytes;              // size of an image block in bytes – the device’s actual sector size may differ
            public int dwWriteDescriptorCount;          // number of write descriptors to iterate through
            public int dwWriteDescriptorLength;         // total size of all the write descriptors, in bytes (included so they can be read out up front and interpreted later)
            public int dwValidateDescriptorCount;       // number of validation descriptors to check
            public int dwValidateDescriptorLength;      // total size of all the validation descriptors, in bytes
            public int dwInitialTableIndex;             // block index in the payload of the initial (invalid) GPT
            public int dwInitialTableCount;             // count of blocks for the initial GPT, i.e. the GPT spans blockArray[idx..(idx + count -1)]
            public int dwFlashOnlyTableIndex;           // first block index in the payload of the flash-only GPT (included so safe flashing can be accomplished)
            public int dwFlashOnlyTableCount;           // count of blocks in the flash-only GPT
            public int dwFinalTableIndex;               // index in the table of the real GPT
            public int dwFinalTableCount;               // number of blocks in the real GPT
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct STORE_HEADER_V_2_0
        {
            public int dwUpdateType;                    // indicates partial or full flash
            public short MajorVersion;
            public short MinorVersion;                  // used to validate struct
            public short FullFlashMajorVersion;
            public short FullFlashMinorVersion;         // FFU version, i.e. the image format
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 192)]
            public byte[] szPlatformId;                 // string which indicates what device this FFU is intended to be written to
            public int dwBlockSizeInBytes;              // size of an image block in bytes – the device’s actual sector size may differ
            public int dwWriteDescriptorCount;          // number of write descriptors to iterate through
            public int dwWriteDescriptorLength;         // total size of all the write descriptors, in bytes (included so they can be read out up front and interpreted later)
            public int dwValidateDescriptorCount;       // number of validation descriptors to check
            public int dwValidateDescriptorLength;      // total size of all the validation descriptors, in bytes
            public int dwInitialTableIndex;             // block index in the payload of the initial (invalid) GPT
            public int dwInitialTableCount;             // count of blocks for the initial GPT, i.e. the GPT spans blockArray[idx..(idx + count -1)]
            public int dwFlashOnlyTableIndex;           // first block index in the payload of the flash-only GPT (included so safe flashing can be accomplished)
            public int dwFlashOnlyTableCount;           // count of blocks in the flash-only GPT
            public int dwFinalTableIndex;               // index in the table of the real GPT
            public int dwFinalTableCount;               // number of blocks in the real GPT
            public short NumOfStores;                   // Total number of stores (V2 only)
            public short StoreIndex;                    // Current store index, 1-based (V2 only)
            public long StorePayloadSize;               // Payload data only, excludes padding (V2 only)
            public short DevicePathLength;              // Length of the device path (V2 only)
            //CHAR16 DevicePath[1];                       // Device path has no NUL at then end (V2 only)
        }
    }
}
