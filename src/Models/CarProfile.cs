using System;

namespace AutoUSBToolkit.Models
{
    public class CarProfile
    {
        public string Id { get; set; }
        public string Make { get; set; }
        public string SystemName { get; set; }
        public string Category { get; set; } // OEM / Aftermarket / Universal
        public string RecommendedFileSystem { get; set; }
        public uint RecommendedClusterSize { get; set; }
        public bool RequiresMbr { get; set; }
        public int MaxFilesPerFolder { get; set; }
        public int MaxFolderDepth { get; set; }
        public int MaxTotalFiles { get; set; }
        public int RecommendedMaxDriveSizeGb { get; set; }
        public string SupportedAudio { get; set; }
        public string SupportedVideo { get; set; }
        public string TagStandard { get; set; }
        public int MaxArtworkResolution { get; set; }
        public string Notes { get; set; }

        public string DisplayTitle
        {
            get
            {
                if (string.IsNullOrEmpty(SystemName))
                    return Make;
                return string.Format("{0} ({1})", Make, SystemName);
            }
        }

        public string ClusterSizeDisplay
        {
            get
            {
                if (RecommendedClusterSize >= 1024)
                    return string.Format("{0} KB", RecommendedClusterSize / 1024);
                return string.Format("{0} bytes", RecommendedClusterSize);
            }
        }
    }
}

