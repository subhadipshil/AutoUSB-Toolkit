using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoUSBToolkit.Models;

namespace AutoUSBToolkit.Core
{
    public static class DriveDoctor
    {
        private static readonly HashSet<string> MacJunkFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".Trashes", ".fseventsd", ".Spotlight-V100", ".TemporaryItems"
        };

        private static readonly HashSet<string> MacJunkFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".DS_Store"
        };

        private static readonly HashSet<string> WindowsJunkFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Thumbs.db", "desktop.ini", "AlbumArtSmall.jpg", "Folder.jpg"
        };

        private static readonly HashSet<string> SafeAudioExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wma", ".m4a", ".aac", ".wav", ".flac"
        };

        private static readonly HashSet<string> SafeVideoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".avi", ".mkv", ".m4v"
        };

        public static async Task<AuditResult> AuditAsync(UsbDriveInfo drive, CarProfile profile, IProgress<int> progress)
        {
            return await Task.Run(() => AuditDrive(drive, profile, progress));
        }

        private static AuditResult AuditDrive(UsbDriveInfo drive, CarProfile profile, IProgress<int> progress)
        {
            var audit = new AuditResult();
            audit.DriveLetter = drive.DriveLetter;
            string root = drive.DriveLetter.TrimEnd('\\') + "\\";

            Logger.Info(string.Format("Starting Car USB Audit on {0} using preset '{1}'...", drive.DriveLetter, profile.DisplayTitle));

            // 1. Check Partition Style
            if (profile.RequiresMbr && string.Equals(drive.PartitionStyle, "GPT", StringComparison.OrdinalIgnoreCase))
            {
                audit.Issues.Add(new AuditIssue
                {
                    Category = "Critical",
                    Title = "Incompatible Partition Style: GPT Detected",
                    Description = "Car stereos require MBR (Master Boot Record) partition style. Most car head units will fail to detect this USB drive.",
                    TargetPath = drive.DriveLetter,
                    CanAutoFix = false
                });
            }

            // 2. Check File System
            if (!string.IsNullOrEmpty(profile.RecommendedFileSystem) &&
                !string.Equals(drive.FileSystem, profile.RecommendedFileSystem, StringComparison.OrdinalIgnoreCase))
            {
                // If profile recommends FAT32 and drive is exFAT or NTFS
                audit.Issues.Add(new AuditIssue
                {
                    Category = "Warning",
                    Title = string.Format("File System Mismatch: {0} (Expected {1})", drive.FileSystem, profile.RecommendedFileSystem),
                    Description = string.Format("This car model prefers {0}. With {1}, playback may show 'Unreadable Device' or 'No Media'.",
                        profile.RecommendedFileSystem, drive.FileSystem),
                    TargetPath = drive.DriveLetter,
                    CanAutoFix = false
                });
            }

            // 3. Check Cluster Size
            if (drive.ClusterSizeBytes > 0 && drive.ClusterSizeBytes < 16384)
            {
                audit.Issues.Add(new AuditIssue
                {
                    Category = "Warning",
                    Title = string.Format("Small Cluster Size ({0})", drive.ClusterSizeDisplay),
                    Description = "Cluster size is smaller than 16 KB/32 KB. This causes higher FAT index lookup overhead and audio track skipping on automotive head units.",
                    TargetPath = drive.DriveLetter,
                    CanAutoFix = false
                });
            }

            // 4. Check Volume Label
            if (!string.IsNullOrEmpty(drive.VolumeLabel))
            {
                if (drive.VolumeLabel.Length > 11)
                {
                    audit.Issues.Add(new AuditIssue
                    {
                        Category = "Warning",
                        Title = string.Format("Volume Label Too Long ('{0}')", drive.VolumeLabel),
                        Description = "FAT32 volume labels must be 11 characters or fewer. Long labels can cause car firmware display anomalies.",
                        TargetPath = drive.DriveLetter,
                        CanAutoFix = true
                    });
                }

                if (Regex.IsMatch(drive.VolumeLabel, @"[^A-Za-z0-9_\-]"))
                {
                    audit.Issues.Add(new AuditIssue
                    {
                        Category = "Warning",
                        Title = string.Format("Special Characters in Volume Label ('{0}')", drive.VolumeLabel),
                        Description = "Car stereos handle alphanumeric ASCII labels best (e.g., CAR_USB).",
                        TargetPath = drive.DriveLetter,
                        CanAutoFix = true
                    });
                }
            }

            // 5. Scan Files & Directories Recursively
            try
            {
                var dirQueue = new Queue<string>();
                dirQueue.Enqueue(root);

                int scannedDirs = 0;
                int scannedFiles = 0;

                while (dirQueue.Count > 0)
                {
                    string currentDir = dirQueue.Dequeue();
                    scannedDirs++;

                    // Check directory depth
                    int depth = currentDir.Substring(root.Length).Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).Length;
                    if (depth > profile.MaxFolderDepth)
                    {
                        audit.OverDepthFolders.Add(currentDir);
                        audit.Issues.Add(new AuditIssue
                        {
                            Category = "Warning",
                            Title = string.Format("Directory Nesting Exceeds Car Limit (Depth: {0}, Max: {1})", depth, profile.MaxFolderDepth),
                            Description = "Songs inside deep folders may not be indexed by your car stereo.",
                            TargetPath = currentDir,
                            CanAutoFix = false
                        });
                    }

                    // Enumerate subdirectories
                    try
                    {
                        string[] subDirs = Directory.GetDirectories(currentDir);
                        foreach (var sub in subDirs)
                        {
                            string dirName = Path.GetFileName(sub);

                            // Detect Mac junk directories
                            if (MacJunkFolderNames.Contains(dirName))
                            {
                                audit.MacJunkFiles.Add(sub);
                                audit.Issues.Add(new AuditIssue
                                {
                                    Category = "Junk",
                                    Title = "Hidden Mac System Directory Detected",
                                    Description = dirName + " takes space and slows down head unit library scanning.",
                                    TargetPath = sub,
                                    CanAutoFix = true
                                });
                                continue;
                            }

                            // Detect Windows Recycle Bin
                            if (string.Equals(dirName, "$RECYCLE.BIN", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(dirName, "System Volume Information", StringComparison.OrdinalIgnoreCase))
                            {
                                audit.WindowsJunkFiles.Add(sub);
                                continue;
                            }

                            dirQueue.Enqueue(sub);
                        }
                    }
                    catch { }

                    // Enumerate files in current directory
                    try
                    {
                        string[] files = Directory.GetFiles(currentDir);
                        int validMediaCountInDir = 0;

                        foreach (var file in files)
                        {
                            scannedFiles++;
                            string fileName = Path.GetFileName(file);
                            string ext = Path.GetExtension(file);

                            // Check Mac AppleDouble files (starts with ._)
                            if (fileName.StartsWith("._"))
                            {
                                audit.MacJunkFiles.Add(file);
                                try { audit.CleanableJunkBytes += new FileInfo(file).Length; } catch { }
                                continue;
                            }

                            // Check Mac .DS_Store
                            if (MacJunkFileNames.Contains(fileName))
                            {
                                audit.MacJunkFiles.Add(file);
                                try { audit.CleanableJunkBytes += new FileInfo(file).Length; } catch { }
                                continue;
                            }

                            // Check Windows Thumbs.db / desktop.ini
                            if (WindowsJunkFileNames.Contains(fileName))
                            {
                                audit.WindowsJunkFiles.Add(file);
                                try { audit.CleanableJunkBytes += new FileInfo(file).Length; } catch { }
                                continue;
                            }

                            // Check Problematic Filename Characters for Car Audio
                            if (HasProblematicCarCharacters(fileName))
                            {
                                audit.IllegalCharacterFiles.Add(file);
                                audit.Issues.Add(new AuditIssue
                                {
                                    Category = "Warning",
                                    Title = "Problematic Filename Characters",
                                    Description = string.Format("File '{0}' contains characters that crash or confuse car stereos.", fileName),
                                    TargetPath = file,
                                    CanAutoFix = true
                                });
                            }

                            // Check Unsupported Media Extensions
                            if (!string.IsNullOrEmpty(ext))
                            {
                                bool isAudio = SafeAudioExtensions.Contains(ext);
                                bool isVideo = SafeVideoExtensions.Contains(ext);

                                if (isAudio || isVideo)
                                {
                                    validMediaCountInDir++;
                                }
                                else if (!ext.Equals(".txt", StringComparison.OrdinalIgnoreCase) &&
                                         !ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) &&
                                         !ext.Equals(".png", StringComparison.OrdinalIgnoreCase))
                                {
                                    audit.UnsupportedMediaFiles.Add(file);
                                }
                            }
                        }

                        // Check max files in directory limit
                        if (validMediaCountInDir > profile.MaxFilesPerFolder)
                        {
                            audit.OverLimitFolders.Add(currentDir);
                            audit.Issues.Add(new AuditIssue
                            {
                                Category = "Warning",
                                Title = string.Format("Folder Exceeds Song Limit ({0} songs, Max: {1})", validMediaCountInDir, profile.MaxFilesPerFolder),
                                Description = "Most car head units truncate tracks after file 255 in a single directory.",
                                TargetPath = currentDir,
                                CanAutoFix = false
                            });
                        }
                    }
                    catch { }

                    if (progress != null && scannedDirs % 5 == 0)
                    {
                        progress.Report(Math.Min(95, scannedDirs * 2));
                    }
                }

                audit.TotalFoldersScanned = scannedDirs;
                audit.TotalFilesScanned = scannedFiles;

                if (progress != null) progress.Report(100);
            }
            catch (Exception ex)
            {
                Logger.Error("Error scanning drive: " + ex.Message);
            }

            // Summary log
            Logger.Info(string.Format("Audit complete: {0} files, {1} folders scanned. Found {2} issues ({3} Mac junk, {4} Win junk, {5} bad filenames).",
                audit.TotalFilesScanned, audit.TotalFoldersScanned, audit.TotalIssuesCount,
                audit.MacJunkFiles.Count, audit.WindowsJunkFiles.Count, audit.IllegalCharacterFiles.Count));

            return audit;
        }

        public static async Task<int> CleanAndFixAsync(AuditResult audit, IProgress<int> progress)
        {
            return await Task.Run(() => ExecuteCleanAndFix(audit, progress));
        }

        private static int ExecuteCleanAndFix(AuditResult audit, IProgress<int> progress)
        {
            int fixedCount = 0;
            int totalItems = audit.MacJunkFiles.Count + audit.WindowsJunkFiles.Count + audit.IllegalCharacterFiles.Count;
            if (totalItems == 0) return 0;

            int processed = 0;

            // 1. Purge Mac Junk Files
            foreach (var file in audit.MacJunkFiles)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                        fixedCount++;
                    }
                    else if (Directory.Exists(file))
                    {
                        Directory.Delete(file, true);
                        fixedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn("Could not remove junk: " + ex.Message);
                }

                processed++;
                if (progress != null) progress.Report((processed * 100) / totalItems);
            }

            // 2. Purge Windows Junk Files
            foreach (var file in audit.WindowsJunkFiles)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                        fixedCount++;
                    }
                }
                catch { }

                processed++;
                if (progress != null) progress.Report((processed * 100) / totalItems);
            }

            // 3. Rename Problematic Filenames to Clean Car-Safe Names
            foreach (var file in audit.IllegalCharacterFiles)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        string dir = Path.GetDirectoryName(file);
                        string fileName = Path.GetFileName(file);
                        string cleanName = SanitizeCarFilename(fileName);

                        if (!string.Equals(fileName, cleanName, StringComparison.Ordinal))
                        {
                            string targetPath = Path.Combine(dir, cleanName);
                            // Ensure no collision
                            if (File.Exists(targetPath))
                            {
                                string baseName = Path.GetFileNameWithoutExtension(cleanName);
                                string ext = Path.GetExtension(cleanName);
                                targetPath = Path.Combine(dir, baseName + "_" + Guid.NewGuid().ToString("N").Substring(0, 4) + ext);
                            }

                            File.Move(file, targetPath);
                            fixedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn("Could not rename file: " + ex.Message);
                }

                processed++;
                if (progress != null) progress.Report((processed * 100) / totalItems);
            }

            Logger.Success(string.Format("Clean & Fix completed: Removed/repaired {0} items!", fixedCount));
            if (progress != null) progress.Report(100);
            return fixedCount;
        }

        public static bool HasProblematicCarCharacters(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            // Car stereos choke on: [ ] { } ? * : | " < > # % & + = ~ ^ $ ` ; ' and emojis/non-ascii
            foreach (char c in name)
            {
                if (c > 127) return true; // Non-ASCII / Unicode / Emojis
                if (c == '[' || c == ']' || c == '{' || c == '}' || c == '?' ||
                    c == '*' || c == ':' || c == '|' || c == '"' || c == '<' ||
                    c == '>' || c == '#' || c == '%' || c == '&' || c == '+' ||
                    c == '=' || c == '~' || c == '^' || c == '$' || c == '`' ||
                    c == ';' || c == '\'')
                {
                    return true;
                }
            }
            return false;
        }

        public static string SanitizeCarFilename(string name)
        {
            if (string.IsNullOrEmpty(name)) return "track.mp3";

            string ext = Path.GetExtension(name);
            string baseName = Path.GetFileNameWithoutExtension(name);

            var sb = new StringBuilder();
            foreach (char c in baseName)
            {
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ' ')
                {
                    sb.Append(c);
                }
                else if (c == '(' || c == ')' || c == '.')
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('_');
                }
            }

            string cleaned = Regex.Replace(sb.ToString(), @"_{2,}", "_");
            cleaned = Regex.Replace(cleaned, @"\s{2,}", " ").Trim();
            if (string.IsNullOrEmpty(cleaned)) cleaned = "track";

            // Max filename length safe for car head units: 64 chars
            if (cleaned.Length > 64)
            {
                cleaned = cleaned.Substring(0, 64).Trim();
            }

            return cleaned + ext.ToLowerInvariant();
        }
    }
}

