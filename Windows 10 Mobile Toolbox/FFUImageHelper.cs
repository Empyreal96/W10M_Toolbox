using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// This is adapted from "Sharp.ffu2img"
/// </summary>
namespace W10M_Toolbox
{
    class FFUImageHelper
    {
        public struct SecurityHeader
        {
            public uint cbSize { get; set; }
            public string signature { get; set; }
            public uint dwChunkSizeInKb { get; set; }
            public uint dwAlgId { get; set; }
            public uint dwCatalogSize { get; set; }
            public uint dwHashTableSize { get; set; }
        }

        public struct ImageHeader
        {
            public uint cbSize { get; set; }
            public string signature { get; set; }
            public uint ManifestLength { get; set; }
            public uint dwChunkSize { get; set; }
        }

        public struct StoreHeader
        {
            public uint dwUpdateType { get; set; }
            public ushort MajorVersion { get; set; }
            public ushort MinorVersion { get; set; }
            public ushort FullFlashMajorVersion { get; set; }
            public ushort FullFlashMinorVersion { get; set; }
            public string szPlatformId { get; set; }
            public uint dwBlockSizeInBytes { get; set; }
            public uint dwWriteDescriptorCount { get; set; }
            public uint dwWriteDescriptorLength { get; set; }
            public uint dwValidateDescriptorCount { get; set; }
            public uint dwValidateDescriptorLength { get; set; }
            public uint dwInitialTableIndex { get; set; }
            public uint dwInitialTableCount { get; set; }
            public uint dwFlashOnlyTableIndex { get; set; }
            public uint dwFlashOnlyTableCount { get; set; }
            public uint dwFinalTableIndex { get; set; }
            public uint dwFinalTableCount { get; set; }
        }

        public struct BlockDataEntry
        {
            public uint dwDiskAccessMethod { get; set; }
            public uint dwBlockIndex { get; set; }
            public uint dwLocationCount { get; set; }
            public uint dwBlockCount { get; set; }
        }
        public static SecurityHeader ReadSecurityHeader(BinaryReader reader, StreamWriter logWriter)
        {
            logWriter.WriteLine("FFUSecHeader begin: {0:x8}", reader.BaseStream.Position);

            var result = new SecurityHeader
            {
                cbSize = reader.ReadUInt32(),
                signature = new string(reader.ReadChars(12)),
                dwChunkSizeInKb = reader.ReadUInt32(),
                dwAlgId = reader.ReadUInt32(),
                dwCatalogSize = reader.ReadUInt32(),
                dwHashTableSize = reader.ReadUInt32(),
            };

            if (!result.signature.Equals("SignedImage "))
            {
                logWriter.WriteLine("Exiting, incorrect signature: {0}", result.signature);
                throw new Exception(string.Format("Error: security header signature incorrect: {0}", result.signature));
            }

            LogPropertyValues(result, logWriter);

            reader.BaseStream.Seek(result.dwCatalogSize, SeekOrigin.Current);
            reader.BaseStream.Seek(result.dwHashTableSize, SeekOrigin.Current);
            GoToEndOfChunk(reader, (int)result.dwChunkSizeInKb);

            return result;
        }

        public static void ReadImageHeader(BinaryReader reader, SecurityHeader securityHeader,
          StreamWriter logWriter)
        {
            logWriter.WriteLine("FFUImgHeader begin: {0:x8}", reader.BaseStream.Position);

            var result = new ImageHeader
            {
                cbSize = reader.ReadUInt32(),
                signature = new string(reader.ReadChars(12)),
                ManifestLength = reader.ReadUInt32(),
                dwChunkSize = reader.ReadUInt32(),
            };

            if (!result.signature.Equals("ImageFlash  "))
            {
                logWriter.WriteLine("Exiting, incorrect signature: {0}", result.signature);
                throw new Exception(string.Format("Error: image header signature incorrect. {0}", result.signature));
            }

            LogPropertyValues(result, logWriter);

            reader.BaseStream.Seek(result.ManifestLength, SeekOrigin.Current);

            GoToEndOfChunk(reader, (int)securityHeader.dwChunkSizeInKb);
        }

        public static StoreHeader ReadStoreHeader(BinaryReader reader, StreamWriter logWriter)
        {
            logWriter.WriteLine("FFUStoreHeader begin: {0:x8}", reader.BaseStream.Position);

            var result = new StoreHeader
            {
                dwUpdateType = reader.ReadUInt32(),
                MajorVersion = reader.ReadUInt16(),
                MinorVersion = reader.ReadUInt16(),
                FullFlashMajorVersion = reader.ReadUInt16(),
                FullFlashMinorVersion = reader.ReadUInt16(),
                szPlatformId = new string(reader.ReadChars(192)),
                dwBlockSizeInBytes = reader.ReadUInt32(),
                dwWriteDescriptorCount = reader.ReadUInt32(),
                dwWriteDescriptorLength = reader.ReadUInt32(),
                dwValidateDescriptorCount = reader.ReadUInt32(),
                dwValidateDescriptorLength = reader.ReadUInt32(),
                dwInitialTableIndex = reader.ReadUInt32(),
                dwInitialTableCount = reader.ReadUInt32(),
                dwFlashOnlyTableIndex = reader.ReadUInt32(),
                dwFlashOnlyTableCount = reader.ReadUInt32(),
                dwFinalTableIndex = reader.ReadUInt32(),
                dwFinalTableCount = reader.ReadUInt32(),
            };

            LogPropertyValues(result, logWriter);

            reader.BaseStream.Seek(result.dwValidateDescriptorLength, SeekOrigin.Current);

            return result;
        }
        public static BlockDataEntry ReadBlockDataEntry(BinaryReader reader)
        {
            var result = new BlockDataEntry
            {
                dwDiskAccessMethod = reader.ReadUInt32(),
                dwBlockIndex = reader.ReadUInt32(),
                dwLocationCount = reader.ReadUInt32(),
                dwBlockCount = reader.ReadUInt32()
            };

            return result;
        }

        public static void GoToEndOfChunk(BinaryReader reader, Int32 chunkSizeinKb)
        {
            var remainderOfChunk = reader.BaseStream.Position % (chunkSizeinKb * 1024);
            var distanceToChunkEnd = (chunkSizeinKb * 1024) - remainderOfChunk;
            reader.BaseStream.Seek(distanceToChunkEnd, SeekOrigin.Current);
        }

        public static void LogPropertyValues<T>(T obj, StreamWriter logWriter) where T : struct
        {
            if (logWriter == null)
                return;

            foreach (var property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                logWriter.WriteLine("{0} = {1}", property.Name, property.GetValue(obj, null));
            }
        }
    }
}
