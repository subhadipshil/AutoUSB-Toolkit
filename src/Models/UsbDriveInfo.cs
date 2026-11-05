using System;

namespace AutoUSBToolkit.Models
{
    public class UsbDriveInfo
    {
        public string DriveLetter { get; set; }
        public string VolumeLabel { get; set; }
        public string FileSystem { get; set; }
        public long TotalSizeBytes { get; set; }
        public long FreeSizeBytes { get; set; }
        public uint ClusterSizeBytes { get; set; }
        public bool IsRemovable { get; set; }
        public bool IsUsb { get; set; }
        public int PhysicalDriveIndex { get; set; }
        public int PartitionNumber { get; set; }
        public string PartitionStyle { get; set; } // "MBR", "GPT", or "RAW"
        public string DeviceModel { get; set; }

        public double TotalSizeGb
        {
            get { return Math.Round((double)TotalSizeBytes / (1024 * 1024 * 1024), 1); }
        }

        public double FreeSizeGb
        {
            get { return Math.Round((double)FreeSizeBytes / (1024 * 1024 * 1024), 1); }
        }

        public double UsedPercent
        {
            get
            {
                if (TotalSizeBytes <= 0) return 0;
                long used = TotalSizeBytes - FreeSizeBytes;
                return Math.Round(((double)used / TotalSizeBytes) * 100, 1);
            }
        }

        public string ClusterSizeDisplay
        {
            get
            {
                if (ClusterSizeBytes == 0) return "Unknown";
                if (ClusterSizeBytes >= 1024)
                    return string.Format("{0} KB", ClusterSizeBytes / 1024);
                return string.Format("{0} B", ClusterSizeBytes);
            }
        }

        public string DisplayName
        {
            get
            {
                string label = string.IsNullOrEmpty(VolumeLabel) ? "NO NAME" : VolumeLabel;
                string part = string.IsNullOrEmpty(PartitionStyle) ? "MBR" : PartitionStyle;
                return string.Format("{0} [{1}] - {2:0.0} GB ({3}, {4})",
                    DriveLetter, label, TotalSizeGb, FileSystem, part);
            }
        }

        public bool IsCarOptimal
        {
            get
            {
                // Optimal car USB: FAT32 (or exFAT), MBR partition, 32KB cluster
                bool isFat32 = string.Equals(FileSystem, "FAT32", StringComparison.OrdinalIgnoreCase);
                bool isMbr = string.Equals(PartitionStyle, "MBR", StringComparison.OrdinalIgnoreCase);
                return isFat32 && isMbr;
            }
        }
    }
}

