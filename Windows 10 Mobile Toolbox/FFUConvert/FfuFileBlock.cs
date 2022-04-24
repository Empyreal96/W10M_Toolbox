using System.IO;

namespace FfuConvert
{
    public sealed class FfuFileBlock
    {
        internal FfuFileBlock()
        {
        }

        public int DescriptorIndex { get; internal set; }
        public SeekOrigin TargetSeekOrigin { get; internal set; }
        public long TargetSeekOffset { get; internal set; }
        public byte[] Data { get; internal set; }

        public override string ToString() => "[" + DescriptorIndex + "] " + TargetSeekOrigin + ":" + TargetSeekOffset + " (" + Data?.Length + ")";
    }
}
