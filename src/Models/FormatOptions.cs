using System;

namespace AutoUSBToolkit.Models
{
    public class FormatOptions
    {
        public string DriveLetter { get; set; }
        public string FileSystem { get; set; } // "FAT32", "exFAT", "NTFS"
        public uint ClusterSize { get; set; } // 0 = Default, 4096, 8192, 16384, 32768, 65536
        public string VolumeLabel { get; set; }
        public bool QuickFormat { get; set; }
        public bool EnforceMbr { get; set; }
        public bool WipeData { get; set; }
        public bool UseDirectFat32Engine { get; set; }

        public FormatOptions()
        {
            FileSystem = "FAT32";
            ClusterSize = 32768; // 32KB standard for automotive
            VolumeLabel = "CAR_USB";
            QuickFormat = true;
            EnforceMbr = true;
            WipeData = false;
            UseDirectFat32Engine = false;
        }
    }

    public class SyncOptions
    {
        public string SourcePath { get; set; }
        public string TargetDrive { get; set; }
        public bool SanitizeFilenames { get; set; }
        public bool PurgeMacWindowsJunk { get; set; }
        public bool NormalizeTagsToId3v23 { get; set; }
        public bool ResizeAlbumArtTo500 { get; set; }
        public int FolderOrganizationMode { get; set; } // 0 = Keep as is, 1 = Artist/Album/Title, 2 = Flat with max 255/folder
        public bool TranscodeUnsupportedToMp3 { get; set; }
        public int AudioBitrateKbps { get; set; } // 320, 256, 192
        public bool RunFatSortAfterSync { get; set; }

        public SyncOptions()
        {
            SanitizeFilenames = true;
            PurgeMacWindowsJunk = true;
            NormalizeTagsToId3v23 = true;
            ResizeAlbumArtTo500 = true;
            FolderOrganizationMode = 0;
            TranscodeUnsupportedToMp3 = true;
            AudioBitrateKbps = 320;
            RunFatSortAfterSync = true;
        }
    }

    public class SyncFileItem
    {
        public string SourceFilePath { get; set; }
        public string DestinationFilePath { get; set; }
        public string FileName { get; set; }
        public long FileSizeBytes { get; set; }
        public string Extension { get; set; }
        public bool IsAudio { get; set; }
        public bool NeedsTranscode { get; set; }
        public string Status { get; set; } // "Pending", "Processing", "Copied", "Transcoded", "Failed", "Skipped"

        public string SizeDisplay
        {
            get
            {
                if (FileSizeBytes < 1024 * 1024)
                    return string.Format("{0:0.0} KB", (double)FileSizeBytes / 1024);
                return string.Format("{0:0.0} MB", (double)FileSizeBytes / (1024 * 1024));
            }
        }
    }
}

