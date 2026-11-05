using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoUSBToolkit.Core
{
    /// <summary>
    /// FAT Playback Directory Sorter (FATSort engine).
    /// Enforces physical alphabetical/track order on FAT32 USB drives
    /// so car stereos play albums in exact sequential order (01, 02, 03...).
    /// </summary>
    public static class FatSorter
    {
        public static async Task<int> SortDriveAsync(string driveLetter, IProgress<int> progress)
        {
            return await Task.Run(() => ExecuteSort(driveLetter, progress));
        }

        private static int ExecuteSort(string driveLetter, IProgress<int> progress)
        {
            string root = driveLetter.TrimEnd('\\') + "\\";
            Logger.Info("Starting FAT Directory Sorting on " + root + "...");

            int totalFoldersSorted = 0;
            var dirQueue = new Queue<string>();
            dirQueue.Enqueue(root);

            var allDirs = new List<string>();
            while (dirQueue.Count > 0)
            {
                string cur = dirQueue.Dequeue();
                allDirs.Add(cur);
                try
                {
                    foreach (var sub in Directory.GetDirectories(cur))
                    {
                        string name = Path.GetFileName(sub);
                        if (!name.StartsWith(".") && !name.StartsWith("$"))
                        {
                            dirQueue.Enqueue(sub);
                        }
                    }
                }
                catch { }
            }

            int count = allDirs.Count;
            for (int i = 0; i < count; i++)
            {
                string dir = allDirs[i];
                try
                {
                    bool sorted = SortDirectoryEntries(dir);
                    if (sorted) totalFoldersSorted++;
                }
                catch (Exception ex)
                {
                    Logger.Warn(string.Format("Could not sort folder {0}: {1}", dir, ex.Message));
                }

                if (progress != null)
                {
                    progress.Report((int)(((i + 1) * 100.0) / count));
                }
            }

            Logger.Success(string.Format("FAT Playback Sorting complete! Processed {0} folders ({1} reorganized).", count, totalFoldersSorted));
            return totalFoldersSorted;
        }

        private static bool SortDirectoryEntries(string folderPath)
        {
            string[] files = Directory.GetFiles(folderPath);
            if (files.Length <= 1) return false;

            // Natural alphanumeric sort
            Array.Sort(files, NaturalCompare);

            // Create temporary staging directory on the same volume
            string tempDir = Path.Combine(folderPath, "_fat_tmp_" + Guid.NewGuid().ToString("N").Substring(0, 6));
            try
            {
                Directory.CreateDirectory(tempDir);

                // Move all files into staging directory
                var stagedFiles = new List<KeyValuePair<string, string>>();
                for (int i = 0; i < files.Length; i++)
                {
                    string original = files[i];
                    string fileName = Path.GetFileName(original);
                    string target = Path.Combine(tempDir, fileName);
                    File.Move(original, target);
                    stagedFiles.Add(new KeyValuePair<string, string>(target, original));
                }

                // Move them back into the original directory in strictly sorted sequential order
                // This forces the FAT directory table to allocate entries in sequential alphabetical order!
                foreach (var pair in stagedFiles)
                {
                    File.Move(pair.Key, pair.Value);
                }

                return true;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch { }
            }
        }

        private static int NaturalCompare(string x, string y)
        {
            string nameX = Path.GetFileName(x);
            string nameY = Path.GetFileName(y);

            // Alphanumeric chunk comparison (e.g., "Track 2" < "Track 10")
            string[] chunksX = Regex.Split(nameX, "([0-9]+)");
            string[] chunksY = Regex.Split(nameY, "([0-9]+)");

            int minLen = Math.Min(chunksX.Length, chunksY.Length);
            for (int i = 0; i < minLen; i++)
            {
                if (chunksX[i] != chunksY[i])
                {
                    long numX, numY;
                    if (long.TryParse(chunksX[i], out numX) && long.TryParse(chunksY[i], out numY))
                    {
                        return numX.CompareTo(numY);
                    }
                    return string.Compare(chunksX[i], chunksY[i], StringComparison.OrdinalIgnoreCase);
                }
            }

            return chunksX.Length.CompareTo(chunksY.Length);
        }
    }
}

