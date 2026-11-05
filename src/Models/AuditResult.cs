using System;
using System.Collections.Generic;

namespace AutoUSBToolkit.Models
{
    public class AuditIssue
    {
        public string Category { get; set; } // "Critical", "Warning", "Junk", "Info"
        public string Title { get; set; }
        public string Description { get; set; }
        public string TargetPath { get; set; }
        public bool CanAutoFix { get; set; }
    }

    public class AuditResult
    {
        public string DriveLetter { get; set; }
        public DateTime ScanTime { get; set; }
        public int TotalFilesScanned { get; set; }
        public int TotalFoldersScanned { get; set; }
        public long CleanableJunkBytes { get; set; }

        public List<AuditIssue> Issues { get; set; }
        public List<string> MacJunkFiles { get; set; }
        public List<string> WindowsJunkFiles { get; set; }
        public List<string> IllegalCharacterFiles { get; set; }
        public List<string> OverLimitFolders { get; set; }
        public List<string> OverDepthFolders { get; set; }
        public List<string> UnsupportedMediaFiles { get; set; }

        public AuditResult()
        {
            Issues = new List<AuditIssue>();
            MacJunkFiles = new List<string>();
            WindowsJunkFiles = new List<string>();
            IllegalCharacterFiles = new List<string>();
            OverLimitFolders = new List<string>();
            OverDepthFolders = new List<string>();
            UnsupportedMediaFiles = new List<string>();
            ScanTime = DateTime.Now;
        }

        public int TotalIssuesCount
        {
            get { return Issues.Count; }
        }

        public bool IsClean
        {
            get { return Issues.Count == 0; }
        }

        public string JunkSizeDisplay
        {
            get
            {
                if (CleanableJunkBytes < 1024)
                    return string.Format("{0} B", CleanableJunkBytes);
                if (CleanableJunkBytes < 1024 * 1024)
                    return string.Format("{0:0.0} KB", (double)CleanableJunkBytes / 1024);
                return string.Format("{0:0.0} MB", (double)CleanableJunkBytes / (1024 * 1024));
            }
        }
    }
}

