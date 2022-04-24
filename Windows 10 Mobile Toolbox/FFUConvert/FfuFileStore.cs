using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace FfuConvert
{
    public sealed class FfuFileStore
    {
        internal List<FfuFile.WriteDescriptor> _descriptors = new List<FfuFile.WriteDescriptor>();
        private FfuFile _file;

        internal FfuFileStore(FfuFile file)
        {
            _file = file;
        }

        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        public int Index { get; internal set; } // 1-based
        public long PayloadPosition { get; internal set; }
        public string DevicePath { get; internal set; }
        public int BlockCount => _descriptors.Select(d => d.BlockCount).Sum();
        public long PayloadSizeInBytes => BlockCount * _file.BlockSizeInBytes;
        public long TargetSizeInBytes => MaxBlockIndex * (long)_file.BlockSizeInBytes;
        public int MaxBlockIndex => _descriptors.Select(d => d.Locations.Where(l1 => l1.dwDiskAccessMethod == FfuFile.DISK_ACCESS_METHOD.DISK_BEGIN).Select(l2 => l2.dwBlockIndex)
            .DefaultIfEmpty().Max())
            .DefaultIfEmpty().Max();

        private void OnProgressChanged(FfuFileBlock block)
        {
            ProgressChanged?.Invoke(this, new ProgressChangedEventArgs((100 * block.DescriptorIndex) / _descriptors.Count, block));
        }

        public IEnumerable<FfuFileBlock> EnumerateBlocks()
        {
            var block = new byte[_file.BlockSizeInBytes];
            for (int i = 0; i < _descriptors.Count; i++)
            {
                // move to block #i
                _file.SeekBegin(PayloadPosition + i * _file.BlockSizeInBytes);

                if (!_file.TryRead(block))
                    throw new InvalidDataException();

                var desc = _descriptors[i];
                foreach (var location in desc.Locations)
                {
                    var bl = new FfuFileBlock
                    {
                        DescriptorIndex = i,
                        Data = block
                    };
                    switch (location.dwDiskAccessMethod)
                    {
                        case FfuFile.DISK_ACCESS_METHOD.DISK_BEGIN:
                            bl.TargetSeekOrigin = SeekOrigin.Begin;
                            bl.TargetSeekOffset = location.dwBlockIndex * (long)_file.BlockSizeInBytes;
                            break;

                        case FfuFile.DISK_ACCESS_METHOD.DISK_END:
                            bl.TargetSeekOrigin = SeekOrigin.End;
                            bl.TargetSeekOffset = -((location.dwBlockIndex + 1) * (long)_file.BlockSizeInBytes);
                            break;

                        default:
                            throw new NotSupportedException();
                    }
                    yield return bl;
                }
            }
        }

        public void WriteVirtualDisk(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            if (Path.GetExtension(filePath).ToLowerInvariant() != ".vhdx")
                throw new NotSupportedException();

            using (var file = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            {
                using (var vd = DiscUtils.Vhdx.Disk.InitializeDynamic(file, DiscUtils.Streams.Ownership.None, TargetSizeInBytes))
                {
                    foreach (var block in EnumerateBlocks())
                    {
                        Debug.WriteLine($"(Seek) Target Offset: {block.TargetSeekOffset}   |   Target Origin: {block.TargetSeekOrigin}");
                        vd.Content.Seek(block.TargetSeekOffset, block.TargetSeekOrigin);
                        vd.Content.Write(block.Data, 0, block.Data.Length);
                        OnProgressChanged(block);
                    }
                }
            }
        }

        //Not Finished
        public void WriteDumpDisk(string filePath, FfuFile ffu)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));
            if (ffu == null)
            {
                throw new ArgumentNullException(nameof(ffu));
            }

            if (Path.GetExtension(filePath).ToLowerInvariant() != ".img")
                throw new NotSupportedException();

            using (var file = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            {
                using (var vd = DiscUtils.Vhdx.Disk.InitializeDynamic(file, DiscUtils.Streams.Ownership.None, TargetSizeInBytes))
                {
                    foreach (var block in EnumerateBlocks())
                    {
                        vd.Content.Seek(block.TargetSeekOffset, block.TargetSeekOrigin);
                        vd.Content.Write(block.Data, 0, block.Data.Length);
                        OnProgressChanged(block);
                    }
                }
            }
        }

        public void WriteRaw(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                file.SetLength(TargetSizeInBytes);
                foreach (var block in EnumerateBlocks())
                {
                    file.Seek(block.TargetSeekOffset, block.TargetSeekOrigin);
                    file.Write(block.Data, 0, block.Data.Length);
                    OnProgressChanged(block);
                }
            }
        }
    }
}
