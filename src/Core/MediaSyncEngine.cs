using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AutoUSBToolkit.Models;

namespace AutoUSBToolkit.Core
{
    public class SyncProgressReport
    {
        public int Percentage { get; set; }
        public string CurrentFile { get; set; }
        public int FilesProcessed { get; set; }
        public int TotalFiles { get; set; }
        public long BytesCopied { get; set; }
        public long TotalBytes { get; set; }
        public string StatusMessage { get; set; }
    }

    public static class MediaSyncEngine
    {
        private static readonly HashSet<string> AudioExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wma", ".m4a", ".aac", ".wav", ".flac", ".ogg", ".alac", ".aiff"
        };

        public static async Task<bool> SyncMediaAsync(
            SyncOptions options,
            CarProfile profile,
            IProgress<SyncProgressReport> progress)
        {
            return await Task.Run(() => ExecuteSync(options, profile, progress));
        }

        private static bool ExecuteSync(
            SyncOptions options,
            CarProfile profile,
            IProgress<SyncProgressReport> progress)
        {
            if (string.IsNullOrEmpty(options.SourcePath) || !Directory.Exists(options.SourcePath))
            {
                Logger.Error("Source path does not exist: " + options.SourcePath);
                return false;
            }

            string targetRoot = options.TargetDrive.TrimEnd('\\') + "\\";
            if (!Directory.Exists(targetRoot))
            {
                Logger.Error("Target USB drive does not exist: " + options.TargetDrive);
                return false;
            }

            Logger.Info(string.Format("Starting Car USB Media Sync: '{0}' -> '{1}'...", options.SourcePath, targetRoot));

            // 1. Gather all source files
            var sourceFiles = new List<string>();
            try
            {
                string[] all = Directory.GetFiles(options.SourcePath, "*.*", SearchOption.AllDirectories);
                foreach (var f in all)
                {
                    string name = Path.GetFileName(f);
                    string ext = Path.GetExtension(f);

                    // Skip Mac and Windows junk if requested
                    if (options.PurgeMacWindowsJunk)
                    {
                        if (name.StartsWith("._") || name.Equals(".DS_Store", StringComparison.OrdinalIgnoreCase) ||
                            name.Equals("Thumbs.db", StringComparison.OrdinalIgnoreCase) || name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                    }

                    // Keep audio and common media files
                    if (AudioExtensions.Contains(ext) || ext.Equals(".mp4", StringComparison.OrdinalIgnoreCase))
                    {
                        sourceFiles.Add(f);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to enumerate source files: " + ex.Message);
                return false;
            }

            int totalFiles = sourceFiles.Count;
            if (totalFiles == 0)
            {
                Logger.Warn("No compatible audio/media files found in source directory.");
                return true;
            }

            long totalBytes = 0;
            foreach (var f in sourceFiles)
            {
                try { totalBytes += new FileInfo(f).Length; } catch { }
            }

            Logger.Info(string.Format("Found {0} media files ({1:0.0} MB) to transfer.",
                totalFiles, (double)totalBytes / (1024 * 1024)));

            int processedFiles = 0;
            long copiedBytes = 0;

            for (int i = 0; i < totalFiles; i++)
            {
                string src = sourceFiles[i];
                string fileName = Path.GetFileName(src);
                string ext = Path.GetExtension(src);

                string safeName = options.SanitizeFilenames ? DriveDoctor.SanitizeCarFilename(fileName) : fileName;

                // Determine destination directory
                string destDir;
                if (options.FolderOrganizationMode == 1) // Artist / Album
                {
                    var tag = AudioOptimizer.ReadId3Info(src);
                    string safeArtist = DriveDoctor.SanitizeCarFilename(tag.Artist);
                    string safeAlbum = DriveDoctor.SanitizeCarFilename(tag.Album);
                    destDir = Path.Combine(targetRoot, "Music", safeArtist, safeAlbum);
                }
                else if (options.FolderOrganizationMode == 2) // Flat playlists with max 250 files
                {
                    int folderBatch = (processedFiles / 250) + 1;
                    destDir = Path.Combine(targetRoot, "Music", string.Format("Batch_{0:D2}", folderBatch));
                }
                else // Preserve relative source hierarchy
                {
                    string relPath = src.Substring(options.SourcePath.Length).TrimStart(Path.DirectorySeparatorChar);
                    string relDir = Path.GetDirectoryName(relPath);
                    destDir = Path.Combine(targetRoot, relDir);
                }

                try
                {
                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    string destFile = Path.Combine(destDir, safeName);

                    // Copy file with progress
                    CopyFileWithStream(src, destFile, ref copiedBytes, totalBytes);

                    // Optimize tags & artwork if MP3
                    if (ext.Equals(".mp3", StringComparison.OrdinalIgnoreCase) && options.NormalizeTagsToId3v23)
                    {
                        AudioOptimizer.OptimizeMp3Tags(destFile, options.ResizeAlbumArtTo500 ? 500 : 800);
                    }

                    processedFiles++;
                }
                catch (Exception ex)
                {
                    Logger.Warn(string.Format("Failed to copy file {0}: {1}", fileName, ex.Message));
                }

                if (progress != null)
                {
                    int pct = (int)((processedFiles * 100.0) / totalFiles);
                    progress.Report(new SyncProgressReport
                    {
                        Percentage = pct,
                        CurrentFile = safeName,
                        FilesProcessed = processedFiles,
                        TotalFiles = totalFiles,
                        BytesCopied = copiedBytes,
                        TotalBytes = totalBytes,
                        StatusMessage = string.Format("Copied {0}/{1} songs ({2}%)", processedFiles, totalFiles, pct)
                    });
                }
            }

            // If requested, run FAT directory sorter so files play in correct sequence
            if (options.RunFatSortAfterSync)
            {
                Logger.Info("Running automatic FAT directory sorting for sequential car playback...");
                FatSorter.SortDriveAsync(options.TargetDrive, null).Wait();
            }

            Logger.Success(string.Format("Media Sync complete! Successfully transferred {0} files ({1:0.0} MB) to {2}.",
                processedFiles, (double)copiedBytes / (1024 * 1024), targetRoot));

            return true;
        }

        private static void CopyFileWithStream(string src, string dst, ref long totalCopied, long overallTotal)
        {
            byte[] buffer = new byte[64 * 1024]; // 64 KB copy buffer
            using (var inStream = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var outStream = new FileStream(dst, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                int bytesRead;
                while ((bytesRead = inStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    outStream.Write(buffer, 0, bytesRead);
                    totalCopied += bytesRead;
                }
            }
        }
    }
}

